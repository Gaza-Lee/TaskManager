using Microsoft.JSInterop;

namespace TaskManager.Services;

public class UserIdentityService(IJSRuntime js)
{
    private string? _cachedName;
    public event Action? OnUserChanged;

    public async Task<string?> GetCurrentUserAsync()
    {
        if (_cachedName is not null)
            return _cachedName;

        try
        {
            _cachedName = await js.InvokeAsync<string?>("localStorage.getItem", "taskHub_userName");
        }
        catch
        {
            // JS interop not available during prerender
            return null;
        }
        return _cachedName;
    }

    public async Task SetCurrentUserAsync(string name)
    {
        _cachedName = name.Trim();
        await js.InvokeVoidAsync("localStorage.setItem", "taskHub_userName", _cachedName);
        OnUserChanged?.Invoke();
    }

    public async Task<bool> HasUserAsync()
    {
        var name = await GetCurrentUserAsync();
        return !string.IsNullOrWhiteSpace(name);
    }
}
