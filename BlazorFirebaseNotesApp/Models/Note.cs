namespace BlazorFirebaseNotesApp.Models;

// This is our clean, simple Note object for the UI
public class Note
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}