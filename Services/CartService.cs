using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Services;

public sealed class CartService
{
    public const decimal FreeShippingThreshold = 999;
    public const decimal ShippingFee = 89;

    private readonly Dictionary<string, CartItem> _items = [];

    public event Action? Changed;

    public IReadOnlyCollection<CartItem> Items => _items.Values;
    public int ItemCount => _items.Values.Sum(item => item.Quantity);
    public decimal Subtotal => _items.Values.Sum(item => item.Product.Price * item.Quantity);
    public decimal Shipping => Subtotal == 0 || Subtotal >= FreeShippingThreshold ? 0 : ShippingFee;
    public decimal Total => Subtotal + Shipping;

    public void Add(Product product, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        if (_items.TryGetValue(product.Id, out var existing))
        {
            _items[product.Id] = existing with { Quantity = existing.Quantity + quantity };
        }
        else
        {
            _items.Add(product.Id, new CartItem(product, quantity));
        }

        Changed?.Invoke();
    }

    public void Increase(string productId)
    {
        if (!_items.TryGetValue(productId, out var existing))
        {
            return;
        }

        _items[productId] = existing with { Quantity = existing.Quantity + 1 };
        Changed?.Invoke();
    }

    public void Decrease(string productId)
    {
        if (!_items.TryGetValue(productId, out var existing))
        {
            return;
        }

        if (existing.Quantity <= 1)
        {
            return;
        }

        _items[productId] = existing with { Quantity = existing.Quantity - 1 };
        Changed?.Invoke();
    }

    public void Remove(string productId)
    {
        if (_items.Remove(productId))
        {
            Changed?.Invoke();
        }
    }
}
