using ArtworkCore.Class;
using ArtworkCore.Models;
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
using ArtworkCore.Services;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArtworkCombineController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<ArtworkCombineController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        public ArtworkCombineController(ILogger<ArtworkCombineController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action, EmailService emailService)
        {
            _db_action = db_action;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        #region Get
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            DataTable dt = new();
            List<SfwArt> list_data_sfwart = new();
            List<SfwVideo> list_data_sfwvideo = new();
            List<NsfwArt> list_data_nsfwart = new();
            List<NsfwVideo> list_data_nsfwvideo = new();
            try
            {
                NpgsqlConnection _connect = _db_action.Connection();

                _connect.Open();

                string query = $"SELECT * FROM master.sfw_art;";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    NpgsqlDataReader dataReader = cmd.ExecuteReader();
                    dt.Load(dataReader);
                }
                list_data_sfwart = (from rw in dt.AsEnumerable()
                                    select new SfwArt()
                                    {
                                        Id = Convert.ToString(rw["id"]),
                                        ImgUrl = Convert.ToString(rw["img_url"]),
                                        ImgName = Convert.ToString(rw["img_name"]),
                                        ImgDescribe = Convert.ToString(rw["img_describe"])
                                    }).ToList();

                query = $"SELECT * FROM master.nsfw_art;";
                dt = new();
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                {
                    NpgsqlDataReader dataReader = cmd.ExecuteReader();
                    dt.Load(dataReader);
                }
                list_data_nsfwart = (from rw in dt.AsEnumerable()
                                     select new NsfwArt()
                                     {
                                         Id = Convert.ToString(rw["id"]),
                                         ImgNsfwUrl = Convert.ToString(rw["img_nsfw_url"]),
                                         ImgNsfwName = Convert.ToString(rw["img_nsfw_name"]),
                                         ImgNsfwDescribe = Convert.ToString(rw["img_nsfw_describe"])
                                     }).ToList();
                _connect.Close();
                _connect.Dispose();
            }
            catch (Exception ex) { }

            var list_total = new
            {
                list_data_sfwart = list_data_sfwart,
                list_data_nsfwart = list_data_nsfwart
            };
            return Ok(list_total);
        }
        #endregion

        #region Post
        [HttpPost("post")]
        [Authorize(Roles = "admin")]
        public IActionResult Post(PostRequest request)
        {
            string newImageId = Guid.NewGuid().ToString();
            string message = string.Empty;
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();

                switch (request.Type.ToLower())
                {
                    case "sfw_art":
                        var sfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        if(sfw_art == null)
                            return BadRequest("Invalid data");

                        sfw_art.Id = newImageId;

                        list_param.Add(_db_action.ParamMaker("id", sfw_art.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_url", sfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_name", sfw_art.ImgName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_describe", sfw_art.ImgDescribe, DbType.String));

                        string sfw_art_query = $"INSERT INTO  master.sfw_art (id, img_url, img_name, img_describe)VALUES(:id, :img_url, :img_name, :img_describe);";
                        using(NpgsqlCommand cmd = new NpgsqlCommand(sfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Insert image successfully";
                        break;

                    case "nsfw_art":
                        var nsfw_art = JsonConvert.DeserializeObject<NsfwArt>(request.Data.ToString());
                        if (nsfw_art == null)
                            return BadRequest("Invalid data");

                        nsfw_art.Id = newImageId;

                        list_param.Add(_db_action.ParamMaker("id", nsfw_art.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_url", nsfw_art.ImgNsfwUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_name", nsfw_art.ImgNsfwName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_describe", nsfw_art.ImgNsfwDescribe, DbType.String));

                        string nsfw_art_query = $"INSERT INTO master.nsfw_art (id, img_nsfw_url, img_nsfw_name, img_nsfw_describe)VALUES(:id, :img_nsfw_url, null, null);";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(nsfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Insert image successfully";
                        break;

                    default:
                        return BadRequest("Unknow type");
                }

                _connect.Close();
            }
            catch (Exception ex) 
            {
                message = "Insert image failed\n\r" + ex;
                return StatusCode(500, new { message });
            }
            return Ok(new { message, id = newImageId });
        }
        #endregion

        #region Put
        [HttpPut("put/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Put(string id, PutRequest request)
        {
            string message = string.Empty;
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();
                switch (request.Type.ToLower())
                {
                    case "sfw_art":
                        var sfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_url", sfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_name", sfw_art.ImgName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_describe", sfw_art.ImgDescribe, DbType.String));

                        string sfw_art_query = $"UPDATE master.sfw_art SET img_url = :img_url, img_name = :img_name, img_describe = :img_describe WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(sfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Update image successfully";
                        break;

                    case "nsfw_art":
                        var nsfw_art = JsonConvert.DeserializeObject<NsfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_url", nsfw_art.ImgNsfwUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_name", nsfw_art.ImgNsfwName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_describe", nsfw_art.ImgNsfwDescribe, DbType.String));

                        string nsfw_art_query = $"UPDATE master.nsfw_art SET img_nsfw_url = :img_nsfw_url, img_nsfw_name = :img_nsfw_name, img_nsfw_describe = :img_nsfw_describe WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(nsfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Update image successfully";
                        break;
                }

                _connect.Close();
            }
            catch (Exception ex) 
            {
                message = "Update image failed\n\r" + ex;
                return StatusCode(500, new { message });
            }

            return Ok(new{message});
        }
        #endregion

        #region Delete
        [HttpDelete("del")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(DeleteRequest request)
        {
            string message = string.Empty;
            DataTable dt = new();
            List<NpgsqlParameter> list_param = new();
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();
                switch (request.Type.ToLower())
                {
                    case "sfw_art":
                        var sfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", sfw_art.Id, DbType.String));

                        string sfw_art_query = $"DELETE FROM master.sfw_art WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(sfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Delete image successfully";
                        break;

                    case "nsfw_art":
                        var nsfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", nsfw_art.Id, DbType.String));

                        string nsfw_art_query = $"DELETE FROM master.nsfw_art WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(nsfw_art_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Delete image successfully";
                        break;
                }

                _connect.Close();
            }
            catch (Exception ex)
            {
                message = "Insert image failed\n\r" + ex;
            }

            return Ok(message);
        }
        #endregion

        #region Login 
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if(request == null)
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

                string token = GenerateJwtToken(request.UserName);
                
                return Ok(new { loginMessage = "Login Successful!", 
                                token = token, 
                                username = dt.Rows[0]["username"].ToString(),
                                role = dt.Rows[0]["role"].ToString(),
                });
            }
            catch (Exception ex) { 
                return StatusCode(500, new {loginMessage = "Login Failed...", error = ex.Message });
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
            if (string.IsNullOrEmpty(request.Email) ||
            string.IsNullOrEmpty(request.Username) ||
            string.IsNullOrEmpty(request.Password) ||
            request.Age <= 0)
            {
                return BadRequest(new { registerMessage = "All fields are required and age must be greater than 0" });
            }
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
                list_param.Add(_db_action.ParamMaker("age", request.Age, DbType.Int16));
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
                return BadRequest( new {message = "Email is required"});
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
                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Email not found" });
                }

                string baseUrl;

                // Kiểm tra môi trường
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    baseUrl = "http://localhost:8080";
                }
                else
                {
                    baseUrl = "https://exemple.com"; // chưa có public
                }

                // Nếu tìm thấy email, tạo một token khôi phục mật khẩu
                string resetToken = Guid.NewGuid().ToString();
                DateTime tokenExpiration = DateTime.UtcNow.AddMinutes(30);

                string updateTokenQuery = $"UPDATE master.account SET reset_token = @reset, token_expiration = @expiration WHERE email = @Email;";
                using (var cmd = new NpgsqlCommand(updateTokenQuery, _connect))
                {
                    cmd.Parameters.AddWithValue("@reset", resetToken);
                    cmd.Parameters.AddWithValue("@expiration", tokenExpiration);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    await cmd.ExecuteNonQueryAsync();
                }

                string resetUrl = $"{baseUrl}/new-password?token={resetToken}";

                // Gửi email khôi phục mật khẩu
                string subject = "Reset Your Password";
                string body = $"Click the link to reset your password: <a href='{resetUrl}'>Reset Password</a>";

                // Gọi phương thức gửi email qua dịch vụ EmailService
                await _emailService.SendAsync(request.Email, subject, body);

                // Thông báo thành công khi gửi email khôi phục
                return Ok(new { message = "Recovery notification sent"});
            }

            catch (Exception ex) 
            {
                // Nếu có lỗi trong quá trình xử lý, trả về mã lỗi 500
                return StatusCode(500, new { message = "Failed to send recovery information"});
            }
            finally
            {
                // Đóng kết nối cơ sở dữ liệu
                _connect.Close(); 
            }
           
        }
        #endregion

        //#region Reset password
        //[HttpPost("reset-password")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        //{
        //    NpgsqlConnection _connect = _db_action.Connection();
        //    DataTable dt = new();
        //    List<NpgsqlParameter> list_param = new();


        //}
        //#endregion

        #region Generate login token
        private string GenerateJwtToken(string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            if (key == null || key.Length == 0)
            {
                throw new Exception("JWT Secret Key is not configured.");
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("username", username)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        #endregion
    }
}
