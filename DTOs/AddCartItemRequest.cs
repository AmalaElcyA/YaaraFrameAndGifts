namespace YaaraFramesGifts.DTOs;

/// <summary>Payload for adding a product to a cart.</summary>
public sealed record AddCartItemRequest
{
    /// <summary>ID of the product to add.</summary>
    public int ProductId { get; init; }

    /// <summary>Number of units to add.</summary>
    public int Quantity { get; init; } = 1;
}
