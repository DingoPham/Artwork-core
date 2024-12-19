using ArtworkCore.Class;
using ArtworkCore.Models;
using ArtworkCore.Services;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly JwtService _jwtService;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        public AuthenticationController(ILogger<AuthenticationController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action, JwtService jwtService, EmailService emailService)
        {
            _logger = logger;
            _configuration = configuration;
            _db_action = db_action;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        #region Login 
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body can't be null" });
            }

            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Username and password cannot be empty" });
            }

            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();

                string query = $"SELECT * FROM master.account WHERE username = :username AND password = :password;";
                list_param.Add(_db_action.ParamMaker("username", request.UserName, DbType.String));
                list_param.Add(_db_action.ParamMaker("password", request.Password, DbType.String));

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (var param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    dt.Load(reader);
                }

                if (dt.Rows.Count == 0)
                {
                    return Unauthorized(new { loginMessage = "Invalid username or password" });
                }

                string token = _jwtService.GenerateJwtToken(request.UserName, dt.Rows[0]["role"].ToString());

                return Ok(new
                {
                    loginMessage = "Login Successful!",
                    token = token,
                    username = dt.Rows[0]["username"].ToString(),
                    role = dt.Rows[0]["role"].ToString(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { loginMessage = "Login Failed...", error = ex.Message });
            }
            finally
            {
                _connect.Close();
            }
        }
        #endregion

        #region Register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();

                string newAccountId = Guid.NewGuid().ToString();

                list_param.Add(_db_action.ParamMaker("id", newAccountId, DbType.String));
                list_param.Add(_db_action.ParamMaker("email", request.Email, DbType.String));
                list_param.Add(_db_action.ParamMaker("username", request.Username, DbType.String));
                list_param.Add(_db_action.ParamMaker("password", request.Password, DbType.String));
                list_param.Add(_db_action.ParamMaker("age", request.Age, DbType.Date));
                list_param.Add(_db_action.ParamMaker("role", request.Role, DbType.String));

                string query = $"INSERT INTO master.account (id, email, username, password, age, role) VALUES (:id, :email, :username, :password, :age, :role);";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (NpgsqlParameter param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }
                    cmd.ExecuteNonQuery();
                }

                _connect.Close();
                return Ok(new { registerMessage = "Registration Successful!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { registerMessage = "Registration Failed...", error = ex.Message });
            }
        }
        #endregion

        #region Forgot password
        [HttpPost("forget-password")]
        [AllowAnonymous]
        public async Task<IActionResult> Forget([FromBody] ForgetPRequest request)
        {
            // Kiểm tra xem email có được nhập không
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            NpgsqlConnection _connect = _db_action.Connection();
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();

            try
            {
                // Kết nối với cơ sở dữ liệu và kiểm tra xem email có tồn tại không
                _connect.Open();

                list_param.Add(_db_action.ParamMaker("email", request.Email, DbType.String));

                string query = $"SELECT * FROM master.account WHERE email = :email;";
                string userName = string.Empty;

                using (var cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (NpgsqlParameter param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }

                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    dt.Load(reader);
                }

                // Nếu không tìm thấy email, trả về lỗi NotFound
                if (dt.Rows.Count > 0)
                {
                    userName = dt.Rows[0]["username"].ToString();
                }
                else
                {
                    return NotFound(new { message = "Email not found" });
                }

                string baseUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? "http://localhost:8080"
                    : "https://exemple.com";
        
                // Nếu tìm thấy email, tạo một token khôi phục mật khẩu
                string resetToken = _jwtService.GenerateResetToken(request.Email);

                string updateTokenQuery = $"UPDATE master.account SET reset_token = @reset WHERE email = @Email;";
                using (var cmd = new NpgsqlCommand(updateTokenQuery, _connect))
                {
                    cmd.Parameters.AddWithValue("@reset", resetToken);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    await cmd.ExecuteNonQueryAsync();
                }

                Console.WriteLine("Token saved to database");

                string resetUrl = $"{baseUrl}/reset-password?token={resetToken}";

                // Gửi email khôi phục mật khẩu
                string subject = "Reset Your Password";
                // body làm đẹp thư gửi đi
                string body = $@"
                    <div style='font-family: Arial, sans-serif; font-size: 16px; color: #333;'>
                        <h2 style='color: #2e6c80;'>Password Reset Request</h2>
                        <p>Dear {userName},</p>
                        <p>We received a request to reset your password. Click the button below to reset your password:</p>
                        <a href='{resetUrl}' style='display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a>
                        <p>If you did not request a password reset, please ignore this email.</p>
                        <br/>
                        <p style='font-size: 12px; color: #777;'>If you're having trouble clicking the button, copy and paste the URL below into your web browser:</p>
                        <p style='font-size: 12px; color: #777;'>{resetUrl}</p>
                    </div>";

                // Gọi phương thức gửi email qua dịch vụ EmailService
                await _emailService.SendAsync(request.Email, subject, body);

                // Thông báo thành công khi gửi email khôi phục
                return Ok(new { forgetMessage = "Recovery notification sent" });

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // Nếu có lỗi trong quá trình xử lý, trả về mã lỗi 500
                return StatusCode(500, new { forgetMessage = $"Failed to send recovery information: {ex.Message}" });
            }
            finally
            {
                // Đóng kết nối cơ sở dữ liệu
                _connect.Close();
            }

        }
        #endregion

        #region Reset password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromQuery] string token, [FromBody] ResetRequest request)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { resetMessage = "token is empty" });
            }

            NpgsqlConnection _connect = _db_action.Connection();
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();

            try
            {
                _connect.Open();

                string query = "SELECT * FROM master.account WHERE reset_token = @token";

                list_param.Add(_db_action.ParamMaker("token", token, DbType.String));

                using (var cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (NpgsqlParameter param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }

                    NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                    dt.Load(reader);
                }
                if (dt.Rows.Count == 0)
                {
                    return BadRequest(new { resetMessage = "Invalid token" });
                }

                string updatePasswordQuery = @"UPDATE master.account SET password = @newPassword, reset_token = NULL WHERE reset_token = @token";

                using (var cmd = new NpgsqlCommand(updatePasswordQuery, _connect))
                {
                    cmd.Parameters.AddWithValue("@newPassword", request.NewPassword);
                    cmd.Parameters.AddWithValue("@token", token);

                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Rows affected: {affectedRows}");
                }
                return Ok(new { resetMessage = "Password successful reset!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { resetMessage = "Password reset failed", ex.Message });
            }

            finally
            {
                _connect.Close();
            }
        }
        #endregion
    }
}
