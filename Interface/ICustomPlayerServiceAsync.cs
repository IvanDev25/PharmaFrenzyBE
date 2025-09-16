using Api.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Web.Response;


namespace Api.Interface
{
    public interface ICustomPlayerServiceAsync
    {
        Task<Response<string>> CreatePlayers(List<Player> playersDto);
        Task<Player> GetPlayerById(int id);
        Task<Response> UpdatePlayer(Player playerDto);
        Task<Response> DeletePlayer(int id);
    }
}
