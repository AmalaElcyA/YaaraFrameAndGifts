namespace YaaraFramesGifts.DTOs;

/// <summary>Payload for setting a cart item's quantity.</summary>
public sealed record UpdateCartItemRequest
{
    /// <summary>New quantity for the cart item.</summary>
    public int Quantity { get; init; }
}
