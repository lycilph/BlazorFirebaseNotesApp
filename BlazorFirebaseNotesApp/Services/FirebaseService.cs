using System.Net.Http.Json;
using BlazorFirebaseNotesApp.Models;

namespace BlazorFirebaseNotesApp.Services;

public class FirebaseService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly string? _apiKey;
    private readonly string? _firestoreApiUrl;

    public FirebaseService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
        _apiKey = _config["Firebase:ApiKey"];
        var projectId = _config["Firebase:ProjectId"];
        _firestoreApiUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    // GET all notes
    public async Task<List<Note>> GetNotesAsync()
    {
        var response = await _http.GetFromJsonAsync<FirestoreCollection>($"{_firestoreApiUrl}/notes?key={_apiKey}");
        if (response?.documents == null)
        {
            return new List<Note>();
        }

        return response.documents.Select(doc => new Note
        {
            // Extracting the document ID from the 'name' field
            Id = doc.name.Split('/').Last(),
            Text = doc.fields.Text.stringValue
        }).ToList();
    }

    // ADD a new note
    public async Task AddNoteAsync(Note note)
    {
        var firestoreDoc = new { fields = new { Text = new { stringValue = note.Text } } };
        await _http.PostAsJsonAsync($"{_firestoreApiUrl}/notes?key={_apiKey}", firestoreDoc);
    }

    // DELETE a note
    public async Task DeleteNoteAsync(string noteId)
    {
        await _http.DeleteAsync($"{_firestoreApiUrl}/notes/{noteId}?key={_apiKey}");
    }
}
