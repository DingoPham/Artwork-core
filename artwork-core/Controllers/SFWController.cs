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

namespace ArtworkCore.Controllers
{
    [ApiController]
    [ServiceFilter(typeof(CustomFilter))]
    [Authorize]
    [Route("[controller]")]
    public class SFWController : ControllerBase
    {
        private readonly ILogger<SFWController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        public SFWController(ILogger<SFWController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action)
        {
            _db_action = db_action;
            _configuration = configuration;
            _logger = logger;
        }

        #region Get
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            DataTable dt = new();
            List<SfwArt> list_data_sfwart = new();
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
                                        ImgDescribe = Convert.ToString(rw["img_describe"]),
                                        Order = Convert.ToInt32(rw["order"])
                                    }).ToList();

                //query = $"SELECT * FROM master.nsfw_art;";
                //dt = new();
                //using (NpgsqlCommand cmd = new NpgsqlCommand(query, _connect))
                //{
                //    NpgsqlDataReader dataReader = cmd.ExecuteReader();
                //    dt.Load(dataReader);
                //}
                _connect.Close();
                _connect.Dispose();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error fetching SFW art");
            }

            var list_total = new
            {
                list_data_sfwart = list_data_sfwart
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
                        if (sfw_art == null)
                            return BadRequest("Invalid data");

                        sfw_art.Id = newImageId;

                        list_param.Add(_db_action.ParamMaker("id", sfw_art.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_url", sfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_name", sfw_art.ImgName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_describe", sfw_art.ImgDescribe, DbType.String));

                        string sfw_art_query = $"INSERT INTO master.sfw_art (id, img_url, img_name, img_describe)VALUES(:id, :img_url, :img_name, :img_describe);";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(sfw_art_query, _connect))
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

        #region
        [HttpPut("order")]
        [Authorize(Roles = "admin")]
        public IActionResult Order([FromBody] OrderRequest request)
        {
            string message = string.Empty;
            NpgsqlConnection _connect = _db_action.Connection();

            try
            {
                _connect.Open();

                if (request.Images == null || !request.Images.Any())
                {
                    return BadRequest("Danh sách ảnh không hợp lệ");
                }

                // Cập nhật thứ tự cho từng ảnh trong danh sách
                using (NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    cmd.Connection = _connect;
                    cmd.CommandType = CommandType.Text;

                    // Chuẩn bị câu lệnh SQL để cập nhật từng bản ghi
                    StringBuilder queryBuilder = new StringBuilder();
                    List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

                    for (int i = 0; i < request.Images.Count; i++)
                    {
                        var image = request.Images[i];
                        queryBuilder.Append($"UPDATE master.sfw_art SET \"order\" = @order{i} WHERE id = @id{i}; ");
                        parameters.Add(new NpgsqlParameter($"@order{i}", i));
                        parameters.Add(new NpgsqlParameter($"@id{i}", image.Id));
                    }

                    cmd.CommandText = queryBuilder.ToString();
                    cmd.Parameters.AddRange(parameters.ToArray());
                    cmd.ExecuteNonQuery();
                }

                message = "Cập nhật thứ tự ảnh thành công";
                _connect.Close();
            }
            catch (Exception ex)
            {
                message = "Cập nhật thứ tự ảnh thất bại\n\r" + ex.Message;
                return StatusCode(500, new { message });
            }
            finally
            {
                _connect.Dispose();
            }

            return Ok(new { message });
        }
        #endregion
    }
}