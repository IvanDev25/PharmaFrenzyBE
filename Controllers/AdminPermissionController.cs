using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Api.Services;
using Api.DTOs.Account;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminPermissionController : ControllerBase
    {
        private readonly ICustomAdminPermissionAsync _customAdminPermissionAsync;

        public AdminPermissionController(ICustomAdminPermissionAsync customAdminPermissionAsync)
        {
            _customAdminPermissionAsync = customAdminPermissionAsync;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdminPermission(AdminPermission adminPermissionDto)
        {
            try
            {
                var item = await _customAdminPermissionAsync.CreateAdminPermission(adminPermissionDto);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllAdminPermission()
        {
            try
            {
                var result = await _customAdminPermissionAsync.GetAllAdminPermission();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAdminPermissionByUserId(string userId)
        {
            try
            {
                var result = await _customAdminPermissionAsync.GetAdminPermissions(userId);

                if (result == null || string.IsNullOrEmpty(result.UserId))
                {
                    return NotFound($"Admin permission not found for userId: {userId}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
