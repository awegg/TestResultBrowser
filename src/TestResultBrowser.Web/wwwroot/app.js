window.trb = window.trb || {};
window.trb._sizeObservers = window.trb._sizeObservers || new WeakMap();

window.trb.observeSize = (element, dotNetRef) => {
    if (!element || !dotNetRef) {
        return;
    }

    const notify = () => {
        const rect = element.getBoundingClientRect();
        dotNetRef.invokeMethodAsync("OnChartSizeChanged", rect.width);
    };

    notify();
    const observer = new ResizeObserver(() => notify());
    observer.observe(element);
    window.trb._sizeObservers.set(element, observer);
};

window.trb.unobserveSize = (element) => {
    if (!element) {
        return;
    }

    const observer = window.trb._sizeObservers.get(element);
    if (observer) {
        observer.disconnect();
        window.trb._sizeObservers.delete(element);
    }
};
