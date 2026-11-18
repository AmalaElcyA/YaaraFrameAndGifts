using Microsoft.JSInterop;
using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Api;

public sealed class CartSession(
    IYaaraApiClient apiClient,
    IJSRuntime jsRuntime,
    ILogger<CartSession> logger) : IAsyncDisposable
{
    private const string CartStorageKey = "yaara-frames-gifts-cart-id";
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private IJSObjectReference? jsModule;
    private Guid? cartId;
    private bool isInitialized;

    public event Action? Changed;

    public CartDto? Cart { get; private set; }
    public int ItemCount => Cart?.ItemCount ?? 0;
    public bool IsInitialized => isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            if (isInitialized)
            {
                return;
            }

            var storedCartId = await ReadStoredCartIdAsync();
            var response = await apiClient.GetCartAsync(storedCartId, cancellationToken);
            await ApplyCartResponseAsync(response);
            isInitialized = true;
            Changed?.Invoke();
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task AddItemAsync(
        int productId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var response = await apiClient.AddCartItemAsync(
                cartId,
                new AddCartItemRequest { ProductId = productId, Quantity = quantity },
                cancellationToken);
            await ApplyCartResponseAsync(response);
            Changed?.Invoke();
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task UpdateQuantityAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var response = await apiClient.UpdateCartItemAsync(
                cartId,
                productId,
                new UpdateCartItemRequest { Quantity = quantity },
                cancellationToken);
            await ApplyCartResponseAsync(response);
            Changed?.Invoke();
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task RemoveItemAsync(int productId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var response = await apiClient.RemoveCartItemAsync(cartId, productId, cancellationToken);
            cartId = response.CartId;
            await PersistCartIdAsync(response.CartId);
            var updatedCart = await apiClient.GetCartAsync(cartId, cancellationToken);
            await ApplyCartResponseAsync(updatedCart);
            Changed?.Invoke();
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var response = await apiClient.ClearCartAsync(cartId, cancellationToken);
            cartId = response.CartId;
            await PersistCartIdAsync(response.CartId);
            var updatedCart = await apiClient.GetCartAsync(cartId, cancellationToken);
            await ApplyCartResponseAsync(updatedCart);
            Changed?.Invoke();
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    private async Task<Guid?> ReadStoredCartIdAsync()
    {
        try
        {
            jsModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cart-session.js");
            var storedValue = await jsModule.InvokeAsync<string?>("getCartId", CartStorageKey);
            return Guid.TryParse(storedValue, out var storedId) && storedId != Guid.Empty
                ? storedId
                : null;
        }
        catch (JSException exception)
        {
            logger.LogWarning(exception, "Could not read the cart ID from browser storage.");
            return null;
        }
    }

    private async Task PersistCartIdAsync(Guid newCartId)
    {
        cartId = newCartId;
        try
        {
            jsModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cart-session.js");
            await jsModule.InvokeVoidAsync("setCartId", CartStorageKey, newCartId.ToString("D"));
        }
        catch (JSException exception)
        {
            logger.LogWarning(exception, "Could not save the cart ID to browser storage.");
        }
    }

    private async Task ApplyCartResponseAsync(ApiCartResponse response)
    {
        cartId = response.CartId;
        if (response.Cart is null)
        {
            throw new InvalidOperationException("The cart API response did not contain cart details.");
        }

        Cart = response.Cart;
        await PersistCartIdAsync(response.CartId);
    }

    public async ValueTask DisposeAsync()
    {
        if (jsModule is not null)
        {
            try
            {
                await jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        operationGate.Dispose();
    }
}
