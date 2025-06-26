using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorFirebaseNotesApp.Services;

public class FirebaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public FirebaseAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(_anonymous);
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt")));
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }

    // Helper to parse claims from JWT
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null)
            throw new ArgumentNullException(nameof(keyValuePairs));

        if (keyValuePairs.TryGetValue(ClaimTypes.Name, out var name))
            claims.Add(new Claim(ClaimTypes.Name, name.ToString()));
        if (keyValuePairs.TryGetValue("user_id", out var userId)) // Firebase uses "user_id"
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        if (keyValuePairs.TryGetValue("email", out var email))
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        if (keyValuePairs.TryGetValue("email_verified", out var emailVerified))
        {
            // The claim value must be a string. We'll use "True" or "False".
            claims.Add(new Claim("email_verified", emailVerified.ToString()));
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}