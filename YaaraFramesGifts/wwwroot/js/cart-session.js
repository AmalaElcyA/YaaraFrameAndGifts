export function getCartId(key) {
    return window.localStorage.getItem(key);
}

export function setCartId(key, cartId) {
    window.localStorage.setItem(key, cartId);
}
