using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Api.Interface
{
    public interface IDapperServiceAsync
    {
        Task<T> Get<T>(string sp, string cn, DynamicParameters parms, CommandType commandType);
        Task<IEnumerable<T>> GetAll<T>(string sp, string cn, DynamicParameters parms, CommandType commandType);
        Task Insert<T>(string sp, string cn, DynamicParameters parms, CommandType commandType);
        Task Update<T>(string sp, string cn, DynamicParameters parms, CommandType commandType);
        Task<int> ExecuteAsync(string query, string connectionName, object parameters, CommandType commandType);
        Task Delete<T>(string sp, string cn, DynamicParameters parms, CommandType commandType);
        Task<T> QuerySingleAsync<T>(string query, string connectionName, object parameters, CommandType commandType);
        Task<T> ExecuteScalarAsync<T>(string query, object parameters = null, CommandType commandType = CommandType.Text);
    }
}
