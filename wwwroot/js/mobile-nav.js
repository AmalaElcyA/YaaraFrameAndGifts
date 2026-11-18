export function activateMenu(panel, dotNetReference) {
    const focusableSelector = 'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])';

    const handleKeyDown = event => {
        if (event.key === "Escape") {
            event.preventDefault();
            dotNetReference.invokeMethodAsync("CloseMobileMenuFromKeyboard");
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusable = [...panel.querySelectorAll(focusableSelector)]
            .filter(element => element.getClientRects().length > 0);

        if (focusable.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    };

    panel.__yaaraMenuKeyDown = handleKeyDown;
    panel.addEventListener("keydown", handleKeyDown);
    panel.querySelector("button")?.focus();
}

export function deactivateMenu(panel) {
    if (panel.__yaaraMenuKeyDown) {
        panel.removeEventListener("keydown", panel.__yaaraMenuKeyDown);
        delete panel.__yaaraMenuKeyDown;
    }
}
