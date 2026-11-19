using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Api;

public sealed class YaaraApiClient : IYaaraApiClient
{
    private readonly HttpClient httpClient;

    public YaaraApiClient(
        HttpClient httpClient,
        NavigationManager navigationManager,
        IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
        }
        else if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseAddress))
        {
            httpClient.BaseAddress = baseAddress;
        }
        else
        {
            throw new InvalidOperationException("Api:BaseUrl must be an absolute URL when configured.");
        }

        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<CategoryDto>("api/categories", cancellationToken);

    public async Task<CategoryDto?> GetCategoryAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/categories/{Uri.EscapeDataString(slug)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadBodyAsync<CategoryDto>(response, cancellationToken);
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        string? category = null,
        string? badge = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Add($"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(badge))
        {
            query.Add($"badge={Uri.EscapeDataString(badge)}");
        }

        var path = "api/products";
        if (query.Count > 0)
        {
            path = $"{path}?{string.Join("&", query)}";
        }

        return GetListAsync<ProductDto>(path, cancellationToken);
    }

    public Task<IReadOnlyList<ProductDto>> GetFeaturedProductsAsync(
        CancellationToken cancellationToken = default) =>
        GetListAsync<ProductDto>("api/products/featured", cancellationToken);

    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/products/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadBodyAsync<ProductDto>(response, cancellationToken);
    }

    public Task<ApiCartResponse> GetCartAsync(Guid? cartId, CancellationToken cancellationToken = default) =>
        SendCartRequestAsync(HttpMethod.Get, "api/cart", cartId, null, true, cancellationToken);

    public Task<ApiCartResponse> AddCartItemAsync(
        Guid? cartId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendCartRequestAsync(HttpMethod.Post, "api/cart/items", cartId, request, true, cancellationToken);

    public Task<ApiCartResponse> UpdateCartItemAsync(
        Guid? cartId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendCartRequestAsync(
            HttpMethod.Put,
            $"api/cart/items/{productId}",
            cartId,
            request,
            true,
            cancellationToken);

    public Task<ApiCartResponse> RemoveCartItemAsync(
        Guid? cartId,
        int productId,
        CancellationToken cancellationToken = default) =>
        SendCartRequestAsync(
            HttpMethod.Delete,
            $"api/cart/items/{productId}",
            cartId,
            null,
            false,
            cancellationToken);

    public Task<ApiCartResponse> ClearCartAsync(Guid? cartId, CancellationToken cancellationToken = default) =>
        SendCartRequestAsync(HttpMethod.Delete, "api/cart", cartId, null, false, cancellationToken);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadBodyAsync<List<T>>(response, cancellationToken);
    }

    private async Task<ApiCartResponse> SendCartRequestAsync(
        HttpMethod method,
        string path,
        Guid? cartId,
        object? body,
        bool expectsCart,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (cartId is { } id && id != Guid.Empty)
        {
            request.Headers.Add("X-Cart-Id", id.ToString("D"));
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var responseCartId = ReadCartId(response);
        var cart = expectsCart
            ? await ReadBodyAsync<CartDto>(response, cancellationToken)
            : null;
        return new ApiCartResponse(responseCartId, cart);
    }

    private static Guid ReadCartId(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Cart-Id", out var values)
            || !Guid.TryParse(values.FirstOrDefault(), out var cartId)
            || cartId == Guid.Empty)
        {
            throw new YaaraApiException(
                (int)response.StatusCode,
                "The cart service returned an invalid cart identifier. Please try again.");
        }

        return cartId;
    }

    private static async Task<T> ReadBodyAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return body ?? throw new YaaraApiException(
            (int)response.StatusCode,
            "The service returned an empty response. Please try again.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (JsonException)
        {
        }

        var message = problem?.Detail
            ?? problem?.Title
            ?? response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Please check your request and try again.",
                HttpStatusCode.NotFound => "We couldn't find that item.",
                HttpStatusCode.Conflict => "We couldn't complete that change. Please try again.",
                _ => "We're having trouble reaching the shop right now. Please try again."
            };
        throw new YaaraApiException((int)response.StatusCode, message);
    }
}
