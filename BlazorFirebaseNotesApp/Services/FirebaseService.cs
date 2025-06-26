using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using BlazorFirebaseNotesApp.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorFirebaseNotesApp.Services;

public class FirebaseService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly string? _apiKey; // Add the API key back
    private readonly string _basePath; // We'll store the common path here.

    public FirebaseService(HttpClient http,
                           IConfiguration config,
                           AuthenticationStateProvider authStateProvider,
                           ILocalStorageService localStorage)
    {
        _http = http;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _apiKey = config["Firebase:ApiKey"];

        // Define the common part of our API paths.
        var projectId = config["Firebase:ProjectId"];
        _basePath = $"v1/projects/{projectId}/databases/(default)/documents";
    }

    private async Task<string?> GetUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        // The user's unique ID from Firebase is in the NameIdentifier claim
        return user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task AddAuthHeaderToRequest(HttpRequestMessage request)
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return new List<Note>();

        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "notes" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "userId" },
                        op = "EQUAL",
                        value = new { stringValue = userId }
                    }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}:runQuery?key={_apiKey}")
        {
            Content = JsonContent.Create(query)
        };
        await AddAuthHeaderToRequest(request); // Add the header manually

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // The response for runQuery is a bit different, it's an array of documents.
        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonResponse);

        var notes = new List<Note>();
        foreach (var element in jsonDoc.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("document", out var document))
            {
                notes.Add(new Note
                {
                    Id = document.GetProperty("name").GetString().Split('/').Last(),
                    Text = document.GetProperty("fields").GetProperty("text").GetProperty("stringValue").GetString(),
                    UserId = document.GetProperty("fields").GetProperty("userId").GetProperty("stringValue").GetString()
                });
            }
        }
        return notes;
    }

    public async Task<Note> AddNoteAsync(Note note)
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return null;

        note.UserId = userId;
        var firestoreDoc = new
        {
            fields = new
            {
                Text = new { stringValue = note.Text },
                userId = new { stringValue = note.UserId }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/notes?key={_apiKey}")
        {
            Content = JsonContent.Create(firestoreDoc)
        };
        await AddAuthHeaderToRequest(request); // Add the header manually

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Note>(); // Assuming conversion logic
    }

    public async Task DeleteNoteAsync(string noteId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_basePath}/notes/{noteId}?key={_apiKey}");
        await AddAuthHeaderToRequest(request); // Add the header manually

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets the current count for the logged-in user by using a query.
    /// This is the most robust method as it uses the :runQuery endpoint,
    /// which has a reliable CORS policy and works perfectly with the Authorization header.
    /// </summary>
    public async Task<int> GetUserCountAsync()
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return 0;

        // This query finds a document in the 'userProfiles' collection
        // where the document's own name/ID matches the current user's ID.
        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "userProfiles" } },
                where = new
                {
                    fieldFilter = new
                    {
                        // __name__ is a special field representing the document ID
                        field = new { fieldPath = "__name__" },
                        op = "EQUAL",
                        value = new
                        {
                            // The value must be the full resource path
                            //referenceValue = $"{_basePath}/userProfiles/{userId}"
                            referenceValue = $"projects/blazor-notes-app/databases/(default)/documents/userProfiles/{userId}"
                        }
                    }
                },
                limit = 1 // We only expect one result
            }
        };

        // Use a POST request to the :runQuery endpoint
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}:runQuery?key={_apiKey}")
        {
            Content = JsonContent.Create(query)
        };

        // Add the auth header, just like our other working methods
        await AddAuthHeaderToRequest(request);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonResponse);

        // Check if the query returned any results
        var firstResult = jsonDoc.RootElement.EnumerateArray().FirstOrDefault();
        if (firstResult.TryGetProperty("document", out var document))
        {
            // We found the profile document, now parse the count
            if (document.TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty("count", out var countField) &&
                countField.TryGetProperty("integerValue", out var integerValue))
            {
                _ = int.TryParse(integerValue.GetString(), out int currentCount);
                return currentCount;
            }
        }

        // If we reach here, no document was found (new user), so return 0.
        return 0;
    }

    public async Task SetUserCountAsync(int count)
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return;

        var url = $"{_basePath}/userProfiles/{userId}?updateMask.fieldPaths=count&key={_apiKey}";
        var payload = new { fields = new { count = new { integerValue = count.ToString() } } };

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-HTTP-Method-Override", "PATCH");
        await AddAuthHeaderToRequest(request); // Add the header manually

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
