using Api.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Web.Response;

namespace Api.Interface
{
    public interface ICustomEventServiceAsync
    {
        Task<IEnumerable<Event>> GetAllEvent();
        Task<Response<int>> CreateEvent(Event eventDto);
        Task<Category> GetEventById(int id);
        Task<Response> UpdateEvent(Event eventDto);
        Task<Response> DeleteEvent(int id);
    }
}
