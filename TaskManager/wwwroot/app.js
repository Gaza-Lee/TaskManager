// Smoothly scrolls to a task card and briefly pulses it so it's easy to spot
window.scrollToTask = function (elementId) {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.classList.add('highlight-pulse');
        setTimeout(() => el.classList.remove('highlight-pulse'), 2500);
    }
};

// Scrolls a remarks chat area to the bottom (newest message)
window.scrollRemarkAreaToBottom = function (elementId) {
    const el = document.getElementById(elementId);
    if (el) el.scrollTop = el.scrollHeight;
};

// Plays the send-bounce animation on the send button
window.animateSendButton = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.classList.remove('send-bounce');
    void el.offsetWidth; // force reflow to restart animation
    el.classList.add('send-bounce');
    setTimeout(() => el.classList.remove('send-bounce'), 400);
};
