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

        public class UserSettingsDto
        {
            [StringLength(50, ErrorMessage = "Display name cannot exceed 50 characters.")]
            public string? DisplayName { get; set; }
        }

        // GET: api/settings
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                _logger.LogInformation("GetSettings called");
                
                // Get user ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found in token claims");
                    return Unauthorized(new { message = "Invalid token" });
                }

                _logger.LogInformation("Looking for user with ID: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return NotFound(new { message = "User not found." });
                }

                _logger.LogInformation("Found user: {UserId}, DisplayName: {DisplayName}", user.Id, user.DisplayName);

                var settings = new UserSettingsDto
                {
                    DisplayName = user.DisplayName ?? ""
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user settings");
                return StatusCode(500, new { message = "An error occurred while retrieving settings.", error = ex.Message });
            }
        }

        // PUT: api/settings
        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsDto settingsDto)
        {
            try
            {
                _logger.LogInformation("UpdateSettings called with data: {@SettingsDto}", settingsDto);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new { message = "Invalid data", errors = ModelState });
                }

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

                _logger.LogInformation("Updating settings for user: {UserId}", user.Id);

                // Update user properties
                user.DisplayName = settingsDto.DisplayName?.Trim();

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to update user settings for user {UserId}: {Errors}",
                        user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to update settings.", errors = result.Errors.Select(e => e.Description) });
                }

                _logger.LogInformation("Successfully updated settings for user {UserId}", user.Id);
                return Ok(new { message = "Settings updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                return StatusCode(500, new { message = "An error occurred while updating settings.", error = ex.Message });
            }
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