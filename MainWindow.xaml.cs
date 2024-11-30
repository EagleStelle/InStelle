using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InStelle
{
    public sealed partial class MainWindow : Window
    {
        private List<TabData> tabs = new();
        private TabData? currentTab = null;
        private static readonly string savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Instelle",
            "notesAppData.json"
        );

        public MainWindow()
        {
            this.InitializeComponent();
            LoadTabs();
        }

        private void AddTab_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

            var newTab = new TabData("📄");
            tabs.Add(newTab);

            var tabButton = CreateTabButton(newTab);
            TabPanel.Children.Insert(TabPanel.Children.Count - 1, tabButton);

            SetActiveTab(newTab);
            SaveTabs();
        }

        private Button CreateTabButton(TabData tab)
        {
            var button = new Button
            {
                Content = tab.Icon,
                Width = 50,
                Height = 50,
                Margin = new Thickness(5),
                Tag = tab
            };
            button.Click += (s, e) => SetActiveTab(tab);
            return button;
        }

        private void SetActiveTab(TabData tab)
        {
            currentTab = tab;
            RefreshNotes();
        }

        private void RefreshNotes()
        {
            NotesPanel.Children.Clear();
            if (currentTab == null) return;

            foreach (var note in currentTab.Notes)
            {
                NotesPanel.Children.Add(CreateNoteCard(note));
            }

            var addNoteButton = new Button
            {
                Content = "+ Add Note",
                Width = 200,
                Height = 50,
                Margin = new Thickness(5)
            };
            addNoteButton.Click += AddNote_Click;
            NotesPanel.Children.Add(addNoteButton);
        }

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == null) return;

            // Create a new note instance (but don't add it to the tab yet)
            var newNote = new Note
            {
            };

            // Create the NoteWindow and pass the note
            var noteWindow = new NoteWindow(newNote, currentTab, RefreshNotes, SaveTabs);

            // Handle the closing of the NoteWindow
            noteWindow.Closed += (s, args) =>
            {
                // Only add the note if it was saved
                if (noteWindow.NoteSaved)
                {
                    currentTab.Notes.Add(newNote);
                    RefreshNotes();
                    SaveTabs();
                }
            };

            // Open the NoteWindow
            noteWindow.Activate();
        }


        private Border CreateNoteCard(Note note)
        {
            var card = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            };

            // Title: Bold and larger text
            var title = new TextBlock
            {
                Text = note.Title,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16, // Slightly larger
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Description with truncation
            var description = new TextBlock
            {
                Text = note.Description,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2, // Limit to 2 lines
                TextTrimming = TextTrimming.CharacterEllipsis // Add ellipsis for overflowed text
            };

            // Wrap the card in a Border to handle click events
            var noteCard = new Border
            {
                Child = card,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            // Handle click to view note details
            noteCard.Tapped += (s, e) => ShowNoteDetails(note);

            // Add elements to the card
            card.Children.Add(title);
            card.Children.Add(description);

            return noteCard;
        }


        private void ShowNoteDetails(Note note)
        {
            if (currentTab == null)
                return;

            var noteWindow = new NoteWindow(note, currentTab, RefreshNotes, SaveTabs);
            noteWindow.Activate(); // Show the window
        }

        private void SaveTabs()
        {
            try
            {
                var json = JsonSerializer.Serialize(tabs);
                File.WriteAllText(savePath, json);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save tabs: {ex.Message}");
            }
        }

        private void LoadTabs()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    var json = File.ReadAllText(savePath);
                    var loadedTabs = JsonSerializer.Deserialize<List<TabData>>(json);

                    if (loadedTabs != null)
                    {
                        tabs = loadedTabs;

                        foreach (var tab in tabs)
                        {
                            var tabButton = CreateTabButton(tab);
                            TabPanel.Children.Insert(TabPanel.Children.Count - 1, tabButton);
                        }

                        if (tabs.Count > 0)
                        {
                            SetActiveTab(tabs[0]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load tabs: {ex.Message}");
                tabs = new List<TabData>();
            }
        }

        private async void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    public class TabData
    {
        public string Icon { get; set; }
        public List<Note> Notes { get; set; }

        public TabData(string icon)
        {
            Icon = icon ?? throw new ArgumentNullException(nameof(icon));
            Notes = new List<Note>();
        }
    }

    public class Note
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
