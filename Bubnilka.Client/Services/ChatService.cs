using System.Net.Http.Json;

namespace Bubnilka.Client.Services;

/// <summary>
/// Сервис для работы с чатами с проверкой ответов
/// </summary>
public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public ChatService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    /// <summary>
    /// Получение списка чатов
    /// </summary>
    public async Task<List<dynamic>> GetChats()
    {
        EnsureAuthenticated();

        var response = await _httpClient.GetAsync("api/chats");
        
        if (response.IsSuccessStatusCode)
        {
            var chats = await response.Content.ReadFromJsonAsync<List<dynamic>>() ?? new List<dynamic>();
            return chats;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorized();
            return new List<dynamic>();
        }
        else
        {
            throw new HttpRequestException($"Ошибка получения чатов: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Создание чата
    /// </summary>
    public async Task<dynamic> CreateChat(int otherUserId)
    {
        EnsureAuthenticated();

        var request = new { OtherUserId = otherUserId };
        var response = await _httpClient.PostAsJsonAsync("api/chats", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<dynamic>() ?? new { };
            return result;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorized();
            throw new UnauthorizedAccessException();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Ошибка создания чата: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Получение сообщений чата
    /// </summary>
    public async Task<List<dynamic>> GetMessages(int chatId)
    {
        EnsureAuthenticated();

        var response = await _httpClient.GetAsync($"api/chats/{chatId}/messages");

        if (response.IsSuccessStatusCode)
        {
            var messages = await response.Content.ReadFromJsonAsync<List<dynamic>>() ?? new List<dynamic>();
            return messages;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorized();
            return new List<dynamic>();
        }
        else
        {
            throw new HttpRequestException($"Ошибка получения сообщений: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Поиск пользователей
    /// </summary>
    public async Task<List<dynamic>> SearchUsers(string query)
    {
        EnsureAuthenticated();

        var response = await _httpClient.GetAsync($"api/chats/users/search?query={Uri.EscapeDataString(query)}");

        if (response.IsSuccessStatusCode)
        {
            var users = await response.Content.ReadFromJsonAsync<List<dynamic>>() ?? new List<dynamic>();
            return users;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorized();
            return new List<dynamic>();
        }
        else
        {
            throw new HttpRequestException($"Ошибка поиска: {response.StatusCode}");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_authService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Пользователь не авторизован");
        }
    }

    private async Task HandleUnauthorized()
    {
        await _authService.Logout();
    }
}
