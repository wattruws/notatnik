using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace notatnik;

public partial class MainWindow : Window
{
    private static readonly Regex NoteHeaderPattern = new(
        @"^## (?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}) — (?<title>[^\n]*)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly string NotesFilePath = ResolveNotesFilePath();
    private readonly ObservableCollection<Note> _notes = new();

    public MainWindow()
    {
        InitializeComponent();
        NotesList.ItemsSource = _notes;
        LoadNotes();
    }

    private void LoadNotes()
    {
        try
        {
            if (File.Exists(NotesFilePath))
            {
                var savedNotes = ParseNotes(File.ReadAllText(NotesFilePath, Encoding.UTF8));
                foreach (var note in savedNotes.OrderByDescending(note => note.UpdatedAt))
                {
                    _notes.Add(note);
                }
            }

            UpdateNoteCount();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Nie udało się wczytać notatek: {exception.Message}";
            StatusText.Foreground = Avalonia.Media.Brushes.Firebrick;
        }
    }

    private void SaveNote_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var title = TitleInput.Text?.Trim();
        var content = ContentInput.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            StatusText.Text = "Wpisz tytuł notatki przed zapisaniem.";
            StatusText.Foreground = Avalonia.Media.Brushes.Firebrick;
            TitleInput.Focus();
            return;
        }

        var selectedNote = NotesList.SelectedItem as Note;
        if (selectedNote is null)
        {
            selectedNote = new Note();
            _notes.Insert(0, selectedNote);
        }

        selectedNote.Title = title;
        selectedNote.Content = content;
        selectedNote.UpdatedAt = DateTimeOffset.Now;
        SortNotesNewestFirst();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NotesFilePath)!);
            File.WriteAllText(NotesFilePath, FormatNotes(_notes), new UTF8Encoding(false));
            NotesList.SelectedItem = selectedNote;
            UpdateNoteCount();
            PageTitleText.Text = "Edytuj notatkę";
            StatusText.Text = "Notatka została zapisana.";
            StatusText.Foreground = Avalonia.Media.Brushes.ForestGreen;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Nie udało się zapisać notatki: {exception.Message}";
            StatusText.Foreground = Avalonia.Media.Brushes.Firebrick;
        }
    }

    private static string ResolveNotesFilePath()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "notatnik.csproj")))
                {
                    return Path.Combine(directory.FullName, "notatki.md");
                }

                directory = directory.Parent;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "notatki.md");
    }

    private static Note[] ParseNotes(string text)
    {
        var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var headers = NoteHeaderPattern.Matches(normalizedText);
        if (headers.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(normalizedText))
            {
                throw new InvalidDataException("Plik nie zawiera notatek w rozpoznawanym formacie.");
            }

            return Array.Empty<Note>();
        }

        if (!string.IsNullOrWhiteSpace(normalizedText[..headers[0].Index]))
        {
            throw new InvalidDataException("Plik zawiera tekst przed pierwszą notatką.");
        }

        var notes = new Note[headers.Count];
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            if (!DateTimeOffset.TryParseExact(
                    header.Groups["timestamp"].Value,
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var updatedAt))
            {
                throw new InvalidDataException(
                    $"Nieprawidłowa data notatki: {header.Groups["timestamp"].Value}.");
            }

            var contentStart = header.Index + header.Length;
            if (contentStart < normalizedText.Length && normalizedText[contentStart] == '\n')
            {
                contentStart++;
            }

            var contentEnd = index + 1 < headers.Count
                ? headers[index + 1].Index
                : normalizedText.Length;

            if (index + 1 < headers.Count)
            {
                if (contentEnd < 2 || normalizedText.Substring(contentEnd - 2, 2) != "\n\n")
                {
                    throw new InvalidDataException("Notatki muszą być oddzielone pustą linią.");
                }

                contentEnd -= 2;
            }

            if (contentEnd < contentStart)
            {
                throw new InvalidDataException("Plik notatek ma nieprawidłowy format.");
            }

            notes[index] = new Note
            {
                Title = header.Groups["title"].Value,
                Content = normalizedText[contentStart..contentEnd],
                UpdatedAt = updatedAt
            };
        }

        return notes;
    }

    private static string FormatNotes(IEnumerable<Note> notes)
    {
        return string.Join(
            "\n\n",
            notes.Select(note =>
                $"## {note.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} — {note.Title.Replace("\r", " ").Replace("\n", " ")}\n{note.Content.Replace("\r\n", "\n").Replace('\r', '\n')}"));
    }

    private void SortNotesNewestFirst()
    {
        var sortedNotes = _notes.OrderByDescending(note => note.UpdatedAt).ToArray();
        for (var index = 0; index < sortedNotes.Length; index++)
        {
            var currentIndex = _notes.IndexOf(sortedNotes[index]);
            if (currentIndex != index)
            {
                _notes.Move(currentIndex, index);
            }
        }
    }

    private void NewNote_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NotesList.SelectedItem = null;
        TitleInput.Text = string.Empty;
        ContentInput.Text = string.Empty;
        PageTitleText.Text = "Nowa notatka";
        StatusText.Text = string.Empty;
        TitleInput.Focus();
    }

    private void NotesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is not Note note)
        {
            return;
        }

        TitleInput.Text = note.Title;
        ContentInput.Text = note.Content;
        PageTitleText.Text = "Edytuj notatkę";
        StatusText.Text = string.Empty;
    }

    private void UpdateNoteCount()
    {
        NoteCountText.Text = _notes.Count.ToString();
    }
}

public sealed class Note : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _content = string.Empty;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Preview));
        }
    }

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set
        {
            _updatedAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UpdatedAtLabel));
        }
    }

    public string Preview => string.Join(" ", Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Take(14));

    public string UpdatedAtLabel => $"Zmieniono: {UpdatedAt.ToLocalTime():dd.MM.yyyy, HH:mm}";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}