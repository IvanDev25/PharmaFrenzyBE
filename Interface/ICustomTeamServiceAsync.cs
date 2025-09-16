using System.Collections.Generic;
using System.Threading.Tasks;
using Api.ViewModel;
using Api.Web.Response;

namespace Api.Interface
{
    public interface ICustomTeamServiceAsync
    {
        Task<IEnumerable<TeamViewModel>> GetAllTeam();
        Task<Response> CreateTeam(Team teamDto);
        Task<TeamViewModel> GetTeamById(int id);
        Task<Response> UpdateTeam(Team teamDto);

    }
}
