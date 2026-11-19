using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Controllers;

/// <summary>Product catalog endpoints.</summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Returns products, optionally filtered by category slug and badge.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        [FromQuery(Name = "category")] string? category,
        [FromQuery] string? badge,
        CancellationToken cancellationToken)
    {
        var products = await productService.GetAllAsync(category, badge, cancellationToken);
        return Ok(products);
    }

    /// <summary>Returns searchable, paged products for the administration interface.</summary>
    [HttpGet("manage")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetManageList(
        [FromQuery(Name = "category")] string? category,
        [FromQuery] string? badge,
        [FromQuery] string? search,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = "id",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var products = await productService.GetPageAsync(
            category,
            badge,
            search,
            page,
            pageSize,
            sortBy,
            descending,
            cancellationToken);
        return Ok(products);
    }

    /// <summary>Returns the featured products.</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetFeatured(CancellationToken cancellationToken)
    {
        var products = await productService.GetFeaturedAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>Returns product details by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null
            ? NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Product not found",
                $"Product '{id}' was not found."))
            : Ok(product);
    }

    /// <summary>Creates a product in an existing category.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(request, cancellationToken);
        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.CategorySlug)] = ["The specified category does not exist."]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid product category"
            });
        }

        var product = result.Product
            ?? throw new InvalidOperationException("Product creation completed without a product response.");
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Updates an existing product.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
        if (result.Status == ProductMutationStatus.ProductNotFound)
        {
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Product not found",
                $"Product '{id}' was not found."));
        }

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.CategorySlug)] = ["The specified category does not exist."]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid product category"
            });
        }

        var product = result.Product
            ?? throw new InvalidOperationException("Product update completed without a product response.");
        return Ok(product);
    }

    /// <summary>Deletes a product if it is not referenced by a cart item.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            ProductMutationStatus.Success => NoContent(),
            ProductMutationStatus.ProductNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Product not found",
                $"Product '{id}' was not found.")),
            _ => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Product is in a cart",
                "The product cannot be deleted while it is referenced by a cart item."))
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
