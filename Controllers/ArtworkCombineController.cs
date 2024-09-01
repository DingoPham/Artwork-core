using ArtworkCore.Class;
using ArtworkCore.Models;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using System.Data;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArtworkCombineController : ControllerBase
    {
        private readonly ILogger<ArtworkCombineController> _logger;
        private IConfiguration _configuration;
        private IPostgresSQL_Connection _db_action;
        public ArtworkCombineController(ILogger<ArtworkCombineController> logger, IConfiguration configuration, IPostgresSQL_Connection db_action)
        {
            _db_action = db_action;
            _configuration = configuration;
            _logger = logger;
        }

        #region Get
        [HttpGet]
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
        public IActionResult Post(PostRequest request)
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
                        var nsfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", nsfw_art.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_url", nsfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_name", nsfw_art.ImgName, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_describe", nsfw_art.ImgDescribe, DbType.String));

                        string nsfw_art_query = $"INSERT INTO  master.sfw_art (id, img_nsfw_url, img_nsfw_name, img_nsfw_describe)VALUES(:id, :img_nsfw_url, :img_nsfw_name, :img_nsfw_describe);";
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

        #region Put
        [HttpPut("put")]
        public IActionResult Put(PutRequest request)
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
                        list_param.Add(_db_action.ParamMaker("img_url", sfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_name", sfw_art.ImgUrl, DbType.String));
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
                        var nsfw_art = JsonConvert.DeserializeObject<SfwArt>(request.Data.ToString());
                        list_param.Add(_db_action.ParamMaker("id", nsfw_art.Id, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_url", nsfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_name", nsfw_art.ImgUrl, DbType.String));
                        list_param.Add(_db_action.ParamMaker("img_nsfw_describe", nsfw_art.ImgDescribe, DbType.String));

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
                message = "Insert image failed\n\r" + ex;
            }

            return Ok(message);
        }
        #endregion

        #region Delete
        [HttpDelete("del")]
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
    }
}
