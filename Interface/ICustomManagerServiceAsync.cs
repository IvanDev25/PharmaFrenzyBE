using Api.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Web.Response;

namespace Api.Interface
{
    public interface ICustomManagerServiceAsync
    {
        Task<IEnumerable<Manager>> GetAllManager();
        Task<Response<int>> CreateManager(Manager managerDto);
        Task<Manager> GetManagerById(int id);
        Task<Response> UpdateManager(Manager managerDto);
        Task<Response> DeleteManager(int id);
        Task<IEnumerable<Player>> GetPlayerByManagerId(int managerId);
    }
}
