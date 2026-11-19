using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Data;

public static class DbSeeder
{
    private static readonly Category[] SeedCategories =
    [
        new()
        {
            Slug = "personalized",
            Name = "Personalized Gifts",
            Description = "Thoughtful gifts made special with a personal touch.",
            Icon = "heart",
            AccentColor = "#B86F68"
        },
        new()
        {
            Slug = "handmade",
            Name = "Handmade Gifts",
            Description = "Carefully crafted gifts made by hand.",
            Icon = "flower",
            AccentColor = "#A77B5A"
        },
        new()
        {
            Slug = "occasions",
            Name = "Occasion Gifts",
            Description = "Meaningful gifts for life's special celebrations.",
            Icon = "sparkle",
            AccentColor = "#C18A64"
        },
        new()
        {
            Slug = "home",
            Name = "Home & Decor",
            Description = "Personal touches to make a home feel warmer.",
            Icon = "home",
            AccentColor = "#7E8C73"
        },
        new()
        {
            Slug = "keepsakes",
            Name = "Keepsakes",
            Description = "Lovely ways to hold on to meaningful memories.",
            Icon = "photo",
            AccentColor = "#A67C8A"
        },
        new()
        {
            Slug = "hampers",
            Name = "Gift Hampers",
            Description = "Thoughtful collections, beautifully gathered together.",
            Icon = "gift",
            AccentColor = "#C08C58"
        }
    ];

    private static readonly Product[] SeedProducts =
    [
        new()
        {
            Name = "Personalized Photo Frame",
            Description = "A favorite photograph displayed in a handcrafted frame personalized with a message of your choice.",
            Price = 1299m,
            ImageUrl = "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "personalized",
            Badge = "Popular",
            IsPersonalized = true
        },
        new()
        {
            Name = "Custom Name Wooden Frame",
            Description = "A warm wooden frame featuring a name and date, made to celebrate someone special.",
            Price = 1599m,
            ImageUrl = "https://images.unsplash.com/photo-1504198322253-cfa87a0ff25f?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "personalized",
            Badge = "New",
            IsPersonalized = true
        },
        new()
        {
            Name = "Personalized Keepsake Mug",
            Description = "A ceramic mug printed with a custom name or short note for a thoughtful everyday surprise.",
            Price = 549m,
            ImageUrl = "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "personalized",
            Badge = "Recommended",
            IsPersonalized = true
        },
        new()
        {
            Name = "Customized LED Photo Frame",
            Description = "A softly illuminated photo frame customized with a name to brighten a bedside table or shelf.",
            Price = 2499m,
            ImageUrl = "https://images.unsplash.com/photo-1513883049090-d0b7439799bf?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "personalized",
            Badge = "New",
            IsPersonalized = true
        },
        new()
        {
            Name = "Handmade Gift Box",
            Description = "A carefully assembled gift box with charming details and a handwritten note card.",
            Price = 899m,
            ImageUrl = "https://images.unsplash.com/photo-1513201099705-a9746e1e201f?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "handmade",
            Badge = "Popular",
            IsPersonalized = false
        },
        new()
        {
            Name = "Handmade Memory Scrapbook",
            Description = "A hand-bound scrapbook with textured pages ready for photographs, notes, and little mementos.",
            Price = 1399m,
            ImageUrl = "https://images.unsplash.com/photo-1455390582262-044cdead277a?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "handmade",
            Badge = "Recommended",
            IsPersonalized = false
        },
        new()
        {
            Name = "Birthday Celebration Hamper",
            Description = "A cheerful birthday collection with sweet treats and thoughtful little surprises, ready to gift.",
            Price = 1899m,
            ImageUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "hampers",
            Badge = "Popular",
            IsPersonalized = false
        },
        new()
        {
            Name = "Anniversary Memory Frame",
            Description = "A keepsake frame for a favorite couple's photograph, finished with a loving anniversary message.",
            Price = 1799m,
            ImageUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "occasions",
            Badge = "Recommended",
            IsPersonalized = true
        },
        new()
        {
            Name = "Handcrafted Home Accent",
            Description = "A tasteful decorative accent with a warm finish, chosen to bring a little character to any room.",
            Price = 1099m,
            ImageUrl = "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "home",
            Badge = "New",
            IsPersonalized = false
        },
        new()
        {
            Name = "Little Moments Keepsake Box",
            Description = "A keepsake box for handwritten notes, tiny treasures, and the moments worth remembering.",
            Price = 1199m,
            ImageUrl = "https://images.unsplash.com/photo-1504198322253-cfa87a0ff25f?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "keepsakes",
            Badge = "Popular",
            IsPersonalized = false
        },
        new()
        {
            Name = "Couple's Celebration Gift Set",
            Description = "A thoughtful gift set for two, made for anniversaries, weddings, and shared milestones.",
            Price = 2199m,
            ImageUrl = "https://images.unsplash.com/photo-1522673607200-164d1b6ce486?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "occasions",
            Badge = "Recommended",
            IsPersonalized = false
        },
        new()
        {
            Name = "Premium Curated Gift Hamper",
            Description = "A premium assortment of keepsakes and treats, beautifully arranged for a memorable celebration.",
            Price = 2999m,
            ImageUrl = "https://images.unsplash.com/photo-1513201099705-a9746e1e201f?auto=format&fit=crop&w=800&q=82",
            CategorySlug = "hampers",
            Badge = "New",
            IsPersonalized = false
        }
    ];

    public static async Task SeedAsync(
        YaaraDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existingCategorySlugs = await dbContext.Categories
            .Select(category => category.Slug)
            .ToHashSetAsync(cancellationToken);

        var categoriesToAdd = SeedCategories
            .Where(category => !existingCategorySlugs.Contains(category.Slug))
            .ToArray();

        if (categoriesToAdd.Length > 0)
        {
            dbContext.Categories.AddRange(categoriesToAdd);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingProductNames = await dbContext.Products
            .Select(product => product.Name)
            .ToHashSetAsync(cancellationToken);

        var productsToAdd = SeedProducts
            .Where(product => !existingProductNames.Contains(product.Name))
            .ToArray();

        if (productsToAdd.Length > 0)
        {
            dbContext.Products.AddRange(productsToAdd);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
