namespace BlazorFirebaseNotesApp.Models;

// Represents a value in a document field, e.g., a string.
public class StringValue
{
    public string stringValue { get; set; } = string.Empty;
}

// Represents the fields within a Firestore document.
public class NoteFields
{
    public StringValue Text { get; set; } = new();
    public StringValue UserId { get; set; } = new(); // Add this line
}

// Represents a full Firestore document.
public class FirestoreDocument
{
    public string name { get; set; } = string.Empty; // Format: projects/{projectId}/databases/(default)/documents/notes/{documentId}
    public NoteFields fields { get; set; } = new();
    public DateTime createTime { get; set; }
    public DateTime updateTime { get; set; }
}

// Represents a collection of documents returned from the API.
public class FirestoreCollection
{
    public List<FirestoreDocument> documents { get; set; } = new();
}