using Api.Interface;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Services
{
    public class DapperServiceAsync : IDapperServiceAsync
    {
        private readonly IConfiguration _config;

        public DapperServiceAsync(IConfiguration config)
        {
            _config = config;
        }

        public async Task<T> Get<T>(string sp, string cn, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = new SqlConnection(_config.GetConnectionString(cn));
            var result = await db.QueryAsync<T>(sp, parms, commandType: commandType);
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> GetAll<T>(string sp, string cn, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = new SqlConnection(_config.GetConnectionString(cn));
            var result = await db.QueryAsync<T>(sp, parms, commandTimeout: 600, commandType: commandType);
            return result.ToList();
        }

        public async Task Insert<T>(string sp, string cn, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = new SqlConnection(_config.GetConnectionString(cn));
            var result = await db.ExecuteAsync(sp, parms, commandType: commandType);
        }

        public async Task Update<T>(string sp, string cn, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = new SqlConnection(_config.GetConnectionString(cn));
            var result = await db.ExecuteAsync(sp, parms, commandType: commandType);
        }

        public async Task Delete<T>(string sp, string cn, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = new SqlConnection(_config.GetConnectionString(cn));
            var result = await db.ExecuteAsync(sp, parms, commandType: commandType);
        }

        public async Task<int> ExecuteAsync(string query, string connectionName, object parameters, CommandType commandType = CommandType.Text)
        {
            using var db = new SqlConnection(_config.GetConnectionString(connectionName));
            var result = await db.ExecuteAsync(query, parameters, commandType: commandType);
            return result;
        }

        public async Task<T> QuerySingleAsync<T>(string query, string connectionName, object parameters, CommandType commandType = CommandType.Text)
        {
            using var db = new SqlConnection(_config.GetConnectionString(connectionName));
            return await db.QuerySingleOrDefaultAsync<T>(query, parameters, commandType: commandType);
        }

        public async Task<T> ExecuteScalarAsync<T>(string query, object parameters = null, CommandType commandType = CommandType.Text)
        {
            using var db = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var result = await db.ExecuteScalarAsync<T>(query, parameters, commandType: commandType);
            return result;
        }


    }
}
