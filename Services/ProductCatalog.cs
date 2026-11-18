using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Services;

public sealed class ProductCatalog
{
    private static readonly Category PersonalizedGifts = new(
        "personalized-gifts", "Personalized Gifts", "Make it theirs with a thoughtful personal touch.",
        "https://images.unsplash.com/photo-1513883049090-d0b7439799bf?auto=format&fit=crop&w=900&q=85");
    private static readonly Category HandmadeGifts = new(
        "handmade-gifts", "Handmade Gifts", "Little details, made carefully by hand.",
        "https://images.unsplash.com/photo-1513201099705-a9746e1e201f?auto=format&fit=crop&w=900&q=85");
    private static readonly Category GiftsForOccasions = new(
        "gifts-for-occasions", "Occasion Gifts", "Thoughtful surprises for all of life's big days.",
        "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=900&q=85");
    private static readonly Category HomeDecor = new(
        "home-decor", "Home & Decor", "Warm, personal touches for their happy place.",
        "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=900&q=85");
    private static readonly Category Keepsakes = new(
        "keepsakes", "Keepsakes", "Hold on to the moments that mean the most.",
        "https://images.unsplash.com/photo-1504198322253-cfa87a0ff25f?auto=format&fit=crop&w=900&q=85");
    private static readonly Category GiftHampers = new(
        "gift-hampers", "Gift Hampers", "A lovely collection of little things, all wrapped up.",
        "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=900&q=85");

    private static readonly IReadOnlyList<Category> CatalogCategories =
    [
        PersonalizedGifts,
        HandmadeGifts,
        GiftsForOccasions,
        HomeDecor,
        Keepsakes,
        GiftHampers
    ];

    private static readonly IReadOnlyList<Product> CatalogProducts =
    [
        new(1, "The Forever Frame", Keepsakes, "A favourite memory, made part of home.", 1299,
            "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=82", "Popular",
            [
                "https://images.unsplash.com/photo-1504198322253-cfa87a0ff25f?auto=format&fit=crop&w=800&q=82",
                "https://images.unsplash.com/photo-1513475382585-d06e58bcb0e0?auto=format&fit=crop&w=800&q=82"
            ]),
        new(2, "Little Moments Box", GiftHampers, "A keepsake box for all the little things.", 1499,
            "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=800&q=82", "Recommended",
            [
                "https://images.unsplash.com/photo-1513201099705-a9746e1e201f?auto=format&fit=crop&w=800&q=82",
                "https://images.unsplash.com/photo-1513883049090-d0b7439799bf?auto=format&fit=crop&w=800&q=82"
            ]),
        new(3, "Your Name, In Lights", PersonalizedGifts, "A one-of-a-kind touch, made just for them.", 1799,
            "https://images.unsplash.com/photo-1513883049090-d0b7439799bf?auto=format&fit=crop&w=800&q=82", "New",
            [
                "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=82",
                "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=800&q=82"
            ], isPersonalized: true),
        new(4, "Sunday Kind of Flowers", HomeDecor, "A little colour for their everyday corner.", 899,
            "https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=82", "Popular"),
        new(5, "Our Story Frame", Keepsakes, "Make your favourite chapter a keepsake.", 1599,
            "https://images.unsplash.com/photo-1504198322253-cfa87a0ff25f?auto=format&fit=crop&w=800&q=82", "Recommended"),
        new(6, "Soft Thoughts Gift Set", GiftHampers, "A thoughtful little bundle for a big day.", 1899,
            "https://images.unsplash.com/photo-1513201099705-a9746e1e201f?auto=format&fit=crop&w=800&q=82", "New"),
        new(7, "The Little Love Note", HandmadeGifts, "Your words, beautifully kept close.", 699,
            "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=82", "Recommended"),
        new(8, "Golden Hour Keepsake", HomeDecor, "A warm, lovely detail for their happy place.", 1099,
            "https://images.unsplash.com/photo-1513475382585-d06e58bcb0e0?auto=format&fit=crop&w=800&q=82", "Popular"),
        new(9, "Celebrate Together Hamper", GiftsForOccasions, "A joyful little collection for their special day.", 2199,
            "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=800&q=82", "New")
    ];

    public IReadOnlyList<Category> Categories => CatalogCategories;
    public IReadOnlyList<Product> Products => CatalogProducts;
}
