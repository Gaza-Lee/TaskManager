using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    /// <summary>
    /// Wraps IJSRuntime to provide a clean C# API for browser-specific actions.
    /// </summary>
    public class BrowserService
    {
        private readonly IJSRuntime _js;

        public BrowserService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Smoothly scrolls much needed elements into view.
        /// </summary>
        public async ValueTask ScrollToElementAsync(string elementId)
        {
            await _js.InvokeVoidAsync("scrollToTask", elementId);
        }

        /// <summary>
        /// Ensures a scrollable container (like the remarks chat) is scrolled to its bottom.
        /// </summary>
        public async ValueTask ScrollToBottomAsync(string elementId)
        {
            await _js.InvokeVoidAsync("scrollRemarkAreaToBottom", elementId);
        }

        /// <summary>
        /// Triggers a bounce animation on an element for visual feedback.
        /// </summary>
        public async ValueTask AnimateSendButtonAsync(string elementId)
        {
            await _js.InvokeVoidAsync("animateSendButton", elementId);
        }
    }
}
