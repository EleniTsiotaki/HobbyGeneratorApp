using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using HobbyGeneratorAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HobbyGeneratorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // Get current user (for role check)
        [HttpGet("current")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found for authenticated request.");
                    return NotFound("User not found.");
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("Fetched roles for user {UserId}: {Roles}", user.Id, string.Join(", ", roles));

                return Ok(new
                {
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    roles = roles ?? new List<string>() // Use lowercase 'roles'
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUser");
                return StatusCode(500, "An error occurred while fetching user.");
            }
        }

        // Admin: Get all users
        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        isAdmin = _userManager.IsInRoleAsync(u, "Admin").Result
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsers");
                return StatusCode(500, "An error occurred while fetching users.");
            }
        }

        // Admin: Delete a user
        [HttpDelete("admin/users/{id}")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound("User not found.");

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return BadRequest("Cannot delete admin users.");

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);

                return Ok("User deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUser for user id {Id}", id);
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }
    }
}