using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI;

namespace InStelle
{
    public sealed partial class MainWindow : Window
    {
        private readonly List<TabData> tabs = new();
        private TabData? currentTab = null;
        private static readonly string savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Instelle",
            "notesAppData.json"
        );

        public MainWindow()
        {
            this.InitializeComponent();

            // Set up main window appearance via a Grid or root element
            var rootGrid = (Grid)this.Content;

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
                Tag = tab,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 201, 99, 110)), // Red accent
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 81, 53, 89)) // Darker purple
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

            // Add note cards
            foreach (var note in currentTab.Notes)
            {
                NotesPanel.Children.Add(CreateCard(note.Title, note.Description, () => ShowNoteDetails(note)));
            }

            // Add the "Add Note" card
            NotesPanel.Children.Add(CreateCard("+ Add Note", "", () => AddNote_Click(null, null), isAddCard: true));
        }

        private void AddNote_Click(object? sender, RoutedEventArgs? e)
        {
            if (currentTab == null) return;

            var newNote = new Note();
            var noteWindow = new NoteWindow(newNote, currentTab, RefreshNotes, SaveTabs);

            noteWindow.Closed += (s, args) =>
            {
                if (noteWindow.NoteSaved)
                {
                    currentTab.Notes.Add(newNote);
                    RefreshNotes();
                    SaveTabs();
                }
            };

            // Delay the activation to ensure proper z-order
            DispatcherQueue.TryEnqueue(() => noteWindow.Activate());
        }

        private Border CreateCard(string title, string description, Action onClick, bool isAddCard = false)
        {
            var card = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 111, 79, 110))
            };

            // Add title or main text
            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = isAddCard ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap
            };
            card.Children.Add(titleText);

            // Add description for note cards (skip for Add Note card)
            if (!isAddCard)
            {
                var descriptionText = new TextBlock
                {
                    Text = description,
                    TextWrapping = TextWrapping.Wrap,
                    MaxLines = 2,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                card.Children.Add(descriptionText);
            }

            // Wrap the card in a Border to handle click events
            var border = new Border
            {
                Child = card,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100))
            };

            // Assign click/tap behavior
            border.PointerReleased += (s, e) => onClick();

            return border;
        }

        private void ShowNoteDetails(Note note)
        {
            if (currentTab == null)
                return;

            var noteWindow = new NoteWindow(note, currentTab, RefreshNotes, SaveTabs);
            noteWindow.Activate(); // Show the window

            // Delay the activation to ensure proper z-order
            DispatcherQueue.TryEnqueue(() => noteWindow.Activate());
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
                        tabs.AddRange(loadedTabs);

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
