using ArtworkCore.Class;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly IPostgresSQL_Connection _db_action;

        public AccountController(IPostgresSQL_Connection db_action)
        {
            _db_action = db_action;
        }

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
                // Tao id cho ng dung
                string newUserId = Guid.NewGuid().ToString();

                // Them cac thong tin
                list_param.Add(_db_action.ParamMaker("id", newUserId, DbType.String));
                list_param.Add(_db_action.ParamMaker("username", request.Username, DbType.String));
                list_param.Add(_db_action.ParamMaker("password", request.Password, DbType.String));
                list_param.Add(_db_action.ParamMaker("role", request.Role, DbType.String)); // Co the chon role

                // Thuc hien cau truy van de them user vao db
                string query = "INSERT INTO master.account (id, username, password, role) VALUES(:id, :username, :password, :role);";
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
    }
}
