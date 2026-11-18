using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Controllers;

/// <summary>Operations for anonymous shopping carts.</summary>
[ApiController]
[Route("api/cart")]
public sealed class CartController(ICartApiService cartService) : ControllerBase
{
    /// <summary>Creates a new anonymous cart.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CartDto>> Create(CancellationToken cancellationToken)
    {
        var cart = await cartService.CreateAsync(cancellationToken);
        Response.Headers["X-Cart-Id"] = cart.Id.ToString("D");
        return Created("/api/cart", cart);
    }

    /// <summary>Returns an existing cart or creates one when X-Cart-Id is missing or invalid.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> Get(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        CancellationToken cancellationToken)
    {
        var resolvedCartId = await ResolveCartIdAsync(cartId, cancellationToken);
        var cart = await cartService.GetByIdAsync(resolvedCartId, cancellationToken);
        return Ok(cart!);
    }

    /// <summary>Adds a product to the cart identified by X-Cart-Id.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedCartId = await ResolveCartIdAsync(cartId, cancellationToken);
        var result = await cartService.AddItemAsync(
            resolvedCartId,
            request.ProductId,
            request.Quantity,
            cancellationToken);
        return ToActionResult(result, "Cart item");
    }

    /// <summary>Sets a product's quantity in the cart identified by X-Cart-Id.</summary>
    [HttpPut("items/{productId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItem(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedCartId = await ResolveCartIdAsync(cartId, cancellationToken);
        var result = await cartService.UpdateItemAsync(
            resolvedCartId,
            productId,
            request.Quantity,
            cancellationToken);
        return ToActionResult(result, "Cart item");
    }

    /// <summary>Removes a product from the cart identified by X-Cart-Id.</summary>
    [HttpDelete("items/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        int productId,
        CancellationToken cancellationToken)
    {
        var resolvedCartId = await ResolveCartIdAsync(cartId, cancellationToken);
        var result = await cartService.RemoveItemAsync(resolvedCartId, productId, cancellationToken);
        return result.Status switch
        {
            CartApiStatus.Success => NoContent(),
            CartApiStatus.CartNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Cart not found",
                "The requested cart was not found.")),
            _ => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Cart item not found",
                "The requested product is not in this cart."))
        };
    }

    /// <summary>Removes all items from the cart identified by X-Cart-Id.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        CancellationToken cancellationToken)
    {
        var resolvedCartId = await ResolveCartIdAsync(cartId, cancellationToken);
        var result = await cartService.ClearAsync(resolvedCartId, cancellationToken);
        return result == CartApiStatus.Success
            ? NoContent()
            : NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Cart not found",
                "The requested cart was not found."));
    }

    private async Task<Guid> ResolveCartIdAsync(string? rawCartId, CancellationToken cancellationToken)
    {
        Guid? requestedCartId = Guid.TryParse(rawCartId, out var parsedCartId) && parsedCartId != Guid.Empty
            ? parsedCartId
            : null;
        var cartId = await cartService.GetOrCreateCartIdAsync(requestedCartId, cancellationToken);
        Response.Headers["X-Cart-Id"] = cartId.ToString("D");
        return cartId;
    }

    private ActionResult<CartDto> ToActionResult(CartApiResult result, string resourceName)
    {
        return result.Status switch
        {
            CartApiStatus.Success => Ok(result.Cart),
            CartApiStatus.CartNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Cart not found",
                "The requested cart was not found.")),
            CartApiStatus.ProductNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Product not found",
                "The requested product was not found.")),
            CartApiStatus.CartItemNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Cart item not found",
                "The requested product is not in this cart.")),
            CartApiStatus.InvalidQuantity => BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid quantity",
                "Quantity must be between 1 and 99, and the total quantity for a product cannot exceed 99.")),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Cart operation failed",
                detail: $"The {resourceName} operation could not be completed.")
        };
    }

    private ProblemDetails CreateProblem(int statusCode, string title, string detail) => new()
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = HttpContext.Request.Path
    };
}
