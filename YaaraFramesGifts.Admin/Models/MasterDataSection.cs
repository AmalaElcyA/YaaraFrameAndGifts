namespace YaaraFramesGifts.Admin.Models;

public sealed record MasterDataSection(string Name, string Route, string Description, string Code);

public static class MasterDataSections
{
    public static IReadOnlyList<MasterDataSection> All { get; } =
    [
        new("Categories", "/master-data/categories", "Manage catalog categories and their storefront presentation.", "C"),
        new("Products", "/master-data/products", "Manage product details, pricing, images, and category links.", "P")
    ];
}
