using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Security;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Admin.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = AdminAuthenticationDefaults.Policy)]
public sealed class AdminProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = "id",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default) =>
        Ok(await productService.GetPageAsync(
            null,
            null,
            search,
            page,
            pageSize,
            sortBy,
            descending,
            cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found",
                Instance = HttpContext.Request.Path
            })
            : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(request, cancellationToken);
        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return CategoryValidationError(nameof(request.CategorySlug));
        }

        var product = result.Product
            ?? throw new InvalidOperationException("Product creation completed without a product response.");
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
        if (result.Status == ProductMutationStatus.ProductNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found",
                Instance = HttpContext.Request.Path
            });
        }

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return CategoryValidationError(nameof(request.CategorySlug));
        }

        var product = result.Product
            ?? throw new InvalidOperationException("Product update completed without a product response.");
        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            ProductMutationStatus.Success => NoContent(),
            ProductMutationStatus.ProductNotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found",
                Instance = HttpContext.Request.Path
            }),
            _ => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Product is in a cart",
                Detail = "The product cannot be deleted while it is referenced by a cart item.",
                Instance = HttpContext.Request.Path
            })
        };
    }

    private BadRequestObjectResult CategoryValidationError(string fieldName) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [fieldName] = ["The specified category does not exist."]
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid product category"
        });
}
