using HobbyGeneratorAPI.Data;
using HobbyGeneratorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HobbyGeneratorAPI.Models.Dtos;

namespace HobbyGeneratorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
    public class AdminController : ControllerBase
    {
        private readonly HobbyDbContext _context;

        public AdminController(HobbyDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/hobbies
        //GET all hobbies with filtering (search, category) and follower counts.
        [HttpGet("hobbies")]
        public async Task<IActionResult> GetAllHobbies([FromQuery] string? search, [FromQuery] string? category)
        {
            var query = _context.Hobbies.AsQueryable();

            // Apply search filter (case-insensitive)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h => EF.Functions.Like(h.Name.ToLower(), $"%{search.ToLower()}%"));
            }

            // Apply category filter (if not "all")
            if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
            {
                query = query.Where(h => h.Type == category);
            }

            var hobbies = await query
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    h.Description,
                    h.Type,
                    h.Link,
                    h.ImageUrl,
                    FollowersCount = h.Users.Count
                })
                .ToListAsync();

            return Ok(hobbies);
        }

        // POST api/admin/hobbies
        // Create a hobby
        [HttpPost("hobbies")]
        public async Task<IActionResult> CreateHobby([FromBody] HobbyCreateDto dto)
        {
            var hobby = new Hobby
            {
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                Type = dto.Type ?? string.Empty,
                Link = dto.Link ?? string.Empty,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };
            _context.Hobbies.Add(hobby);
            await _context.SaveChangesAsync();
            return Ok(hobby);
        }

        // PUT api/admin/hobbies/{id}
        // UPDATE a hobby
        [HttpPut("hobbies/{id}")]
        public async Task<IActionResult> UpdateHobby(int id, [FromBody] HobbyCreateDto dto)
        {
            var hobby = await _context.Hobbies.FindAsync(id);
            if (hobby == null) return NotFound();
            hobby.Name = dto.Name ?? string.Empty;
            hobby.Description = dto.Description ?? string.Empty;
            hobby.Type = dto.Type ?? string.Empty;
            hobby.Link = dto.Link ?? string.Empty;
            hobby.ImageUrl = dto.ImageUrl ?? string.Empty;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE api/admin/hobbies/{id}
        // DELETE: Delete a hobby
        [HttpDelete("hobbies/{id}")]
        public async Task<IActionResult> DeleteHobby(int id)
        {
            var hobby = await _context.Hobbies.FindAsync(id);
            if (hobby == null) return NotFound();
            _context.Hobbies.Remove(hobby);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET api/admin/users
        // GET: Get all users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    UserName = u.UserName,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .ToList()
                })
                .ToListAsync();
            return Ok(users);
        }

        // DELETE api/admin/users/{id}
        // DELETE: Delete a user and their hobbies / posts
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users
                .Include(u => u.Hobbies)
                .Include(u => u.ForumPosts)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // Remove associated forum posts
            if (user.ForumPosts.Any())
            {
                _context.ForumPosts.RemoveRange(user.ForumPosts);
            }

            // Remove associated hobby followings
            if (user.Hobbies.Any())
            {
                user.Hobbies.Clear();
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Message = $"Failed to delete user due to database constraints: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        // DELETE api/admin/hobbies/{hobbyId}/forum/{postId}
        // DELETE: Delete a forum post + replies
        [HttpDelete("hobbies/{hobbyId}/forum/{postId}")]
        public async Task<IActionResult> DeleteForumPost(int hobbyId, int postId)
        {
            var post = await _context.ForumPosts
                .FirstOrDefaultAsync(p => p.HobbyId == hobbyId && p.Id == postId);
            if (post == null) return NotFound();

            // Remove replies to prevent foreign key constraints
            var replies = await _context.ForumPosts
                .Where(p => p.ParentPostId == postId)
                .ToListAsync();
            if (replies.Any())
            {
                _context.ForumPosts.RemoveRange(replies);
            }

            _context.ForumPosts.Remove(post);
            try
            {
                await _context.SaveChangesAsync();
                var postDto = new ForumPostDto
                {
                    Id = post.Id,
                    HobbyId = post.HobbyId,
                    UserId = post.UserId,
                    Content = post.Content,
                    CreatedAt = post.CreatedAt
                };
                return Ok(postDto);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Message = $"Failed to delete post due to database constraints: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        // GET api/admin/statistics
        // GET: Get statistics (total hobbies, followers, top hobbies, category distribution)
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var totalHobbies = await _context.Hobbies.CountAsync();
            var totalFollowers = await _context.Hobbies.SumAsync(h => h.Users.Count);
            var topHobbies = await _context.Hobbies
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    FollowersCount = h.Users.Count
                })
                .OrderByDescending(h => h.FollowersCount)
                .Take(5)
                .ToListAsync();
            var categories = await _context.Hobbies
                .GroupBy(h => h.Type)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .Where(c => c.Name != null)
                .ToListAsync();

            return Ok(new
            {
                TotalHobbies = totalHobbies,
                TotalFollowers = totalFollowers,
                TopHobbies = topHobbies,
                Categories = categories
            });
        }
    }

}