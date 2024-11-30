using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Shapes = Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using System.Diagnostics;

namespace InStelle
{
    public sealed partial class MainPage : Page
    {
        private static int lastActiveTabIndex = 0; // Static field to persist the tab index
        private TabData? currentTab = null;
        private readonly List<TabData> tabs = new();
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

        private StackPanel CreateTabStackPanel(TabData tab)
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri(tab.Icon)),
                Width = 100,
                Height = 100,
                Stretch = Stretch.UniformToFill
            };

            // Create a horizontal bar (Rectangle) for selection indication
            var selectionBar = new Shapes.Rectangle
            {
                Height = 10,
                Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 201, 99, 110)), // Red Accent
                Visibility = Visibility.Collapsed // Hidden by default
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 100,
                Margin = new Thickness(5),
                Tag = tab,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)), // Red accent
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(selectionBar);

            // Click behavior to activate tab
            stackPanel.PointerPressed += (s, e) => SetActiveTab(tab);

            // Context menu for Edit and Delete
            var menuFlyout = new MenuFlyout();

            var editMenuItem = new MenuFlyoutItem { Text = "Edit Tab" };
            editMenuItem.Click += (s, e) => EditTab(tab, stackPanel);
            menuFlyout.Items.Add(editMenuItem);

            var deleteMenuItem = new MenuFlyoutItem { Text = "Delete Tab" };
            deleteMenuItem.Click += (s, e) => DeleteTab(tab, stackPanel);
            menuFlyout.Items.Add(deleteMenuItem);

            stackPanel.ContextFlyout = menuFlyout;

            // Store the selection bar in the stack panel's Tag for easier access later
            stackPanel.Tag = new Tuple<TabData, Shapes.Rectangle>(tab, selectionBar);

            return stackPanel;
        }

        public async void AddTab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = await CreateTabDialog();

            if (dialog.Result == ContentDialogResult.Primary && dialog.ImagePath != null)
            {
                var newTab = new TabData(dialog.ImagePath);
                tabs.Add(newTab);

                var tabButton = CreateTabStackPanel(newTab);
                TabPanel.Children.Insert(TabPanel.Children.Count - 1, tabButton);

                SetActiveTab(newTab);
                SaveTabs();
            }
        }

        private async void EditTab(TabData tab, StackPanel stackPanel)
        {
            var dialog = await CreateTabDialog(tab.Icon);

            if (dialog.Result == ContentDialogResult.Primary && dialog.ImagePath != null)
            {
                tab.Icon = dialog.ImagePath;

                // Update the image in the StackPanel
                var image = stackPanel.Children[0] as Image;
                if (image != null)
                {
                    image.Source = new BitmapImage(new Uri(tab.Icon));
                }

                SaveTabs();
            }
        }

        private async Task<(ContentDialogResult Result, string? ImagePath)> CreateTabDialog(string? existingImagePath = null)
        {
            string? selectedImagePath = existingImagePath;
            Windows.Storage.StorageFile? droppedImageFile = null;

            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Save"
            };

            // Create a centered title using a TextBlock
            var titleTextBlock = new TextBlock
            {
                Text = "Tab Image",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10), // Add margins to adjust positioning
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)) // Match the theme color
            };

            // Create the dialog content
            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(titleTextBlock);

            var instructionText = new TextBlock
            {
                Text = "Drop an image here:",
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(instructionText);

            var dropArea = new Border
            {
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 300, // Controls the square size (300px x 300px)
                Width = 300,  // Keep it square
                Child = new TextBlock
                {
                    Text = "Drop Image Here",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)),
                }
            };

            stackPanel.Children.Add(dropArea);

            dialog.Content = stackPanel;

            // Event handling for image drop
            dropArea.AllowDrop = true;

            dropArea.DragEnter += (s, e) =>
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                dropArea.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 234, 178, 178));
            };

            dropArea.DragLeave += (s, e) =>
            {
                dropArea.Background = new SolidColorBrush(Colors.Transparent);
            };

            dropArea.Drop += async (s, e) =>
            {
                if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file &&
                        (file.FileType == ".jpg" || file.FileType == ".png" || file.FileType == ".jpeg"))
                    {
                        droppedImageFile = file;
                        dropArea.Child = new Image
                        {
                            Source = new BitmapImage(new Uri(file.Path)),
                            Stretch = Stretch.UniformToFill, // Ensures image keeps its aspect ratio but fills the square
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                    }
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Delete the previous image if it exists
                if (!string.IsNullOrEmpty(existingImagePath) && File.Exists(existingImagePath))
                {
                    try
                    {
                        File.Delete(existingImagePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting old image: {ex.Message}");
                    }
                }

                // Save the new image if one was dropped
                if (droppedImageFile != null)
                {
                    selectedImagePath = await SaveImageToAppData(droppedImageFile);
                }
            }

            return (result, selectedImagePath);
        }

        private async Task<string> SaveImageToAppData(Windows.Storage.StorageFile file)
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InStelle");
            Directory.CreateDirectory(appDataPath);

            var imagePath = Path.Combine(appDataPath, $"{Guid.NewGuid()}{file.FileType}");
            await file.CopyAsync(await Windows.Storage.StorageFolder.GetFolderFromPathAsync(appDataPath),
                Path.GetFileName(imagePath), Windows.Storage.NameCollisionOption.ReplaceExisting);

            return imagePath;
        }

        private void DeleteTab(TabData tab, StackPanel stackPanel)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tab.Icon) && File.Exists(tab.Icon))
                {
                    File.Delete(tab.Icon);
                }

                tabs.Remove(tab);
                TabPanel.Children.Remove(stackPanel);

                if (currentTab == tab)
                {
                    currentTab = tabs.Count > 0 ? tabs[0] : null;
                    RefreshNotes();
                }

                SaveTabs();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to delete tab: {ex.Message}");
            }
        }

        private void SetActiveTab(TabData tab)
        {
            currentTab = tab;

            // Update all tab buttons
            foreach (var child in TabPanel.Children)
            {
                if (child is Button tabButton && tabButton.Tag is Tuple<TabData, Shapes.Rectangle> tagData)
                {
                    var buttonTab = tagData.Item1;
                    var selectionBar = tagData.Item2;

                    if (buttonTab == currentTab)
                    {
                        selectionBar.Visibility = Visibility.Visible; // Show bar for active tab
                        tabButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 129, 131)); // Active tab color
                    }
                    else
                    {
                        selectionBar.Visibility = Visibility.Collapsed; // Hide bar for inactive tabs
                        tabButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 201, 99, 110)); // Inactive tab color
                    }
                }
            }

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
            NotesPanel.Children.Add(CreateCard("+ Add Note", "", () => AddNote_Click(this, new RoutedEventArgs()), isAddCard: true));
        }

        public void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == null) return;

            var newNote = new Note();

            // Save the current tab index to the static field
            lastActiveTabIndex = tabs.IndexOf(currentTab);

            // Navigate to NotePage
            Frame.Navigate(typeof(NotePage), Tuple.Create(newNote, currentTab, RefreshNotes, SaveTabs));
        }


        private void ShowNoteDetails(Note note)
        {
            if (currentTab == null) return;

            // Save the current tab index to the static field
            lastActiveTabIndex = tabs.IndexOf(currentTab);

            // Navigate to the NotePage with parameters
            Frame.Navigate(typeof(NotePage), Tuple.Create(note, currentTab, RefreshNotes, SaveTabs));
        }

        private Button CreateCard(string title, string description, Action onClick, bool isAddCard = false)
        {
            var card = new Button
            {
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = isAddCard
                    ? new SolidColorBrush(ColorHelper.FromArgb(255, 234, 178, 178)) // Add Note card color (peach/pink)
                    : new SolidColorBrush(ColorHelper.FromArgb(255, 111, 79, 110)),  // Default note color
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(0), // Optional: remove border
                HorizontalAlignment = HorizontalAlignment.Stretch, // Ensures the button stretches widthwise
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch, // Ensures content alignment
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // Use a StackPanel as the content of the button to stack the TextBlock elements
            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch, // Ensures the panel stretches with the button
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16,
                Foreground = isAddCard ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White), // Black for Add Note, white for others
                Margin = new Thickness(0, 0, 0, isAddCard ? 0 : 5), // No bottom margin for Add Note
                HorizontalAlignment = isAddCard ? HorizontalAlignment.Center : HorizontalAlignment.Left, // Center for Add Note, left for others
                TextAlignment = isAddCard ? TextAlignment.Center : TextAlignment.Left, // Center for Add Note, left for others
                TextWrapping = TextWrapping.Wrap,
                Visibility = string.IsNullOrWhiteSpace(title) ? Visibility.Collapsed : Visibility.Visible
            };
            contentPanel.Children.Add(titleText);

            var descriptionText = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.White), // White for regular note description
                MaxLines = string.IsNullOrWhiteSpace(title) ? int.MaxValue : 2, // Expand description if no title
                TextTrimming = string.IsNullOrWhiteSpace(title) ? TextTrimming.None : TextTrimming.CharacterEllipsis,
                FontWeight = string.IsNullOrWhiteSpace(title) ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                FontSize = string.IsNullOrWhiteSpace(title) ? 16 : 14,
                Visibility = string.IsNullOrWhiteSpace(description) ? Visibility.Collapsed : Visibility.Visible,
                HorizontalAlignment = HorizontalAlignment.Left // Aligns the description to the left
            };
            contentPanel.Children.Add(descriptionText);

            // Set the StackPanel as the content of the Button
            card.Content = contentPanel;

            // Add click event
            card.Click += (s, e) => onClick();

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
                            var tabStackPanel = CreateTabStackPanel(tab);
                            TabPanel.Children.Insert(TabPanel.Children.Count - 1, tabStackPanel);
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Restore the active tab using the saved index
            if (tabs.Count > 0 && lastActiveTabIndex >= 0 && lastActiveTabIndex < tabs.Count)
            {
                SetActiveTab(tabs[lastActiveTabIndex]);
            }

            System.Diagnostics.Debug.WriteLine($"Navigating back to MainPage. Restoring Tab Index: {lastActiveTabIndex}");
        }
    }

    public class TabData
    {
        public string Icon { get; set; } // Path to the tab's image
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
