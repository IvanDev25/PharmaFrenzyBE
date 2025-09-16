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
    public class CustomTeamServiceAsync : ICustomTeamServiceAsync
    {
        private readonly IDapperServiceAsync _dapperServiceAsync;
        private readonly EmailService _emailService;
        private readonly UserManager<User> _userManager;

        public CustomTeamServiceAsync(IDapperServiceAsync dapperServiceAsync,
            EmailService emailService,
            UserManager<User> userManager
            )
        {
            _dapperServiceAsync = dapperServiceAsync;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<IEnumerable<TeamViewModel>> GetAllTeam()
        {
            string query = @"SELECT
                            t.Id,
                            t.TeamName,
                            t.ManagerId,
                            m.Name AS ManagerName,
                            t.CategoryId,
                            c.CategoryName,
                            t.Status
                            FROM Teams t
                            LEFT JOIN Managers m ON t.ManagerId = m.Id
                            LEFT JOIN Categories c ON t.CategoryId = c.Id;";

            var result = await _dapperServiceAsync.GetAll<TeamViewModel>(
                query,
                Connection.LoveBoracayDB,
                null,
                CommandType.Text
            );

            return result;
        }

        public async Task<TeamViewModel> GetTeamById(int id)
        {
            string query = @"SELECT
                        t.Id,
                        t.TeamName,
                        t.ManagerId,
                        m.Name AS ManagerName,
                        t.CategoryId,
                        c.CategoryName,
                        t.Status
                     FROM Teams t
                     LEFT JOIN Managers m ON t.ManagerId = m.Id
                     LEFT JOIN Categories c ON t.CategoryId = c.Id
                     WHERE t.Id = @Id;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var result = await _dapperServiceAsync.Get<TeamViewModel>(
                query,
                Connection.LoveBoracayDB,
                parameters,
                CommandType.Text
            );

            return result;
        }


        public async Task<Response> CreateTeam(Team teamDto)
        {
            string insertQuery = @"
        INSERT INTO Teams (TeamName, ManagerId, CategoryId, Status)
        VALUES (@TeamName, @ManagerId, @CategoryId, @Status)";

            var parameters = new
            {
                TeamName = teamDto.TeamName,
                ManagerId = teamDto.ManagerId,
                CategoryId = teamDto.EventId,
                Status = "Pending" // Status is always set to Pending
            };

            var result = await _dapperServiceAsync.ExecuteAsync(
                insertQuery,
                Connection.LoveBoracayDB,
                parameters,
                CommandType.Text
            );

            if (result > 0)
            {
                // Fetch manager
                string managerQuery = "SELECT * FROM Managers WHERE Id = @Id";
                var manager = await _dapperServiceAsync.Get<Manager>(
                    managerQuery,
                    Connection.LoveBoracayDB,
                    new DynamicParameters(new { Id = teamDto.ManagerId }),
                    CommandType.Text
                );

                // Fetch category name
                string categoryQuery = "SELECT CategoryName FROM Categories WHERE Id = @Id";
                var categoryName = await _dapperServiceAsync.Get<string>(
                    categoryQuery,
                    Connection.LoveBoracayDB,
                    new DynamicParameters(new { Id = teamDto.EventId }),
                    CommandType.Text
                );

                // Fetch players
                string playersQuery = "SELECT Name FROM Players WHERE ManagerId = @ManagerId";
                var players = await _dapperServiceAsync.GetAll<Player>(
                    playersQuery,
                    Connection.LoveBoracayDB,
                    new DynamicParameters(new { ManagerId = teamDto.ManagerId }),
                    CommandType.Text
                );

                if (manager != null)
                {
                    var playerNames = string.Join(", ", players.Select(p => p.Name));
                    var emailBody = $"<p>Hello {manager.Name},</p>" +
                                    "<p>Your team has been successfully created:</p>" +
                                    $"<p>Team Name: {teamDto.TeamName}</p>" +
                                    $"<p>Category: {categoryName}</p>" +
                                    $"<p>Status: Pending</p>" +
                                    $"<p>Players: {playerNames}</p>" +
                                    "<p>Thank you,</p>" +
                                    "<p>Your Team LoveBoracay Management</p>";

                    var emailSendDto = new EmailSendDto(manager.Email, "Team Created Successfully", emailBody);
                    var emailSent = await _emailService.SendEmailAsync(emailSendDto);

                    if (emailSent)
                        return new Response("Team created successfully and email notification sent.");
                    else
                        return new Response("Team created successfully but failed to send email notification.");
                }

                return new Response("Manager not found.");
            }

            return new Response("Failed to create team.");
        }


        public async Task<Response> UpdateTeam(Team teamDto)
        {
            // Get the existing team
            string getQuery = "SELECT * FROM Teams WHERE Id = @Id";
            var existingTeam = await _dapperServiceAsync.Get<Team>(
                getQuery,
                Connection.LoveBoracayDB,
                new DynamicParameters(new { Id = teamDto.Id }),
                CommandType.Text
            );

            if (existingTeam == null)
                return new Response("Team not found.");

            // Update the team
            string updateQuery = @"
        UPDATE Teams 
        SET TeamName = @TeamName, 
            ManagerId = @ManagerId, 
            CategoryId = @CategoryId, 
            Status = @Status 
        WHERE Id = @Id";

            var parameters = new
            {
                Id = teamDto.Id,
                TeamName = teamDto.TeamName,
                ManagerId = teamDto.ManagerId,
                CategoryId = teamDto.EventId,
                Status = teamDto.Status
            };

            var result = await _dapperServiceAsync.ExecuteAsync(
                updateQuery,
                Connection.LoveBoracayDB,
                parameters,
                CommandType.Text
            );

            if (result > 0)
            {
                // Check if status changed to Verified
                if (!string.Equals(existingTeam.Status, teamDto.Status, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(teamDto.Status, "Verified", StringComparison.OrdinalIgnoreCase))
                {
                    // Fetch manager
                    var manager = await _dapperServiceAsync.Get<Manager>(
                        "SELECT * FROM Managers WHERE Id = @Id",
                        Connection.LoveBoracayDB,
                        new DynamicParameters(new { Id = teamDto.ManagerId }),
                        CommandType.Text
                    );

                    // Fetch category name
                    var categoryName = await _dapperServiceAsync.Get<string>(
                        "SELECT CategoryName FROM Categories WHERE Id = @Id",
                        Connection.LoveBoracayDB,
                        new DynamicParameters(new { Id = teamDto.EventId }),
                        CommandType.Text
                    );

                    // Fetch players
                    var players = await _dapperServiceAsync.GetAll<Player>(
                        "SELECT Name FROM Players WHERE ManagerId = @ManagerId",
                        Connection.LoveBoracayDB,
                        new DynamicParameters(new { ManagerId = teamDto.ManagerId }),
                        CommandType.Text
                    );

                    var playerNames = string.Join(", ", players.Select(p => p.Name));
                    var emailBody = $"<p>Hello {manager.Name},</p>" +
                                    "<p>Your team has been verified:</p>" +
                                    $"<p>Team Name: {teamDto.TeamName}</p>" +
                                    $"<p>Category: {categoryName}</p>" +
                                    $"<p>Status: {teamDto.Status}</p>" +
                                    $"<p>Players: {playerNames}</p>" +
                                    "<p>Thank you,</p>" +
                                    "<p>Your Team LoveBoracay Management</p>";

                    var emailSendDto = new EmailSendDto(manager.Email, "Team Verified", emailBody);
                    var emailSent = await _emailService.SendEmailAsync(emailSendDto);

                    if (emailSent)
                        return new Response("Team updated successfully and verification email sent.");
                    else
                        return new Response("Team updated successfully but failed to send email.");
                }

                return new Response("Team updated successfully.");
            }

            return new Response("Failed to update team.");
        }



    }
}
