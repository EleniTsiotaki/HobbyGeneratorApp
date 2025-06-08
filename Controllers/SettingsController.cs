using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HobbyGeneratorAPI.Models;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace HobbyGeneratorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SettingsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            ILogger<SettingsController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // DELETE: api/settings/account
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                _logger.LogInformation("DeleteAccount called");

                // Get user ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found in token claims");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return NotFound(new { message = "User not found." });
                }

                // Check if user is admin (prevent admin deletion)
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    _logger.LogWarning("Attempt to delete admin account: {UserId}", userId);
                    return BadRequest(new { message = "Admin accounts cannot be deleted." });
                }

                _logger.LogInformation("Deleting account for user: {UserId}", user.Id);

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to delete user account for user {UserId}: {Errors}",
                        user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to delete account.", errors = result.Errors.Select(e => e.Description) });
                }

                _logger.LogInformation("Successfully deleted account for user {UserId}", user.Id);
                return Ok(new { message = "Account deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account");
                return StatusCode(500, new { message = "An error occurred while deleting the account.", error = ex.Message });
            }
        }

        // Test endpoint
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new { message = "Settings controller is working", timestamp = DateTime.UtcNow });
        }
    }
}