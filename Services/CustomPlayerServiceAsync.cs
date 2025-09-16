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
    public class CustomPlayerServiceAsync : ICustomPlayerServiceAsync
    {

        private readonly IDapperServiceAsync _dapperServiceAsync;


        public CustomPlayerServiceAsync(IDapperServiceAsync dapperServiceAsync)
        {
            _dapperServiceAsync = dapperServiceAsync;
        }

        public async Task<Response<string>> CreatePlayers(List<Player> playersDto)
        {
            if (playersDto == null || playersDto.Count == 0)
            {
                return new Response<string>(null, "No players to add.");
            }

            var insertQuery = @"
        INSERT INTO Players (Name, Age, PhoneNumber, ManagerId)
        VALUES (@Name, @Age, @PhoneNumber, @ManagerId)";

            try
            {
                var result = await _dapperServiceAsync.ExecuteAsync(
                    insertQuery,
                    Connection.LoveBoracayDB,
                    playersDto.Select(player => new
                    {
                        player.Name,
                        player.Age,
                        player.PhoneNumber,
                        player.ManagerId
                    }).ToList(),
                    CommandType.Text
                );

                if (result <= 0)
                {
                    return new Response<string>(null, "Failed to add players.");
                }

                return new Response<string>("Players added successfully.");
            }
            catch (Exception ex)
            {
                return new Response<string>(null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<Response> DeletePlayer(int id)
        {
            try
            {
                string query = @"DELETE FROM Players WHERE Id = @Id;";
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                await _dapperServiceAsync.Delete<Player>(
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

        public Task<Player> GetPlayerById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Response> UpdatePlayer(Player playerDto)
        {
            throw new NotImplementedException();
        }
    }
}
