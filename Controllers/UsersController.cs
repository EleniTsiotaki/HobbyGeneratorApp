using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly HobbyDbContext _context;

    public UsersController(HobbyDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.Include(u => u.Hobbies).ToListAsync();
        return Ok(users);
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.Include(u => u.Hobbies)
                                        .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> CreateUser(User user)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // Hash the password
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        var existingUser = await _context.Users.FindAsync(id);

        if (existingUser == null)
        {
            return NotFound();
        }

        // Update user properties here (e.g., username, email, etc.)
        existingUser.Username = user.Username;
        existingUser.Email = user.Email;
        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // Hash the new password

        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync();

        return NoContent(); // Indicating the update was successful with no content in the response
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
