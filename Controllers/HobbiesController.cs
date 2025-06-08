using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HobbyGeneratorAPI.Data;
using HobbyGeneratorAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using HobbyGeneratorAPI.Services;
using HobbyGeneratorAPI.Models.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HobbyGeneratorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HobbiesController : ControllerBase
    {
        private readonly HobbyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HobbiesController> _logger;
        private readonly HobbySeederService _hobbySeeder;

        public HobbiesController(
            HobbyDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HobbiesController> logger,
            HobbySeederService hobbySeeder)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hobbySeeder = hobbySeeder;
        }

        // GET api/hobbies/random
        // Returns a single random hobby or a paginated list of random hobbies
        [HttpGet("random")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRandomHobbies(
            [FromQuery] bool single = false,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var userHobbyIds = await _context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == user.Id))
                    .Select(h => h.Id)
                    .ToListAsync();

                var query = _context.Hobbies
                    .Include(h => h.Users)
                    .Where(h => !userHobbyIds.Contains(h.Id));

                if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
                {
                    query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                                            h.Description.ToLower().Contains(searchLower));
                }

                if (!await query.AnyAsync())
                {
                    query = _context.Hobbies.Include(h => h.Users).AsQueryable();
                }

                if (!await query.AnyAsync())
                    return NotFound("No hobbies found.");

                if (single)
                {
                    var singleHobbies = await query
                        .Select(h => new
                        {
                            h.Id,
                            h.Name,
                            h.Description,
                            h.Link,
                            h.Type,
                            h.ImageUrl,
                            FollowersCount = h.Users.Count,
                            IsFollowing = h.Users.Any(u => u.Id == user.Id)
                        })
                        .ToListAsync();

                    var random = new Random();
                    var hobby = singleHobbies[random.Next(singleHobbies.Count)];

                    return Ok(hobby);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var allHobbies = await query
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.Description,
                        h.Link,
                        h.Type,
                        h.ImageUrl,
                        FollowersCount = h.Users.Count,
                        IsFollowing = false
                    })
                    .ToListAsync();

                var randomGen = new Random();
                var hobbies = allHobbies
                    .OrderBy(x => randomGen.Next())
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    hobbies,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRandomHobbies");
                return StatusCode(500, "An error occurred while fetching random hobbies");
            }
        }

        // GET api/hobbies/recommendations
        // Returns paginated hobbies based on user's favorite categories
        [HttpGet("recommendations")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRecommendedHobbies(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var userHobbyIds = await _context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == user.Id))
                    .Select(h => h.Id)
                    .ToListAsync();

                // Get all favorite categories with the highest follow count
                var categoryCounts = await _context.Hobbies
                    .Where(h => userHobbyIds.Contains(h.Id) && !string.IsNullOrEmpty(h.Type))
                    .GroupBy(h => h.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var maxCount = categoryCounts.Any() ? categoryCounts.Max(c => c.Count) : 0;
                var favoriteCategories = categoryCounts
                    .Where(c => c.Count == maxCount)
                    .Select(c => c.Type.ToLower())
                    .ToList();

                var query = _context.Hobbies
                    .Include(h => h.Users)
                    .Where(h => !userHobbyIds.Contains(h.Id));

                if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
                {
                    query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                                            h.Description.ToLower().Contains(searchLower));
                }

                var totalCount = await query.CountAsync();
                var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

                IOrderedQueryable<Hobby> orderedQuery;
                if (favoriteCategories.Any())
                {
                    orderedQuery = query
                        .OrderByDescending(h => h.Type != null && favoriteCategories.Contains(h.Type.ToLower()))
                        .ThenByDescending(h => h.Users.Count)
                        .ThenBy(h => h.Name);
                }
                else
                {
                    orderedQuery = query
                        .OrderByDescending(h => h.Users.Count)
                        .ThenBy(h => h.Name);
                }

                var recommendations = await orderedQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.Description,
                        h.Link,
                        h.Type,
                        h.ImageUrl,
                        FollowersCount = h.Users.Count,
                        IsFollowing = false,
                        IsFavoriteCategory = favoriteCategories.Any() && h.Type != null && favoriteCategories.Contains(h.Type.ToLower())
                    })
                    .ToListAsync();

                return Ok(new
                {
                    hobbies = recommendations,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    },
                    debug = new
                    {
                        userFollowsCount = userHobbyIds.Count,
                        favoriteCategories
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendedHobbies");
                return StatusCode(500, "An error occurred while fetching recommendations");
            }
        }

        // POST api/hobbies/admin/seed
        [HttpPost("admin/seed")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                var (seededHobbies, hobbyCount, seededAdmin) = await _hobbySeeder.SeedDataAsync();
                return Ok(new
                {
                    Message = seededHobbies || seededAdmin
                        ? $"Successfully seeded {hobbyCount} hobbies and {(seededAdmin ? "admin user/role" : "no new admin")}."
                        : $"No seeding needed. {hobbyCount} hobbies already exist.",
                    SeededHobbies = seededHobbies,
                    HobbyCount = hobbyCount,
                    SeededAdmin = seededAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return StatusCode(500, new { Message = $"Error seeding database: {ex.Message}" });
            }
        }

        // GET api/hobbies/my
        [HttpGet("my")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetMyHobbies()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobbies = await _context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == user.Id))
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.Description,
                        h.Link,
                        h.Type,
                        h.ImageUrl,
                        FollowersCount = h.Users.Count,
                        IsFollowing = true
                    })
                    .ToListAsync();

                return Ok(hobbies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyHobbies");
                return StatusCode(500, "An error occurred while fetching your hobbies");
            }
        }

        // GET api/hobbies/{id}
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetHobbyById(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobby = await _context.Hobbies
                    .Include(h => h.Users)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hobby == null)
                    return NotFound();

                var isFollowing = hobby.Users.Any(u => u.Id == user.Id);

                return Ok(new
                {
                    hobby.Id,
                    hobby.Name,
                    hobby.Description,
                    hobby.Link,
                    hobby.Type,
                    hobby.ImageUrl,
                    FollowersCount = hobby.Users.Count,
                    IsFollowing = isFollowing
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHobbyById for id {Id}", id);
                return StatusCode(500, "An error occurred while fetching the hobby");
            }
        }

        // POST api/hobbies/{id}/follow
        [HttpPost("{id}/follow")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> FollowHobby(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobby = await _context.Hobbies
                    .Include(h => h.Users)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hobby == null)
                    return NotFound();

                if (!hobby.Users.Any(u => u.Id == user.Id))
                {
                    hobby.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Hobby followed successfully.",
                    followersCount = hobby.Users.Count,
                    isFollowing = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FollowHobby for id {Id}", id);
                return StatusCode(500, "An error occurred while following the hobby");
            }
        }

        // DELETE api/hobbies/{id}/unfollow
        [HttpDelete("{id}/unfollow")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UnfollowHobby(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobby = await _context.Hobbies
                    .Include(h => h.Users)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hobby == null)
                    return NotFound();

                var userToRemove = hobby.Users.FirstOrDefault(u => u.Id == user.Id);
                if (userToRemove != null)
                {
                    hobby.Users.Remove(userToRemove);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Hobby unfollowed successfully.",
                    followersCount = hobby.Users.Count,
                    isFollowing = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UnfollowHobby for id {Id}", id);
                return StatusCode(500, "An error occurred while unfollowing the hobby");
            }
        }

        // GET api/hobbies/discover
        [HttpGet("discover")]
        public async Task<IActionResult> DiscoverHobbies(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var query = _context.Hobbies
                    .Include(h => h.Users)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(category) && category != "all")
                {
                    query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                                            h.Description.ToLower().Contains(searchLower));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var hobbies = await query
                    .OrderBy(h => h.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.Description,
                        h.Link,
                        h.Type,
                        h.ImageUrl,
                        FollowersCount = h.Users.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    hobbies,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscoverHobbies");
                return StatusCode(500, "An error occurred while fetching hobbies");
            }
        }

        // GET api/hobbies/activity
        [HttpGet("activity")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 5)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var recentPosts = await _context.ForumPosts
                    .Where(p => p.UserId == user.Id || p.Hobby.Users.Any(u => u.Id == user.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .Select(p => new
                    {
                        type = "ForumPost",
                        id = p.Id,
                        content = p.Content,
                        hobbyId = p.HobbyId,
                        hobbyName = p.Hobby.Name,
                        createdAt = p.CreatedAt
                    })
                    .ToListAsync();

                var recentFollows = await _context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == user.Id))
                    .OrderByDescending(h => h.Users.Max(u => u.Id == user.Id ? 1 : 0))
                    .Take(limit)
                    .Select(h => new
                    {
                        type = "HobbyFollow",
                        id = h.Id,
                        content = $"Followed hobby: {h.Name}",
                        hobbyId = h.Id,
                        hobbyName = h.Name,
                        createdAt = DateTime.UtcNow
                    })
                    .ToListAsync();

                var activities = recentPosts
                    .Concat(recentFollows)
                    .OrderByDescending(a => a.createdAt)
                    .Take(limit)
                    .ToList();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentActivity");
                return StatusCode(500, "An error occurred while fetching activity.");
            }
        }

        // GET api/hobbies/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Hobbies
                    .Where(h => !string.IsNullOrEmpty(h.Type))
                    .GroupBy(h => h.Type)
                    .Select(g => new
                    {
                        name = g.Key,
                        count = g.Count()
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories");
                return StatusCode(500, "An error occurred while fetching categories");
            }
        }

        // GET api/hobbies/{id}/forum
        [HttpGet("{id}/forum")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetForumPosts(int id)
        {
            try
            {
                var hobby = await _context.Hobbies
                    .Include(h => h.ForumPosts)
                        .ThenInclude(fp => fp.User)
                    .Include(h => h.ForumPosts)
                        .ThenInclude(fp => fp.Replies)
                            .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hobby == null)
                    return NotFound("Hobby not found.");

                var posts = hobby.ForumPosts
                    .Where(fp => fp.ParentPostId == null)
                    .OrderByDescending(fp => fp.CreatedAt)
                    .Select(fp => new
                    {
                        fp.Id,
                        fp.Content,
                        fp.CreatedAt,
                        UserName = fp.User.UserName,
                        UserId = fp.User.Id,
                        Replies = fp.Replies
                            .OrderBy(r => r.CreatedAt)
                            .Select(r => new
                            {
                                r.Id,
                                r.Content,
                                r.CreatedAt,
                                UserName = r.User.UserName,
                                UserId = r.User.Id
                            })
                            .ToList()
                    })
                    .ToList();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetForumPosts for hobby id {Id}", id);
                return StatusCode(500, "An error occurred while fetching forum posts");
            }
        }

        // POST api/hobbies/{id}/forum
        [HttpPost("{id}/forum")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateForumPost(int id, [FromBody] ForumPostDto postDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobby = await _context.Hobbies
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hobby == null)
                    return NotFound("Hobby not found.");

                var forumPost = new ForumPost
                {
                    Content = postDto.Content,
                    UserId = user.Id,
                    HobbyId = id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ForumPosts.Add(forumPost);
                await _context.SaveChangesAsync();

                var responseDto = new ForumPostDto
                {
                    Id = forumPost.Id,
                    HobbyId = forumPost.HobbyId,
                    UserId = forumPost.UserId,
                    Content = forumPost.Content,
                    CreatedAt = forumPost.CreatedAt
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateForumPost for hobby id {Id}", id);
                return StatusCode(500, "An error occurred while creating the forum post");
            }
        }

        // POST api/hobbies/{hobbyId}/forum/{postId}/reply
        [HttpPost("{hobbyId}/forum/{postId}/reply")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateReply(int hobbyId, int postId, [FromBody] ForumPostDto postDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var hobby = await _context.Hobbies
                    .FirstOrDefaultAsync(h => h.Id == hobbyId);
                if (hobby == null)
                    return NotFound("Hobby not found.");

                var parentPost = await _context.ForumPosts
                    .FirstOrDefaultAsync(fp => fp.Id == postId && fp.HobbyId == hobbyId);
                if (parentPost == null)
                    return NotFound("Parent post not found.");

                var reply = new ForumPost
                {
                    Content = postDto.Content,
                    UserId = user.Id,
                    HobbyId = hobbyId,
                    ParentPostId = postId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ForumPosts.Add(reply);
                await _context.SaveChangesAsync();

                var responseDto = new ForumPostDto
                {
                    Id = reply.Id,
                    HobbyId = reply.HobbyId,
                    UserId = reply.UserId,
                    Content = reply.Content,
                    CreatedAt = reply.CreatedAt
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateReply for hobby id {HobbyId}, post id {PostId}", hobbyId, postId);
                return StatusCode(500, "An error occurred while creating the reply");
            }
        }
    }
}