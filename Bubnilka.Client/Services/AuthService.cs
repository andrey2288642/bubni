using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Bubnilka.Shared.DTOs;

namespace Bubnilka.Client.Services;

/// <summary>
/// Сервис аутентификации с безопасным хранением токена
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly string _tokenKey = "auth_token";

    public event EventHandler<bool>? AuthStateChanged;

    private AuthUser? _currentUser;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public string? Token { get; private set; }

    public AuthUser? CurrentUser => _currentUser;

    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    public async Task<AuthResponse> Register(string email, string password, string confirmPassword, string? displayName)
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword,
            DisplayName = displayName
        };

        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                await SetToken(result.Token);
                _currentUser = new AuthUser
                {
                    Id = result.UserId,
                    Email = result.Email,
                    DisplayName = result.DisplayName,
                    IsOnline = true
                };
                AuthStateChanged?.Invoke(this, true);
            }
            return result ?? AuthResponse.ErrorResponse("Ошибка регистрации");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return error ?? AuthResponse.ErrorResponse("Пользователь уже существует");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return AuthResponse.ErrorResponse($"Ошибка сервера: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    public async Task<AuthResponse> Login(string email, string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                await SetToken(result.Token);
                _currentUser = new AuthUser
                {
                    Id = result.UserId,
                    Email = result.Email,
                    DisplayName = result.DisplayName,
                    IsOnline = true
                };
                AuthStateChanged?.Invoke(this, true);
            }
            return result ?? AuthResponse.ErrorResponse("Ошибка входа");
        }
        else
        {
            var error = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return error ?? AuthResponse.ErrorResponse("Неверный email или пароль");
        }
    }

    /// <summary>
    /// Выход пользователя
    /// </summary>
    public async Task Logout()
    {
        try
        {
            await _httpClient.PostAsJsonAsync("api/auth/logout", new { });
        }
        catch
        {
            // Игнорируем ошибки при выходе
        }

        await ClearToken();
        _currentUser = null;
        AuthStateChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Получение текущего пользователя
    /// </summary>
    public async Task<AuthUser?> GetCurrentUser()
    {
        if (!IsAuthenticated)
            return null;

        try
        {
            var response = await _httpClient.GetAsync("api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<AuthUser>();
                _currentUser = user;
                return user;
            }
        }
        catch
        {
            // Игнорируем ошибки
        }

        return _currentUser;
    }

    /// <summary>
    /// Инициализация сервиса (загрузка токена из хранилища)
    /// </summary>
    public async Task Initialize()
    {
        Token = await _localStorage.GetItemAsStringAsync(_tokenKey);
        
        if (!string.IsNullOrEmpty(Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            
            await GetCurrentUser();
            AuthStateChanged?.Invoke(this, true);
        }
    }

    private async Task SetToken(string token)
    {
        Token = token;
        await _localStorage.SetItemAsStringAsync(_tokenKey, token);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task ClearToken()
    {
        Token = null;
        await _localStorage.RemoveItemAsync(_tokenKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}

/// <summary>
/// Модель текущего пользователя
/// </summary>
public class AuthUser
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
