using Microsoft.EntityFrameworkCore;
using HobbyGeneratorAPI.Data;
using HobbyGeneratorAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace HobbyGeneratorAPI.Services
{
    public class HobbySeederService
    {
        private readonly HobbyDbContext _context;
        private readonly ILogger<HobbySeederService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HobbySeederService(
            HobbyDbContext context,
            ILogger<HobbySeederService> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<(bool SeededHobbies, int HobbyCount, bool SeededAdmin)> SeedDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding process...");

                // Seed Admin Role and User
                bool seededAdmin = await SeedAdminAsync();

                // Seed Hobbies
                var (seededHobbies, hobbyCount) = await SeedHobbiesAsync();

                _logger.LogInformation("Seeding completed. Hobbies seeded: {SeededHobbies}, Hobby count: {HobbyCount}, Admin seeded: {SeededAdmin}",
                    seededHobbies, hobbyCount, seededAdmin);

                return (seededHobbies, hobbyCount, seededAdmin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding data");
                throw;
            }
        }

        private async Task<bool> SeedAdminAsync()
        {
            bool seeded = false;

            // Seed Admin Role
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                _logger.LogInformation("Creating Admin role...");
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    throw new Exception("Failed to create Admin role");
                }
                seeded = true;
            }

            // Seed Default Admin User
            var adminEmail = "admin@gmail.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                _logger.LogInformation("Creating default admin user...");
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var userResult = await _userManager.CreateAsync(adminUser, "Admin123!");
                if (!userResult.Succeeded)
                {
                    _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", userResult.Errors.Select(e => e.Description)));
                    throw new Exception("Failed to create admin user");
                }

                var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign Admin role to user: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    throw new Exception("Failed to assign Admin role");
                }
                seeded = true;
            }
            else
            {
                _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
                // Ensure existing admin has Admin role
                if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to assign Admin role to user: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        throw new Exception("Failed to assign Admin role");
                    }
                    seeded = true;
                }
            }

            return seeded;
        }

        private async Task<(bool Seeded, int Count)> SeedHobbiesAsync()
        {
            // Check if any hobbies already exist
            var existingCount = await _context.Hobbies.CountAsync();
            if (existingCount > 0)
            {
                _logger.LogInformation("Database already contains {Count} hobbies, skipping seeding", existingCount);
                return (false, existingCount);
            }

            _logger.LogInformation("Seeding hobbies...");
            var hobbies = GetSeedHobbies();

            // Add all hobbies
            await _context.Hobbies.AddRangeAsync(hobbies);
            var savedCount = await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} hobbies", savedCount);
            return (true, savedCount);
        }

        private List<Hobby> GetSeedHobbies()
        {
            return new List<Hobby>
            {
                // Creative Hobbies
                new() { Name = "Painting", Description = "Express your creativity with colors and brushes", Type = "Creative", Link = "https://www.artistsnetwork.com/", ImageUrl = "https://images.unsplash.com/photo-1513475382585-d06e58bcb0e0?w=400" },
                new() { Name = "Photography", Description = "Capture beautiful moments and create lasting memories", Type = "Creative", Link = "https://www.photography.com/", ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=400" },
                new() { Name = "Drawing", Description = "Create art with pencils, pens, and digital tools", Type = "Creative", Link = "https://www.drawing.com/", ImageUrl = "https://images.unsplash.com/photo-1583225214464-929602ebb0aa?w=400" },
                new() { Name = "Writing", Description = "Tell stories, write poetry, or keep a journal", Type = "Creative", Link = "https://www.writersdigest.com/", ImageUrl = "https://images.unsplash.com/photo-1455390582262-044cdeadadfa" },
                new() { Name = "Sculpting", Description = "Shape clay, stone, or other materials into art", Type = "Creative", Link = "https://www.sculpture.org/", ImageUrl = "https://images.unsplash.com/photo-1578662996442-48f60103fc96" },

                // Sports & Fitness
                new() { Name = "Running", Description = "Stay fit and explore your neighborhood", Type = "Sports", Link = "https://www.runnersworld.com/", ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b" },
                new() { Name = "Yoga", Description = "Improve flexibility and find inner peace", Type = "Sports", Link = "https://www.yogajournal.com/", ImageUrl = "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b" },
                new() { Name = "Swimming", Description = "Full-body workout in the water", Type = "Sports", Link = "https://www.usaswimming.org/", ImageUrl = "https://images.unsplash.com/photo-1530549387789-4c1017266635" },
                new() { Name = "Cycling", Description = "Explore the outdoors on two wheels", Type = "Sports", Link = "https://www.bicycling.com/", ImageUrl = "https://images.unsplash.com/photo-1571068316344-75bc76f77890" },
                new() { Name = "Rock Climbing", Description = "Challenge yourself on natural and artificial walls", Type = "Sports", Link = "https://www.climbing.com/", ImageUrl = "https://images.unsplash.com/photo-1522163182402-834f871fd851" },

                // Outdoor
                new() { Name = "Hiking", Description = "Explore nature trails and mountain paths", Type = "Outdoor", Link = "https://www.alltrails.com/", ImageUrl = "https://images.unsplash.com/photo-1551698618-1dfe5d97d256" },
                new() { Name = "Camping", Description = "Sleep under the stars and connect with nature", Type = "Outdoor", Link = "https://www.rei.com/learn/expert-advice/camping-for-beginners.html", ImageUrl = "https://images.unsplash.com/photo-1504851149312-7a075b496cc7" },
                new() { Name = "Fishing", Description = "Relax by the water and catch your dinner", Type = "Outdoor", Link = "https://www.fieldandstream.com/", ImageUrl = "https://images.unsplash.com/photo-1445112098124-3e76dd67983c" },
                new() { Name = "Gardening", Description = "Grow your own flowers, herbs, and vegetables", Type = "Outdoor", Link = "https://www.gardenersworld.com/", ImageUrl = "https://images.unsplash.com/photo-1416879595882-3373a0480b5b" },

                // Technology
                new() { Name = "Programming", Description = "Build apps, websites, and solve problems with code", Type = "Technology", Link = "https://www.codecademy.com/", ImageUrl = "https://images.unsplash.com/photo-1461749280684-dccba630e2f6" },
                new() { Name = "3D Printing", Description = "Create physical objects from digital designs", Type = "Technology", Link = "https://www.thingiverse.com/", ImageUrl = "https://images.unsplash.com/photo-1581833971358-2c8b550f87b3" },
                new() { Name = "Electronics", Description = "Build circuits and electronic devices", Type = "Technology", Link = "https://www.arduino.cc/", ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475" },

                // Music
                new() { Name = "Guitar", Description = "Learn to play one of the most popular instruments", Type = "Music", Link = "https://www.justinguitar.com/", ImageUrl = "https://images.unsplash.com/photo-1510915361894-db8b60106cb1" },
                new() { Name = "Piano", Description = "Master the keys and play beautiful melodies", Type = "Music", Link = "https://www.pianonanny.com/", ImageUrl = "https://images.unsplash.com/photo-1520523839897-bd0b52f945a0" },
                new() { Name = "Singing", Description = "Express yourself through the power of voice", Type = "Music", Link = "https://www.singingcarrots.com/", ImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f" },

                // Crafts
                new() { Name = "Knitting", Description = "Create cozy sweaters, scarves, and blankets", Type = "Crafts", Link = "https://www.knittingpatterncentral.com/", ImageUrl = "https://images.unsplash.com/photo-1434725039720-aaad6dd32dfe" },
                new() { Name = "Woodworking", Description = "Build furniture and decorative items from wood", Type = "Crafts", Link = "https://www.woodmagazine.com/", ImageUrl = "https://images.unsplash.com/photo-1504148455328-c376907d081c" },
                new() { Name = "Pottery", Description = "Shape clay into bowls, vases, and art pieces", Type = "Crafts", Link = "https://www.pottery.org/", ImageUrl = "https://images.unsplash.com/photo-1578662996442-48f60103fc96" },

                // Learning
                new() { Name = "Language Learning", Description = "Master a new language and culture", Type = "Learning", Link = "https://www.duolingo.com/", ImageUrl = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173" },
                new() { Name = "Chess", Description = "Improve your strategic thinking with the royal game", Type = "Learning", Link = "https://www.chess.com/", ImageUrl = "https://images.unsplash.com/photo-1528819622765-d6bcf132f793" },
                new() { Name = "Reading", Description = "Explore new worlds through books and literature", Type = "Learning", Link = "https://www.goodreads.com/", ImageUrl = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570" },

                // Cooking
                new() { Name = "Baking", Description = "Create delicious breads, cakes, and pastries", Type = "Cooking", Link = "https://www.kingarthurbaking.com/", ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff" },
                new() { Name = "Cooking", Description = "Master the art of preparing delicious meals", Type = "Cooking", Link = "https://www.allrecipes.com/", ImageUrl = "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136" },

                // Collection
                new() { Name = "Coin Collecting", Description = "Discover history through rare and unique coins", Type = "Collection", Link = "https://www.usmint.gov/", ImageUrl = "https://images.unsplash.com/photo-1621504450181-5d356f61d307" },
                new() { Name = "Stamp Collecting", Description = "Explore the world through postal history", Type = "Collection", Link = "https://about.usps.com/who/leadership/pmg/", ImageUrl = "https://images.unsplash.com/photo-1578662996442-48f60103fc96" }
            };
        }
    }
}