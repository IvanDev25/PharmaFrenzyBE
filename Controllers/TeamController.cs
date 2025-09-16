using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly ICustomTeamServiceAsync _customTeamServiceAsync;

        public TeamController(ICustomTeamServiceAsync customTeamServiceAsync)
        {
            _customTeamServiceAsync = customTeamServiceAsync;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllTeam()
        {
            try
            {
                var response = await _customTeamServiceAsync.GetAllTeam();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeamById(int id)
        {
            try
            {
                var result = await _customTeamServiceAsync.GetTeamById(id);

                if (result == null)
                    return NotFound($"Team with ID {id} not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam(Team teamDto)
        {
            try
            {
                var item = await _customTeamServiceAsync.CreateTeam(teamDto);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, Team teamDto)
        {
            try
            {
                if (id != teamDto.Id)
                {
                    return BadRequest("Team ID mismatch.");
                }

                var response = await _customTeamServiceAsync.UpdateTeam(teamDto);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
