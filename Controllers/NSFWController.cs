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

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NSFWController : ControllerBase
    {
        private readonly ILogger<NSFWController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        public NSFWController(ILogger<NSFWController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action)
        {
            _db_action = db_action;
            _configuration = configuration;
            _logger = logger;
        }

        #region Get
        [HttpGet]
        [Authorize(Roles = "admin,user")]
        public IActionResult Get()
        {
            DataTable dt = new();
            List<NsfwArt> list_data_nsfwart = new();

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "You need to sign in to gain access to here!" });
            }

            try
            {
                NpgsqlConnection _connect = _db_action.Connection();

                _connect.Open();

                string query = $"SELECT * FROM master.nsfw_art;";
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
    }
}
