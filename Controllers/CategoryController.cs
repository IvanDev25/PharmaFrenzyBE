using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Api.Services;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICustomCategoryServiceAsync _customCategoryServiceAsync;

        public CategoryController(ICustomCategoryServiceAsync customCategoryServiceAsync)
        {
            _customCategoryServiceAsync = customCategoryServiceAsync;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategory()
        {
            try
            {
                var response = await _customCategoryServiceAsync.GetAllCategory();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category categoryDto)
        {
            try
            {
                var item = await _customCategoryServiceAsync.CreateCategory(categoryDto);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                var result = await _customCategoryServiceAsync.GetCategoryById(id);

                if (result == null)
                    return NotFound($"Category with ID {id} not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category categoryDto)
        {
            try
            {
                if (id != categoryDto.Id)
                {
                    return BadRequest("Category ID mismatch.");
                }

                var response = await _customCategoryServiceAsync.UpdateCategory(categoryDto);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var response = await _customCategoryServiceAsync.DeleteCategory(id);
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
