using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Security;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Admin.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = AdminAuthenticationDefaults.Policy)]
public sealed class AdminCategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CategoryDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default) =>
        Ok(await categoryService.GetPageAsync(
            search,
            page,
            pageSize,
            sortBy,
            descending,
            cancellationToken));

    [HttpGet("lookups")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetLookups(CancellationToken cancellationToken) =>
        Ok(await categoryService.GetAllAsync(cancellationToken));

    [HttpGet("{slug}")]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetBySlugAsync(slug, cancellationToken);
        return category is null
            ? NotFound(CreateProblem(StatusCodes.Status404NotFound, "Category not found"))
            : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(request, cancellationToken);
        if (result.Status == CategoryMutationStatus.SlugAlreadyExists)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Category slug already exists"));
        }

        var category = result.Category
            ?? throw new InvalidOperationException("Category creation completed without a category response.");
        return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, category);
    }

    [HttpPut("{slug}")]
    public async Task<ActionResult<CategoryDto>> Update(
        string slug,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateAsync(slug, request, cancellationToken);
        if (result.Status == CategoryMutationStatus.NotFound)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Category not found"));
        }

        var category = result.Category
            ?? throw new InvalidOperationException("Category update completed without a category response.");
        return Ok(category);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        var result = await categoryService.DeleteAsync(slug, cancellationToken);
        return result switch
        {
            CategoryMutationStatus.Success => NoContent(),
            CategoryMutationStatus.NotFound => NotFound(
                CreateProblem(StatusCodes.Status404NotFound, "Category not found")),
            _ => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Category has products",
                Detail = "The category cannot be deleted while products are associated with it."
            })
        };
    }

    private ProblemDetails CreateProblem(int statusCode, string title) => new()
    {
        Status = statusCode,
        Title = title,
        Instance = HttpContext.Request.Path
    };
}
