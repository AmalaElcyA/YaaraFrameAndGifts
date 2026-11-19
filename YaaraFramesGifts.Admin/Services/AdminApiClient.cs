using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Admin.Services;

public sealed class AdminApiClient(
    IHttpClientFactory httpClientFactory,
    AuthenticationStateProvider authenticationStateProvider)
{
    public Task<PagedResultDto<CategoryDto>> GetCategoriesAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var path = "api/admin/categories"
            + $"?page={page}&pageSize={pageSize}&sortBy={Uri.EscapeDataString(sortBy)}&descending={descending}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            path += $"&search={Uri.EscapeDataString(search)}";
        }

        return GetAsync<PagedResultDto<CategoryDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoryLookupsAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<CategoryDto>>("api/admin/categories/lookups", cancellationToken);

    public Task<CategoryDto> GetCategoryAsync(string slug, CancellationToken cancellationToken = default) =>
        GetAsync<CategoryDto>($"api/admin/categories/{Uri.EscapeDataString(slug)}", cancellationToken);

    public Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateCategoryRequest, CategoryDto>("api/admin/categories", request, cancellationToken);

    public Task<CategoryDto> UpdateCategoryAsync(
        string slug,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<UpdateCategoryRequest, CategoryDto>(
            $"api/admin/categories/{Uri.EscapeDataString(slug)}",
            request,
            cancellationToken);

    public Task DeleteCategoryAsync(string slug, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/admin/categories/{Uri.EscapeDataString(slug)}", cancellationToken);

    public Task<PagedResultDto<ProductDto>> GetProductsAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var path = "api/admin/products"
            + $"?page={page}&pageSize={pageSize}&sortBy={Uri.EscapeDataString(sortBy)}&descending={descending}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            path += $"&search={Uri.EscapeDataString(search)}";
        }

        return GetAsync<PagedResultDto<ProductDto>>(path, cancellationToken);
    }

    public Task<ProductDto> GetProductAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<ProductDto>($"api/admin/products/{id}", cancellationToken);

    public Task<ProductDto> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateProductRequest, ProductDto>("api/admin/products", request, cancellationToken);

    public Task<ProductDto> UpdateProductAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<UpdateProductRequest, ProductDto>($"api/admin/products/{id}", request, cancellationToken);

    public Task DeleteProductAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/admin/products/{id}", cancellationToken);

    private Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken) =>
        SendForBodyAsync<TResponse>(HttpMethod.Get, path, null, cancellationToken);

    private Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken) =>
        SendForBodyAsync<TResponse>(HttpMethod.Post, path, body, cancellationToken);

    private Task<TResponse> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken) =>
        SendForBodyAsync<TResponse>(HttpMethod.Put, path, body, cancellationToken);

    private async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, path, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<TResponse> SendForBodyAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        return result ?? throw new AdminApiException(
            (int)response.StatusCode,
            "The API returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var token = authenticationState.User.FindFirst("admin_api_token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new AdminApiException(
                (int)HttpStatusCode.Unauthorized,
                "Your administrator session has expired. Sign in again.");
        }

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var client = httpClientFactory.CreateClient("AdminApi");
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AdminApiException(
                0,
                $"Unable to reach the Admin API. {exception.Message}");
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"The API request failed with HTTP {(int)response.StatusCode}.";
        IReadOnlyDictionary<string, string[]>? errors = null;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new AdminApiException(
                (int)response.StatusCode,
                "Your administrator session has expired. Sign in again.");
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            if (problem is not null)
            {
                errors = problem.Errors.ToDictionary(pair => pair.Key, pair => pair.Value);
                message = problem.Detail ?? problem.Title ?? message;
            }
        }
        catch (JsonException)
        {
            message = $"The API request failed with HTTP {(int)response.StatusCode} and returned an invalid error response.";
        }

        throw new AdminApiException((int)response.StatusCode, message, errors);
    }
}

public sealed class AdminApiException(
    int statusCode,
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}
