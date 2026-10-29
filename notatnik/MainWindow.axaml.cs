    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using Avalonia.Controls;

    namespace notatnik;

    public partial class MainWindow : Window
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false, true);

        private readonly ObservableCollection<NoteEntry> _notes = new();
        private readonly string _notesFilePath;

        public MainWindow()
        {
            InitializeComponent();

            _notesFilePath = Path.Combine(FindProjectDirectory(), "notatki.md");
            NotesList.ItemsSource = _notes;

            try
            {
                foreach (var note in LoadNotes(_notesFilePath))
                {
                    _notes.Add(note);
                }
            }
            catch (IOException ex)
            {
                ShowLoadError(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowLoadError(ex.Message);
            }
            catch (SecurityException ex)
            {
                ShowLoadError(ex.Message);
            }
            catch (InvalidDataException ex)
            {
                ShowLoadError(ex.Message);
            }
            catch (DecoderFallbackException ex)
            {
                ShowLoadError(ex.Message);
            }
        }

        private void SaveButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var title = TitleTextBox.Text?.Trim() ?? string.Empty;
            var content = ContentTextBox.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title) || title.Contains('\n') || title.Contains('\r'))
            {
                StatusText.Text = "Podaj tytuł w jednej linii.";
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                StatusText.Text = "Podaj treść notatki.";
                return;
            }

            var note = new NoteEntry(DateTime.Now, title, NormalizeLineEndings(content));
            var updatedNotes = _notes.ToList();
            updatedNotes.Insert(0, note);

            try
            {
                SaveNotes(_notesFilePath, updatedNotes);
            }
            catch (IOException ex)
            {
                ShowSaveError(ex.Message);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowSaveError(ex.Message);
                return;
            }
            catch (SecurityException ex)
            {
                ShowSaveError(ex.Message);
                return;
            }

            _notes.Insert(0, note);
            TitleTextBox.Text = string.Empty;
            ContentTextBox.Text = string.Empty;
            StatusText.Text = "Zapisano.";
        }

        private void ShowLoadError(string message)
        {
            SaveButton.IsEnabled = false;
            StatusText.Text = $"Nie udało się odczytać notatek: {message}";
        }

        private void ShowSaveError(string message)
        {
            StatusText.Text = $"Nie udało się zapisać notatki: {message}";
        }

        private static string FindProjectDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "notatnik.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return AppContext.BaseDirectory;
        }

        private static List<NoteEntry> LoadNotes(string path)
        {
            if (!File.Exists(path))
            {
                return new List<NoteEntry>();
            }

            var text = File.ReadAllText(path, Utf8WithoutBom);
            var lines = NormalizeLineEndings(text).Split('\n');
            var notes = new List<NoteEntry>();
            var bodyLines = new List<string>();
            DateTime createdAt = default;
            string? title = null;

            foreach (var line in lines)
            {
                if (TryParseHeader(line, out var parsedCreatedAt, out var parsedTitle))
                {
                    AddCurrentNote(notes, createdAt, title, bodyLines);
                    createdAt = parsedCreatedAt;
                    title = parsedTitle;
                    bodyLines.Clear();
                }
                else if (title is null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        throw new InvalidDataException("Plik notatek ma nieprawidłowy format.");
                    }
                }
                else
                {
                    bodyLines.Add(line);
                }
            }

            AddCurrentNote(notes, createdAt, title, bodyLines);
            if (!string.IsNullOrWhiteSpace(text) && notes.Count == 0)
            {
                throw new InvalidDataException("Plik notatek nie zawiera wpisów w obsługiwanym formacie.");
            }

            return notes.OrderByDescending(note => note.CreatedAt).ToList();
        }

        private static void AddCurrentNote(
            ICollection<NoteEntry> notes,
            DateTime createdAt,
            string? title,
            List<string> bodyLines)
        {
            if (title is null)
            {
                return;
            }

            var content = string.Join('\n', bodyLines).TrimEnd('\n');
            notes.Add(new NoteEntry(createdAt, title, content));
        }

        private static bool TryParseHeader(string line, out DateTime createdAt, out string title)
        {
            createdAt = default;
            title = string.Empty;

            if (!line.StartsWith("## ", StringComparison.Ordinal))
            {
                return false;
            }

            var separatorIndex = line.IndexOf(" — ", 3, StringComparison.Ordinal);
            if (separatorIndex < 0 ||
                !DateTime.TryParseExact(
                    line.Substring(3, separatorIndex - 3),
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out createdAt))
            {
                return false;
            }

            title = line.Substring(separatorIndex + 3);
            return true;
        }

        private static void SaveNotes(string path, IEnumerable<NoteEntry> notes)
        {
            var serializedNotes = notes.Select(note =>
                $"## {note.CreatedAt.ToString(TimestampFormat, CultureInfo.InvariantCulture)} — {note.Title}\n" +
                NormalizeLineEndings(note.Content).TrimEnd('\n'));
            var content = string.Join("\n\n", serializedNotes);
            var temporaryPath = Path.Combine(
                Path.GetDirectoryName(path)!,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            File.WriteAllText(temporaryPath, content, Utf8WithoutBom);
            File.Move(temporaryPath, path, true);
        }

        private static string NormalizeLineEndings(string value)
        {
            return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        }
    }