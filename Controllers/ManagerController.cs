using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Api.Services;
using System.Linq;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private readonly ICustomManagerServiceAsync _customManagerServiceAsync;

        public ManagerController(ICustomManagerServiceAsync customManagerServiceAsync)
        {
            _customManagerServiceAsync = customManagerServiceAsync;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllManager()
        {
            try
            {
                var response = await _customManagerServiceAsync.GetAllManager();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateManager(Manager managerDto)
        {
            try
            {
                var item = await _customManagerServiceAsync.CreateManager(managerDto);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetManagerById(int id)
        {
            try
            {
                var result = await _customManagerServiceAsync.GetManagerById(id);

                if (result == null)
                    return NotFound($"Manager with ID {id} not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateManager(int id, Manager managerDto)
        {
            try
            {
                if (id != managerDto.Id)
                {
                    return BadRequest("Team ID mismatch.");
                }

                var response = await _customManagerServiceAsync.UpdateManager(managerDto);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManager(int id)
        {
            try
            {
                var response = await _customManagerServiceAsync.DeleteManager(id);
                if (response.HasError)
                    return NotFound(response.ErrorMessage);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{managerId}/players")]
        public async Task<IActionResult> GetPlayersByManagerId(int managerId)
        {
            try
            {
                var players = await _customManagerServiceAsync.GetPlayerByManagerId(managerId);

                if (players == null || !players.Any())
                    return NotFound($"No players found for Manager ID {managerId}.");

                return Ok(players);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }
}
