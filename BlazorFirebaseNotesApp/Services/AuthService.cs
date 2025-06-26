using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BlazorFirebaseNotesApp.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    //private const string BaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
    private const string identityBaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
    private const string tokenBaseUrl = "https://securetoken.googleapis.com/v1/token";

    public AuthService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Firebase:ApiKey"];
    }

    public async Task<FirebaseAuthResponse?> LoginAsync(string email, string password)
    {
        var request = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync($"{identityBaseUrl}signInWithPassword?key={_apiKey}", request);
        return await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
    }

    public async Task<FirebaseAuthResponse?> SignUpAsync(string email, string password)
    {
        var request = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync($"{identityBaseUrl}signUp?key={_apiKey}", request);
        return await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
    }

    public async Task SendVerificationEmailAsync(string idToken)
    {
        var request = new { requestType = "VERIFY_EMAIL", idToken };
        await _http.PostAsJsonAsync($"{identityBaseUrl}sendOobCode?key={_apiKey}", request);
    }

    public async Task ConfirmEmailVerificationAsync(string oobCode)
    {
        var request = new { oobCode };
        await _http.PostAsJsonAsync($"{identityBaseUrl}update?key={_apiKey}", request);
    }

    /// <summary>
    /// Updates the user's profile, e.g., setting their display name.
    /// </summary>
    /// <param name="idToken">The user's current ID token.</param>
    /// <param name="displayName">The name to set for the user.</param>
    /// <returns>A new auth response containing an updated ID token.</returns>
    public async Task<FirebaseAuthResponse> UpdateProfileAsync(string idToken, string displayName)
    {
        var request = new
        {
            idToken,
            displayName,
            returnSecureToken = true // CRITICAL: Ask for a new token
        };

        var response = await _http.PostAsJsonAsync($"{identityBaseUrl}update?key={_apiKey}", request);
        return await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
    }

    /// <summary>
    /// Exchanges a refresh token for a new, updated ID token.
    /// </summary>
    public async Task<FirebaseTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var requestUrl = $"{tokenBaseUrl}?key={_apiKey}";
        var requestBody = new
        {
            grant_type = "refresh_token",
            refresh_token = refreshToken
        };

        var response = await _http.PostAsJsonAsync(requestUrl, requestBody);

        // This can throw an exception on failure, which is good. We'll catch it in the UI.
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FirebaseTokenResponse>();
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

// Add a new model for the token refresh response, as it has a different structure
public class FirebaseTokenResponse
{
    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } // We get a new refresh token back
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } // This is the new, updated ID token
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }
}
