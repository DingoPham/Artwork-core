using ArtworkCore.Class;
using ArtworkCore.Models;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using System.Data;
using System.Text;
using ArtworkCore.Services;
using ArtworkCore.FilterAttribute;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [ServiceFilter(typeof(CustomFilter))]
    [Authorize]
    [Route("[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly ILogger<VideoController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        private readonly AgeCaculator _ageCaculator;
        public VideoController(ILogger<VideoController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action, AgeCaculator ageCaculator)
        {
            _db_action = db_action;
            _configuration = configuration;
            _logger = logger;
            _ageCaculator = ageCaculator;
        }

        #region Get
        [HttpGet]
        [Authorize(Roles = "admin,user")]
        public IActionResult Get()
        {
            string username = User.Claims.FirstOrDefault(_ => _.Type == "username").Value;

            DateTime userAge = DateTime.MinValue;
            try
            {
                using (NpgsqlConnection _connect = _db_action.Connection())
                {
                    _connect.Open();

                    string query = "SELECT age FROM master.account WHERE username = :Username";
                    using (NpgsqlCommand cmd = new(query, _connect))
                    {
                        cmd.Parameters.Add(new NpgsqlParameter("Username", NpgsqlTypes.NpgsqlDbType.Text)
                        {
                            Value = username
                        });
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userAge = reader.GetDateTime(0);
                            }
                            else
                            {
                                return Unauthorized(new { authMessage = "User not found!" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user age");
                return StatusCode(500, "Internal server error");
            }

            int userBirth = _ageCaculator.CalculateAge(userAge);
            // Check if the user is at least 18 years old
            if (userBirth < 18)
            {
                return BadRequest(new { ageMessage = "You must be at least 18 years old to access this content!" });
            }

            DataTable dt = new();
            List<Video> list_data_video = new();

            try
            {
                using (NpgsqlConnection _connect = _db_action.Connection())
                {
                    _connect.Open();

                    string query = $"SELECT * FROM master.video;";
                    dt = new DataTable();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                    {
                        NpgsqlDataReader dataReader = cmd.ExecuteReader();
                        dt.Load(dataReader);
                    }
                    list_data_video = (from rw in dt.AsEnumerable()
                                         select new Video()
                                         {
                                             Id = Convert.ToString(rw["id"]),
                                             VideoUrl = Convert.ToString(rw["video_url"]),
                                             VideoName = Convert.ToString(rw["video_name"]),
                                             VideoDescribe = Convert.ToString(rw["video_describe"])
                                         }).ToList();
                }
                var list_total = new
                {
                    list_data_video = list_data_video
                };
                return Ok(list_total);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching NSFW art");
                return StatusCode(500, "Internal server error");
            }
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
                    case "video":
                        var video = JsonConvert.DeserializeObject<Video>(request.Data.ToString());
                        if (video == null)
                            return BadRequest("Invalid data");

                        video.Id = newImageId;

                        list_param.Add(_db_action.ParamMaker("id", video.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_url", video.VideoUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_name", video.VideoName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_describe", video.VideoDescribe, DbType.String));

                        string video_query = $"INSERT INTO master.video (id, video_url, video_name, video_describe)VALUES(:id, :video_url, :video_name, :video_describe);";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(video_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Insert video successfully";
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
                    case "video":
                        var video = JsonConvert.DeserializeObject<Video>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_url", video.VideoUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_name", video.VideoName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("video_describe", video.VideoDescribe, DbType.String));

                        string video_query = $"UPDATE master.video SET video_url = :video_url, video_name = :video_name, video_describe = :video_describe WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(video_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Update video successfully";
                        break;
                }

                _connect.Close();
            }
            catch (Exception ex)
            {
                message = "Update video failed\n\r" + ex;
                return StatusCode(500, new { message });
            }

            return Ok(new { message });
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
                    case "video":
                        var video = JsonConvert.DeserializeObject<Video>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", video.Id, DbType.String));

                        string video_query = $"DELETE FROM master.video WHERE id = :id;";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(video_query, _connect))
                        {
                            foreach (NpgsqlParameter param in list_param)
                            {
                                cmd.Parameters.Add(param);
                            }
                            cmd.ExecuteNonQuery();
                        }

                        message = "Delete video successfully";
                        break;
                }

                _connect.Close();
            }
            catch (Exception ex)
            {
                message = "Delete video failed\n\r" + ex;
            }

            return Ok(message);
        }
        #endregion
    }
}
