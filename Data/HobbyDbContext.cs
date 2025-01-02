using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

public class HobbyDbContext : DbContext
{
    public HobbyDbContext(DbContextOptions<HobbyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Hobby> Hobbies { get; set; }
}
public class ApplicationUser : IdentityUser
{
    // Add additional properties if needed (e.g., first name, last name, etc.)
    public string FullName { get; set; }
}