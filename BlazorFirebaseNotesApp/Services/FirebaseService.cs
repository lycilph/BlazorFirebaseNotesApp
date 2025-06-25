using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BlazorFirebaseNotesApp.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorFirebaseNotesApp.Services;

public class FirebaseService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly string? _apiKey; // Add the API key back
    private readonly string _basePath; // We'll store the common path here.

    public FirebaseService(HttpClient http, IConfiguration config, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
        _apiKey = config["Firebase:ApiKey"]; // Get the API key

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

    // GET all notes for the current user
    public async Task<List<Note>> GetNotesAsync()
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return new List<Note>();

        // We now use a structured query to filter by userId
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

        var requestContent = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{_basePath}:runQuery?key={_apiKey}", requestContent);

        if (!response.IsSuccessStatusCode) return new List<Note>();

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

    // ADD a new note with the current user's ID
    public async Task AddNoteAsync(Note note)
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return;

        note.UserId = userId; // Set the user ID on the note

        var firestoreDoc = new
        {
            fields = new
            {
                Text = new { stringValue = note.Text },
                userId = new { stringValue = note.UserId } // Include userId field
            }
        };

        var response = await _http.PostAsJsonAsync($"{_basePath}/notes?key={_apiKey}", firestoreDoc);
    }

    // DELETE a note (The security rule already ensures we can only delete our own)
    public async Task DeleteNoteAsync(string noteId)
    {
        await _http.DeleteAsync($"{_basePath}/notes/{noteId}?key={_apiKey}");
    }
}
