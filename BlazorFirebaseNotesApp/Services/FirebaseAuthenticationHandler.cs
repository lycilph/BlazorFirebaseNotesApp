using System.Net.Http.Headers;
using System.Web;
using Blazored.LocalStorage;

namespace BlazorFirebaseNotesApp.Services;

// This handler will intercept every HTTP request and add the auth token.
public class FirebaseAuthenticationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public FirebaseAuthenticationHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Get the token from local storage
        var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);

        // 2. If a token exists, add it to the Authorization header
        if (!string.IsNullOrWhiteSpace(token))
        {
            // The "Bearer" scheme is the standard for JWT tokens
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // 3. Continue sending the request down the pipeline
        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}