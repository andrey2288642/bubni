using Microsoft.AspNetCore.SignalR.Client;

namespace Bubnilka.Client.Services;

/// <summary>
/// Сервис для работы с SignalR с безопасным подключением
/// </summary>
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _serverUrl;

    public event Action<dynamic>? OnMessageReceived;
    public event Action<dynamic>? OnUserStatusChanged;
    public event Action<string>? OnError;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _serverUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:5001";
    }

    /// <summary>
    /// Подключение к SignalR хабу
    /// </summary>
    public async Task Connect()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        try
        {
            var token = _authService.Token;
            if (string.IsNullOrEmpty(token))
                return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Регистрируем обработчики
            _hubConnection.On<dynamic>("ReceiveMessage", (message) =>
            {
                OnMessageReceived?.Invoke(message);
            });

            _hubConnection.On<dynamic>("UserStatusChanged", (status) =>
            {
                OnUserStatusChanged?.Invoke(status);
            });

            _hubConnection.On<string>("Error", (error) =>
            {
                OnError?.Invoke(error);
            });

            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Ошибка подключения к SignalR: {ex.Message}");
        }
    }

    /// <summary>
    /// Отправка сообщения
    /// </summary>
    public async Task SendMessage(int chatId, string content)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            await Connect();
        }

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("SendMessage", chatId, content);
        }
        else
        {
            throw new InvalidOperationException("Не подключено к серверу");
        }
    }

    /// <summary>
    /// Присоединение к чату
    /// </summary>
    public async Task JoinChat(int chatId)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("JoinChat", chatId);
        }
    }

    /// <summary>
    /// Выход из чата
    /// </summary>
    public async Task LeaveChat(int chatId)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("LeaveChat", chatId);
        }
    }

    /// <summary>
    /// Обновление статуса онлайн
    /// </summary>
    public async Task UpdateStatus(bool isOnline)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("UpdateStatus", isOnline);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
