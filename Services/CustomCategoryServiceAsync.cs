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
    public class CustomCategoryServiceAsync : ICustomCategoryServiceAsync
    {
        private readonly IDapperServiceAsync _dapperServiceAsync;


        public CustomCategoryServiceAsync(IDapperServiceAsync dapperServiceAsync)
        {
            _dapperServiceAsync = dapperServiceAsync;
        }

        public async Task<Response<int>> CreateCategory(Category categoryDto)
        {
            try
            {
                string query = @"
            INSERT INTO Categories (CategoryName)
            VALUES (@CategoryName);
            SELECT SCOPE_IDENTITY();";

                var parameters = new
                {
                    CategoryName = categoryDto.CategoryName
                };

                var categoryId = await _dapperServiceAsync.ExecuteScalarAsync<int>(
                    query,
                    parameters,
                    CommandType.Text
                );

                if (categoryId > 0)
                {
                    categoryDto.Id = categoryId;
                    return new Response<int>(categoryId);
                }

                return new Response<int>(0, "Failed to create category.");
            }
            catch (Exception ex)
            {
                return new Response<int>(0, $"Error: {ex.Message}");
            }
        }


        public async Task<Response> DeleteCategory(int id)
        {
            try
            {
                string query = @"DELETE FROM Categories WHERE Id = @Id;";
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                await _dapperServiceAsync.Delete<Manager>(
                    query,
                    Connection.LoveBoracayDB,
                    parameters,
                    CommandType.Text
                );

                return new Response(); // Success
            }
            catch (Exception ex)
            {
                return new Response($"Error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Category>> GetAllCategory()
        {
            string query = @"SELECT
                            c.Id,
                            c.CategoryName
                            FROM Categories c ";

            var result = await _dapperServiceAsync.GetAll<Category>(
                query,
                Connection.LoveBoracayDB,
                null,
                CommandType.Text
            );
            return result;
        }

        public async Task<Category> GetCategoryById(int id)
        {
            string query = @"SELECT
                            c.Id,
                            c.CategoryName
                            FROM Categories c
                            WHERE Id = @Id";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var result = await _dapperServiceAsync.Get<Category>(
                query,
                Connection.LoveBoracayDB, // Adjust to your actual connection name
                parameters,
                CommandType.Text
            );
            return result;
        }

        public async Task<Response> UpdateCategory(Category categoryDto)
        {
            try
            {
                string query = @"
            UPDATE Categories
            SET CategoryName = @CategoryName
            WHERE Id = @Id;";

                var parameters = new DynamicParameters();
                parameters.Add("@Id", categoryDto.Id);
                parameters.Add("@CategoryName", categoryDto.CategoryName);

                int rowsAffected = await _dapperServiceAsync.ExecuteAsync(
                    query,
                    Connection.LoveBoracayDB, // Make sure this matches your actual connection enum/key
                    parameters,
                    CommandType.Text
                );

                if (rowsAffected > 0)
                {
                    return new Response(); // success
                }

                return new Response("No category was updated. Invalid ID?");
            }
            catch (Exception ex)
            {
                return new Response($"Error: {ex.Message}");
            }
        }

    }
}
