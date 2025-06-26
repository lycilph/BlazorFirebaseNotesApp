using System.Net.Http.Json;

namespace BlazorFirebaseNotesApp.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private const string BaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";

    public AuthService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Firebase:ApiKey"];
    }

    public async Task<FirebaseAuthResponse?> LoginAsync(string email, string password)
    {
        var request = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync($"{BaseUrl}signInWithPassword?key={_apiKey}", request);
        return await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
    }

    public async Task<FirebaseAuthResponse?> SignUpAsync(string email, string password)
    {
        var request = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync($"{BaseUrl}signUp?key={_apiKey}", request);
        return await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
    }

    public async Task SendVerificationEmailAsync(string idToken)
    {
        var request = new { requestType = "VERIFY_EMAIL", idToken };
        await _http.PostAsJsonAsync($"{BaseUrl}sendOobCode?key={_apiKey}", request);
    }

    public async Task ConfirmEmailVerificationAsync(string oobCode)
    {
        var request = new { oobCode };
        await _http.PostAsJsonAsync($"{BaseUrl}update?key={_apiKey}", request);
    }
}

// Helper class to deserialize Firebase Auth responses
public class FirebaseAuthResponse
{
    public string kind { get; set; }
    public string idToken { get; set; }
    public string email { get; set; }
    public string refreshToken { get; set; }
    public string expiresIn { get; set; }
    public string localId { get; set; }
}