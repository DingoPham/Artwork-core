using Npgsql;
using System.Data;

namespace ArtworkCore.Services.DBconnect
{
    internal class PostgresSQL_Connection : IPostgresSQL_Connection
    {
        private string _connectionString = "";

        public PostgresSQL_Connection(string connectString)
        {
            _connectionString = connectString;
        }

        #region Connection 
        public NpgsqlConnection Connection()
        {
            try
            {
                NpgsqlConnection _connect = new NpgsqlConnection(_connectionString);
                return _connect;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new NotImplementedException();
            }
        }
        #endregion

        public NpgsqlParameter ParamMaker(string key, dynamic value, DbType type)
        {
            NpgsqlParameter parameter = new();
            parameter.ParameterName = key;
            parameter.Value = value == null ? null : value;
            parameter.DbType = type;
            return parameter;

        }
    }
}
