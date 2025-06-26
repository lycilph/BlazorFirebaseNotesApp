namespace BlazorFirebaseNotesApp.Models;

// Represents a value in a document field, e.g., a string.
public class StringValue
{
    public string stringValue { get; set; } = string.Empty;
}

// Firestore's REST API represents integers as strings.
public class IntegerValue
{
    public string integerValue { get; set; }
}

// Represents the fields within a Firestore document.
public class NoteFields
{
    public StringValue Text { get; set; } = new();
    public StringValue UserId { get; set; } = new(); // Add this line
}

public class UserProfileFields
{
    public IntegerValue count { get; set; } = new();
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

public class FirestoreUserProfileDocument
{
    public string name { get; set; }
    public UserProfileFields fields { get; set; }
    public DateTime createTime { get; set; }
    public DateTime updateTime { get; set; }
}