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
    public class CustomManagerServiceAsync : ICustomManagerServiceAsync
    {

        private readonly IDapperServiceAsync _dapperServiceAsync;


        public CustomManagerServiceAsync(IDapperServiceAsync dapperServiceAsync)
        {
            _dapperServiceAsync = dapperServiceAsync;
        }

        public async Task<Response<int>> CreateManager(Manager managerDto)
        {
            try
            {
                string query = @"
            INSERT INTO Managers (Name, Age, PhoneNumber, Email)
            VALUES (@Name, @Age, @PhoneNumber, @Email);
            SELECT SCOPE_IDENTITY();";

                var parameters = new
                {
                    Name = managerDto.Name,
                    Age = managerDto.Age,
                    PhoneNumber = managerDto.PhoneNumber,
                    Email = managerDto.Email
                };

                var managerId = await _dapperServiceAsync.ExecuteScalarAsync<int>(
                    query,
                    parameters,
                    CommandType.Text
                );

                if (managerId > 0)
                {
                    managerDto.Id = managerId;
                    return new Response<int>(managerId); // ✅ return ID in generic Response
                }

                return new Response<int>(0, "Failed to create manager.");
            }
            catch (Exception ex)
            {
                return new Response<int>(0, $"Error: {ex.Message}");
            }
        }


        public async Task<IEnumerable<Manager>> GetAllManager()
        {
            string query = @"SELECT
                        m.Id,
                        m.Name,
                        m.Age,
                        m.PhoneNumber,
                        m.Email
                    FROM Managers m;";

            var result = await _dapperServiceAsync.GetAll<Manager>(
                query,
                Connection.LoveBoracayDB,
                null,
                CommandType.Text
            );

            return result;
        }


        public async Task<Manager> GetManagerById(int id)
        {
            string query = @"
        SELECT 
            Id,
            Name,
            Age,
            PhoneNumber,
            Email
        FROM Managers
        WHERE Id = @Id;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var result = await _dapperServiceAsync.Get<Manager>(
                query,
                Connection.LoveBoracayDB, // Adjust to your actual connection name
                parameters,
                CommandType.Text
            );

            return result;
        }


        public async Task<Response> UpdateManager(Manager managerDto)
        {
            try
            {
                string query = @"
            UPDATE Managers
            SET Name = @Name,
                Age = @Age,
                PhoneNumber = @PhoneNumber,
                Email = @Email
            WHERE Id = @Id;";

                var parameters = new DynamicParameters();
                parameters.Add("@Id", managerDto.Id);
                parameters.Add("@Name", managerDto.Name);
                parameters.Add("@Age", managerDto.Age);
                parameters.Add("@PhoneNumber", managerDto.PhoneNumber);
                parameters.Add("@Email", managerDto.Email);

                int rowsAffected = await _dapperServiceAsync.ExecuteAsync(
                    query,
                    Connection.LoveBoracayDB,
                    parameters,
                    CommandType.Text
                );

                if (rowsAffected > 0)
                {
                    return new Response(); // success
                }

                return new Response("No manager was updated. Invalid ID?");
            }
            catch (Exception ex)
            {
                return new Response($"Error: {ex.Message}");
            }
        }

        public async Task<Response> DeleteManager(int id)
        {
            try
            {
                string query = @"DELETE FROM Managers WHERE Id = @Id;";
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

        public async Task<IEnumerable<Player>> GetPlayerByManagerId(int managerId)
        {
            string query = @"
        SELECT 
            Id,
            Name,
            Age,
            PhoneNumber,
            ManagerId
        FROM Players
        WHERE ManagerId = @ManagerId;";

            var parameters = new DynamicParameters();
            parameters.Add("@ManagerId", managerId);

            var players = await _dapperServiceAsync.GetAll<Player>(
                query,
                Connection.LoveBoracayDB,
                parameters,
                CommandType.Text
            );

            return players;
        }



    }
}
