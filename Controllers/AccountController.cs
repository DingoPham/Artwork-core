using ArtworkCore.Class;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        private readonly PasswordHasher<string> _passwordHasher; //ma hoa mat khau

        public AccountController(ILogger<AccountController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action)
        {
            _logger = logger;
            _db_action = db_action;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<string>();
        }

        #region register
        // Api dang ky tai khoan
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            NpgsqlConnection _connect = _db_action.Connection();
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            string message = string.Empty;

            try
            {
                _connect.Open();
                //Ma hoa mat khau
                string hashedPassword = _passwordHasher.HashPassword(request.Password, request.Password);
                // Tao id cho ng dung
                string newUserId = Guid.NewGuid().ToString();

                // Them cac thong tin
                list_param.Add(_db_action.ParamMaker("id", newUserId, DbType.String));
                list_param.Add(_db_action.ParamMaker("email", request.Email, DbType.String));
                list_param.Add(_db_action.ParamMaker("username", request.Username, DbType.String));
                list_param.Add(_db_action.ParamMaker("password", request.Password, DbType.String));
                list_param.Add(_db_action.ParamMaker("age", request.Age, DbType.Int16));
                list_param.Add(_db_action.ParamMaker("role", request.Role, DbType.String)); // Co the chon role

                // Thuc hien cau truy van de them user vao db
                string query = "INSERT INTO master.account (id, email, username, password, age, role) VALUES(:id, :email, :username, :password, age, :role);";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (var param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }
                    cmd.ExecuteNonQuery();
                }
                message = "Registered successfully";
                _connect.Close();
            }
            catch (Exception ex)
            {
                message = "Failed to registered" + ex.Message;
                return StatusCode(500, new { message });
            }

            return Ok(new { message });
        }
        #endregion

        #region login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            NpgsqlConnection _connect = _db_action.Connection();
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();

            string role = string.Empty;
            string hashedPassword = string.Empty;

            try
            {
                _connect.Open();
                //Tao param cho username
                list_param.Add(_db_action.ParamMaker("username", request.UserName, DbType.String));

                //Thuc hien truy van voi db
                string query = "SELECT password, role FROM master.account WHERE username = :username;";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    foreach (var param in list_param)
                    {
                        cmd.Parameters.Add(param);
                    }

                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        hashedPassword = reader["password"].ToString();
                        role = reader["role"].ToString();
                    }
                }

                _connect.Close();

                if (string.IsNullOrEmpty(role) || _passwordHasher.VerifyHashedPassword(hashedPassword, request.Password, hashedPassword) != PasswordVerificationResult.Success)
                {
                    return Unauthorized("Invalid username or password");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Login failed: " + ex.Message });
            }

            //Gan role cho tai khoan sau khi xac thuc thanh cong
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, request.UserName),
                new Claim(ClaimTypes.Role, role) //Gan role tu db
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok("Login successful!");
        }
        #endregion 
    }
}
