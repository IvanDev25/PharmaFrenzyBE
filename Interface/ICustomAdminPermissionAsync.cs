using System.Collections.Generic;
using System.Threading.Tasks;
using Api.DTOs.Account;
using Api.ViewModel;
using Api.Web.Response;

namespace Api.Interface
{
    public interface ICustomAdminPermissionAsync
    {
        Task<Response> CreateAdminPermission(AdminPermission adminPermissionDto);
        Task<IEnumerable<AdminPermissionViewModel>> GetAllAdminPermission();
        Task<Response> UpdateAdminPermission(AdminPermission adminPermissionDto);
        Task<AdminPermission> GetAdminPermissions(string userId);
    }
}
