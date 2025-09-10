using Microsoft.JSInterop;

namespace NDAProcesses.Client.Services
{
    public class CookieHelper
    {
        private readonly IJSRuntime _jsRuntime;

        // Constructor to initialize IJSRuntime
        public CookieHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // Retrieves the value of a cookie by name
        public async Task<string> LoginStatus()
        {
            return await _jsRuntime.InvokeAsync<string>("cookieHelper.getCookie");
        }

        // Sets a cookie with the specified name and value
        public async Task LoginUser(string value)
        {
            await _jsRuntime.InvokeVoidAsync("cookieHelper.setCookie", value);
        }

        // Deletes a cookie by name
        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync("cookieHelper.deleteCookie");
        }
    }
}
