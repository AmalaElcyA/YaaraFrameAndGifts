using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Controllers;

/// <summary>Category catalog endpoints.</summary>
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>Returns all product categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>Returns searchable, paged categories for the administration interface.</summary>
    [HttpGet("manage")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResultDto<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<CategoryDto>>> GetManageList(
        [FromQuery] string? search,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await categoryService.GetPageAsync(
            search,
            page,
            pageSize,
            sortBy,
            descending,
            cancellationToken);
        return Ok(categories);
    }

    /// <summary>Returns a category by its slug.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.GetBySlugAsync(slug, cancellationToken);
        return category is null
            ? NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Category not found",
                $"Category '{slug}' was not found."))
            : Ok(category);
    }

    /// <summary>Creates a category with a unique slug.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(request, cancellationToken);
        if (result.Status == CategoryMutationStatus.SlugAlreadyExists)
        {
            return Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Category slug already exists",
                $"A category with slug '{request.Slug}' already exists."));
        }

        var category = result.Category
            ?? throw new InvalidOperationException("Category creation completed without a category response.");
        return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, category);
    }

    /// <summary>Updates the details of an existing category.</summary>
    [HttpPut("{slug}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(
        string slug,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateAsync(slug, request, cancellationToken);
        if (result.Status == CategoryMutationStatus.NotFound)
        {
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Category not found",
                $"Category '{slug}' was not found."));
        }

        var category = result.Category
            ?? throw new InvalidOperationException("Category update completed without a category response.");
        return Ok(category);
    }

    /// <summary>Deletes a category if it has no products.</summary>
    [HttpDelete("{slug}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        var result = await categoryService.DeleteAsync(slug, cancellationToken);
        return result switch
        {
            CategoryMutationStatus.Success => NoContent(),
            CategoryMutationStatus.NotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Category not found",
                $"Category '{slug}' was not found.")),
            _ => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Category has products",
                "The category cannot be deleted while products are associated with it."))
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
