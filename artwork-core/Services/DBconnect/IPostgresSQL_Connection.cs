using Npgsql;
using System.Data;

namespace ArtworkCore.Services.DBconnect
{
    public interface IPostgresSQL_Connection
    {
        NpgsqlConnection Connection();
        NpgsqlParameter ParamMaker(string key, dynamic value, DbType type);
    }
}
