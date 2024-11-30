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
    public sealed partial class MainPage : Page
    {
        private readonly List<TabData> tabs = new();
        private TabData? currentTab = null;
        private static readonly string savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Instelle",
            "notesAppData.json"
        );

        public MainPage()
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
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)), // Red accent
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 81, 53, 89)) // Darker purple
            };

            // Set click behavior to activate tab
            button.Click += (s, e) => SetActiveTab(tab);

            // Create the context menu
            var menuFlyout = new MenuFlyout();

            // Add Edit option
            var editMenuItem = new MenuFlyoutItem { Text = "Edit Tab" };
            editMenuItem.Click += (s, e) => EditTab(tab, button);
            menuFlyout.Items.Add(editMenuItem);

            // Add Delete option
            var deleteMenuItem = new MenuFlyoutItem { Text = "Delete Tab" };
            deleteMenuItem.Click += (s, e) => DeleteTab(tab, button);
            menuFlyout.Items.Add(deleteMenuItem);

            // Attach the context menu to the button
            button.ContextFlyout = menuFlyout;

            return button;
        }

        private async void EditTab(TabData tab, Button button)
        {
            var dialog = new ContentDialog
            {
                Title = "Edit Tab",
                XamlRoot = this.Content.XamlRoot,
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Save"
            };

            // Input fields for icon and name
            var stackPanel = new StackPanel();
            var iconBox = new TextBox { PlaceholderText = "Icon (e.g., 📄)", Text = tab.Icon };
            stackPanel.Children.Add(new TextBlock { Text = "Icon:" });
            stackPanel.Children.Add(iconBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                tab.Icon = iconBox.Text;
                button.Content = tab.Icon; // Update the button's display
                SaveTabs(); // Save changes
            }
        }
        private void DeleteTab(TabData tab, Button button)
        {
            // Remove tab data and UI element
            tabs.Remove(tab);
            TabPanel.Children.Remove(button);

            // Set a new active tab if the deleted tab was active
            if (currentTab == tab)
            {
                currentTab = tabs.Count > 0 ? tabs[0] : null;
                RefreshNotes();
            }

            SaveTabs(); // Save changes
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

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == null) return;

            var newNote = new Note();

            // Navigate to the NotePage with parameters
            Frame.Navigate(typeof(NotePage), Tuple.Create(newNote, currentTab, RefreshNotes, SaveTabs));
        }

        private void ShowNoteDetails(Note note)
        {
            if (currentTab == null) return;

            // Navigate to the NotePage with parameters
            Frame.Navigate(typeof(NotePage), Tuple.Create(note, currentTab, RefreshNotes, SaveTabs));
        }

        private StackPanel CreateCard(string title, string description, Action onClick, bool isAddCard = false)
        {
            var card = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = isAddCard
                    ? new SolidColorBrush(ColorHelper.FromArgb(255, 234, 178, 178)) // Add Note card color (peach/pink)
                    : new SolidColorBrush(ColorHelper.FromArgb(255, 111, 79, 110)),  // Default note color
                CornerRadius = new CornerRadius(5)
            };

            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16,
                Foreground = isAddCard ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White), // Black for Add Note, white for others
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = isAddCard ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap
            };
            card.Children.Add(titleText);

            if (!isAddCard)
            {
                var descriptionText = new TextBlock
                {
                    Text = description,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.White), // White for regular note description
                    MaxLines = 2,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Visibility = string.IsNullOrWhiteSpace(description) ? Visibility.Collapsed : Visibility.Visible
                };
                card.Children.Add(descriptionText);
            }

            card.PointerReleased += (s, e) => onClick();

            return card;
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
