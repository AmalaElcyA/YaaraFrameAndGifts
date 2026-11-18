namespace YaaraFramesGifts.DTOs;

/// <summary>A product and quantity contained in a cart.</summary>
public sealed record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string CategoryName,
    string Description,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
