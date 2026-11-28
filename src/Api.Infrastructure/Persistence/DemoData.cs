using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

/// <summary>The 12 demo users (the same as the User Directory's), added only to an empty table.</summary>
public static class DemoData
{
    private sealed record Demo(
        string Name, string Email, string? Phone, int Age, string Country, string City,
        string Job, string? Bio, string[] Interests, int JoinedDaysAgo);

    private static readonly Demo[] Users =
    [
        new("Amara Okafor", "amara.okafor@example.com", "+44 7700 900123", 29, "GB", "Manchester", "Frontend developer", "Builds accessible design systems and runs a weekend coding club for teenagers.", ["Coding", "Music", "Volunteering"], 120),
        new("Liam Chen", "liam.chen@example.com", "+1 604 555 0147", 34, "CA", "Vancouver", "Physiotherapist", "Former college rower. Spends most weekends on trails around the North Shore.", ["Sports", "Hiking", "Fitness"], 98),
        new("Sofia Martínez", "sofia.martinez@example.com", null, 41, "ES", "Valencia", "Head chef", "Runs a small seasonal restaurant and writes about regional Spanish cooking.", ["Cooking", "Travel", "Books"], 87),
        new("Noah Patel", "noah.patel@example.com", "+44 7700 900456", 22, "GB", "Leeds", "Junior data analyst", null, ["Coding", "Gaming", "Science"], 75),
        new("Grace Hopper", "grace.hopper@example.com", null, 45, "US", "New York", "Engineering manager", "Leads a platform team. Collects vintage computers.", ["Coding", "Science", "Books"], 64),
        new("Yuki Tanaka", "yuki.tanaka@example.com", "+81 90 1234 5678", 31, "JP", "Osaka", "Illustrator", "Freelance illustrator for children's books and indie games.", ["Art", "Gaming", "Photography"], 52),
        new("Siobhán Murphy", "siobhan.murphy@example.com", "+353 87 123 4567", 27, "IE", "Galway", "Marine biologist", "Studies coastal ecosystems along the west of Ireland.", ["Science", "Hiking", "Photography"], 40),
        new("Lukas Becker", "lukas.becker@example.com", null, 38, "DE", "Berlin", "Product designer", "Designs tools for small businesses. Cycles everywhere.", ["Art", "Fitness", "Travel"], 33),
        new("Priya Sharma", "priya.sharma@example.com", "+91 98765 43210", 26, "IN", "Bengaluru", "Machine learning engineer", null, ["Coding", "Music", "Movies"], 21),
        new("Chloé Dubois", "chloe.dubois@example.com", null, 52, "FR", "Lyon", "Bookshop owner", "Owns an independent bookshop with a café in the back.", ["Books", "Cooking", "Gardening"], 14),
        new("Mateus Silva", "mateus.silva@example.com", "+55 11 91234 5678", 19, "BR", "São Paulo", "Student", "Studying architecture. Plays futsal twice a week.", ["Sports", "Art", "Music"], 7),
        new("Olivia Brown", "olivia.brown@example.com", "+61 412 345 678", 63, "AU", "Melbourne", "Retired teacher", "Taught primary school for 30 years; now volunteers at the community garden.", ["Gardening", "Volunteering", "Books"], 2),
    ];

    /// <summary>Adds the demo users if there is nobody yet. Returns how many were added.</summary>
    public static async Task<int> SeedAsync(AppDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return 0;

        var now = clock.GetUtcNow();
        foreach (var d in Users)
        {
            var joined = now.AddDays(-d.JoinedDaysAgo);
            var user = new User
            {
                FullName = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Age = d.Age,
                Country = d.Country,
                City = d.City,
                JobTitle = d.Job,
                Bio = d.Bio,
                Interests = [.. d.Interests],
            };
            user.MarkCreated(joined);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync(ct);
        return Users.Length;
    }
}
