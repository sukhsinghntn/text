using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System.Net.Http.Json;

namespace NDAProcesses.Client.Services
{
    public class UserServiceProxy : IUserService
    {
        private readonly HttpClient _httpClient;

        public UserServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> ValidateUser(UserModel user)
        {
            var response = await _httpClient.PostAsJsonAsync("api/user", user);
            return await response.Content.ReadFromJsonAsync<bool>();
        }

        public async Task<UserModel> GetUserData(string userName)
        {
            return await _httpClient.GetFromJsonAsync<UserModel>($"api/user/{userName}");
        }
    }
}
