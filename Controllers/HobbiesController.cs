using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using HobbyGeneratorAPI.Data;
using HobbyGeneratorAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using HobbyGeneratorAPI.Services;
using System;

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

        // Admin-only seeding endpoint
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

        [HttpPost("try")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> TryHobby([FromBody] HobbyDto hobbyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var existingHobby = await _context.Hobbies
                    .Include(h => h.Users)
                    .FirstOrDefaultAsync(h => h.Name == hobbyDto.Name);

                if (existingHobby != null)
                {
                    var alreadyFollowing = existingHobby.Users?.Any(u => u.Id == user.Id) ?? false;
                    if (!alreadyFollowing)
                    {
                        existingHobby.Users ??= new List<ApplicationUser>();
                        existingHobby.Users.Add(user);
                        await _context.SaveChangesAsync();
                    }

                    return Ok(new
                    {
                        existingHobby.Id,
                        existingHobby.Name,
                        existingHobby.Description,
                        existingHobby.Link,
                        existingHobby.Type,
                        existingHobby.ImageUrl,
                        FollowersCount = existingHobby.Users?.Count ?? 0,
                        IsFollowing = true
                    });
                }

                var newHobby = new Hobby
                {
                    Name = hobbyDto.Name,
                    Description = hobbyDto.Description,
                    Link = hobbyDto.Link,
                    Type = hobbyDto.Type,
                    ImageUrl = hobbyDto.ImageUrl,
                    Users = new List<ApplicationUser> { user }
                };

                _context.Hobbies.Add(newHobby);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    newHobby.Id,
                    newHobby.Name,
                    newHobby.Description,
                    newHobby.Link,
                    newHobby.Type,
                    newHobby.ImageUrl,
                    FollowersCount = 1,
                    IsFollowing = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TryHobby");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

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
                    query = query.Where(h => h.Name.ToLower().Contains(search.ToLower()) ||
                                           h.Description.ToLower().Contains(search.ToLower()));
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

        [HttpGet("random")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRandomHobby()
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

                var availableHobbies = await _context.Hobbies
                    .Include(h => h.Users)
                    .Where(h => !userHobbyIds.Contains(h.Id))
                    .ToListAsync();

                if (!availableHobbies.Any())
                {
                    availableHobbies = await _context.Hobbies
                        .Include(h => h.Users)
                        .ToListAsync();
                }

                if (!availableHobbies.Any())
                    return NotFound("No hobbies found.");

                var random = new Random();
                var hobby = availableHobbies[random.Next(availableHobbies.Count)];
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
                _logger.LogError(ex, "Error in GetRandomHobby");
                return StatusCode(500, "An error occurred while fetching a random hobby");
            }
        }

        [HttpGet("personalised")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public async Task<IActionResult> GetPersonalisedHobbies(
    [FromQuery] string? category = null,
    [FromQuery] string? search = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    [FromQuery] bool randomOrder = false)
{
    try
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        // Get user's current hobbies to exclude them
        var userHobbyIds = await _context.Hobbies
            .Where(h => h.Users.Any(u => u.Id == user.Id))
            .Select(h => h.Id)
            .ToListAsync();

        // Start with hobbies the user doesn't follow
        var query = _context.Hobbies
            .Include(h => h.Users)
            .Where(h => !userHobbyIds.Contains(h.Id));

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
        {
            query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                                h.Description.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        if (totalCount == 0)
        {
            return Ok(new
            {
                hobbies = new List<object>(),
                pagination = new
                {
                    currentPage = page,
                    totalPages = 0,
                    totalCount = 0,
                    pageSize,
                    hasNextPage = false,
                    hasPreviousPage = false
                }
            });
        }

        // Get all matching hobbies for randomization or ordering
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

        // Apply ordering
        List<object> orderedHobbies;
        if (randomOrder)
        {
            var random = new Random();
            orderedHobbies = allHobbies
                .OrderBy(x => random.Next())
                .Cast<object>()
                .ToList();
        }
        else
        {
            // Default: order by popularity (followers count) then by name
            orderedHobbies = allHobbies
                .OrderByDescending(h => h.FollowersCount)
                .ThenBy(h => h.Name)
                .Cast<object>()
                .ToList();
        }

        // Apply pagination
        var pagedHobbies = orderedHobbies
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new
        {
            hobbies = pagedHobbies,
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
        _logger.LogError(ex, "Error in GetPersonalisedHobbies");
        return StatusCode(500, "An error occurred while fetching personalised hobbies");
    }
}

        [HttpGet("random-mix")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRandomMixHobbies(
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

                var query = _context.Hobbies.Include(h => h.Users).AsQueryable();

                var userHobbyIds = await _context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == user.Id))
                    .Select(h => h.Id)
                    .ToListAsync();

                query = query.Where(h => !userHobbyIds.Contains(h.Id));

                if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
                {
                    query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(h => h.Name.ToLower().Contains(search.ToLower()) ||
                                        h.Description.ToLower().Contains(search.ToLower()));
                }

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return Ok(new
                    {
                        hobbies = new List<object>(),
                        pagination = new
                        {
                            currentPage = page,
                            totalPages = 0,
                            totalCount = 0,
                            pageSize,
                            hasNextPage = false,
                            hasPreviousPage = false
                        }
                    });
                }

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
                var shuffledHobbies = allHobbies.OrderBy(x => randomGen.Next()).ToList();

                var hobbies = shuffledHobbies
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

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
                _logger.LogError(ex, "Error in GetRandomMixHobbies");
                return StatusCode(500, "An error occurred while fetching random hobbies");
            }
        }

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

                var userPreferredTypes = await _context.Hobbies
                    .Where(h => userHobbyIds.Contains(h.Id) && !string.IsNullOrEmpty(h.Type))
                    .GroupBy(h => h.Type)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .ToListAsync();

                var query = _context.Hobbies
                    .Include(h => h.Users)
                    .Where(h => !userHobbyIds.Contains(h.Id))
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
                {
                    query = query.Where(h => h.Type != null && h.Type.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(h => h.Name.ToLower().Contains(search.ToLower()) ||
                                        h.Description.ToLower().Contains(search.ToLower()));
                }

                IOrderedQueryable<Hobby> orderedQuery;

                if (userPreferredTypes.Any())
                {
                    orderedQuery = query
                        .OrderByDescending(h => userPreferredTypes.Contains(h.Type ?? ""))
                        .ThenByDescending(h => h.Users.Count)
                        .ThenBy(h => h.Name);
                }
                else
                {
                    orderedQuery = query
                        .OrderByDescending(h => h.Users.Count)
                        .ThenBy(h => h.Name);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

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
                        IsPreferredCategory = userPreferredTypes.Contains(h.Type ?? "")
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
                        userPreferredTypes = userPreferredTypes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendedHobbies");
                return StatusCode(500, "An error occurred while fetching recommendations");
            }
        }

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

        public class HobbyDto
        {
            [Required(ErrorMessage = "Hobby name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Hobby name must be between 2 and 100 characters")]
            public string Name { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            public string Description { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
            public string Type { get; set; } = string.Empty;

            [Url(ErrorMessage = "Please provide a valid URL")]
            public string Link { get; set; } = string.Empty;

            public string ImageUrl { get; set; } = string.Empty;
        }

        public class ForumPostDto
        {
            public int? Id { get; set; }
            public int? HobbyId { get; set; }
            public string? UserId { get; set; }
            [Required(ErrorMessage = "Content is required")]
            [StringLength(1000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 1000 characters")]
            public string Content { get; set; } = string.Empty;
            public DateTime? CreatedAt { get; set; }
        }
    }
}