namespace YaaraFramesGifts.DTOs;

/// <summary>Cart contents and calculated totals.</summary>
public sealed record CartDto(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CartItemDto> Items,
    int ItemCount,
    decimal Subtotal,
    decimal ShippingFee,
    decimal GrandTotal,
    bool IsFreeShippingEligible)
{
    /// <summary>Compatibility alias for the cart's shipping fee.</summary>
    public decimal Shipping => ShippingFee;

    /// <summary>Compatibility alias for the cart's grand total.</summary>
    public decimal Total => GrandTotal;
}
