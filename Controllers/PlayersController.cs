using Api.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Api.Services;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ICustomPlayerServiceAsync _customPlayerServiceAsync;

        public PlayersController(ICustomPlayerServiceAsync customPlayerServiceAsync)
        {
            _customPlayerServiceAsync = customPlayerServiceAsync;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlayers([FromBody] List<Player> playersDto)
        {
            try
            {
                var result = await _customPlayerServiceAsync.CreatePlayers(playersDto);

                if (result.HasError)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            try
            {
                var response = await _customPlayerServiceAsync.DeletePlayer(id);
                if (response.HasError)
                    return NotFound(response.ErrorMessage);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



    }
}
