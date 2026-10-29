using System;
using System.Globalization;

namespace notatnik;

public sealed record NoteEntry(DateTime CreatedAt, string Title, string Content)
{
    public string Timestamp => CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
