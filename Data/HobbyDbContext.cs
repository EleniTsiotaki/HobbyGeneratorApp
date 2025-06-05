using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HobbyGeneratorAPI.Models;

namespace HobbyGeneratorAPI.Data
{
    public class HobbyDbContext : IdentityDbContext<ApplicationUser>
    {
        public HobbyDbContext(DbContextOptions<HobbyDbContext> options) : base(options)
        {
        }

        public DbSet<Hobby> Hobbies { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure User-Hobby many-to-many relationship
            builder.Entity<Hobby>()
                .HasMany(h => h.Users)
                .WithMany(u => u.Hobbies)
                .UsingEntity(j => j.ToTable("UserHobbies"));

            // Configure ForumPost-Hobby one-to-many relationship
            builder.Entity<ForumPost>()
                .HasOne(fp => fp.Hobby)
                .WithMany(h => h.ForumPosts)
                .HasForeignKey(fp => fp.HobbyId);

            // Configure ForumPost-User one-to-many relationship
            builder.Entity<ForumPost>()
                .HasOne(fp => fp.User)
                .WithMany(u => u.ForumPosts)
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Enable cascading delete

            // Configure ForumPost self-referencing relationship for replies
            builder.Entity<ForumPost>()
                .HasOne(fp => fp.ParentPost)
                .WithMany(fp => fp.Replies)
                .HasForeignKey(fp => fp.ParentPostId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes for replies
        }
    }
}