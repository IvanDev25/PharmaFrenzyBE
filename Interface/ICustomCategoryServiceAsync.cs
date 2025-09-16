using Api.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Web.Response;

namespace Api.Interface
{
    public interface ICustomCategoryServiceAsync
    {
        Task<IEnumerable<Category>> GetAllCategory();
        Task<Response<int>> CreateCategory(Category categoryDto);
        Task<Category> GetCategoryById(int id);
        Task<Response> UpdateCategory(Category categoryDto);
        Task<Response> DeleteCategory(int id);
    }
}
