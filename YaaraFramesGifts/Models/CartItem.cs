namespace YaaraFramesGifts.Models;

public sealed class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Cart? Cart { get; set; }
    public Product Product { get; set; } = null!;

    public CartItem()
    {
    }

    public CartItem(Product product, int quantity)
    {
        Product = product;
        ProductId = product.Id;
        Quantity = quantity;
    }
}
