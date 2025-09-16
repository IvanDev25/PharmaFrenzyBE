using Api.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Api.Constant;
using Api.DTOs.Account;
using Microsoft.AspNetCore.Identity;
using Api.Models;
using Dapper;
using Api.Web.Response;
using System.Linq;
using Api.ViewModel;
using System;



namespace Api.Services
{
    public class CustomAdminPermissionAsync : ICustomAdminPermissionAsync
    {
        private readonly IDapperServiceAsync _dapperServiceAsync;


        public CustomAdminPermissionAsync(IDapperServiceAsync dapperServiceAsync)
        {
            _dapperServiceAsync = dapperServiceAsync;
        }
        public async Task<Response> CreateAdminPermission(AdminPermission adminPermissionDto)
        {
            try
            {
                string query = @"
        INSERT INTO AdminPermissions (
            UserId, PlayerManagement, AdminManagement, ManagerManagement, 
            CategoryManagement, TeamManagement, AccessEndDate)
        VALUES (
            @UserId, @PlayerManagement, @AdminManagement, @ManagerManagement, 
            @CategoryManagement, @TeamManagement, @AccessEndDate);";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", adminPermissionDto.UserId);
                parameters.Add("@PlayerManagement", adminPermissionDto.PlayerManagement);
                parameters.Add("@AdminManagement", adminPermissionDto.AdminManagement);
                parameters.Add("@ManagerManagement", adminPermissionDto.ManagerManagement);
                parameters.Add("@CategoryManagement", adminPermissionDto.CategoryManagement);
                parameters.Add("@TeamManagement", adminPermissionDto.TeamManagement);
                parameters.Add("@AccessEndDate", adminPermissionDto.AccessEndDate);

                int result = await _dapperServiceAsync.ExecuteAsync(
                    query,
                    Connection.LoveBoracayDB,
                    parameters,
                    CommandType.Text
                );

                return result > 0
                    ? new Response()
                    : new Response("Failed to create admin permission.");
            }
            catch (Exception ex)
            {
                return new Response($"Error: {ex.Message}");
            }
        }

        public async Task<AdminPermission> GetAdminPermissions(string userId)
        {
            try
            {
                string query = @"
            SELECT 
                ap.Id AS AdminPermissionId,
                ap.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                ap.PlayerManagement,
                ap.AdminManagement,
                ap.ManagerManagement,
                ap.CategoryManagement,
                ap.TeamManagement,
                ap.AccessEndDate
            FROM AdminPermissions ap
            INNER JOIN AspNetUsers u ON u.Id = ap.UserId
            WHERE ap.UserId = @UserId";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);

                var data = await _dapperServiceAsync.Get<AdminPermission>(
                    query,
                    Connection.LoveBoracayDB, // Or the correct DB name you're using
                    parameters,
                    CommandType.Text
                );

                return data ?? new AdminPermission();
            }
            catch (Exception)
            {
                return new AdminPermission();
            }
        }


        public async Task<IEnumerable<AdminPermissionViewModel>> GetAllAdminPermission()
        {
            string query = @"
        SELECT 
            ap.Id AS AdminPermissionId,
            ap.UserId,
            u.FirstName,
            u.LastName,
            u.Email,
            ap.PlayerManagement,
            ap.AdminManagement,
            ap.ManagerManagement,
            ap.CategoryManagement,
            ap.TeamManagement,
            ap.AccessEndDate
        FROM AdminPermissions ap
        JOIN AspNetUsers u ON ap.UserId = u.Id";

            var result = await _dapperServiceAsync.GetAll<AdminPermissionViewModel>(
                query,
                Connection.LoveBoracayDB,
                null,
                CommandType.Text
            );

            return result;
        }



        public Task<Response> UpdateAdminPermission(AdminPermission adminPermissionDto)
        {
            throw new NotImplementedException();
        }
    }
}
