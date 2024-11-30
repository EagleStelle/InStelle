using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
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
using Microsoft.UI.Input;
using Windows.ApplicationModel.DataTransfer;
using System.Linq;

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

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 100,
                Padding = new Thickness(0, 0, 0, 5),
                Margin = new Thickness(5),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 201, 99, 110)), // Default tab background
                Tag = tab // Directly store TabData in the Tag property
            };

            stackPanel.Children.Add(image);
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

            return stackPanel;
        }

        private void SetActiveTab(TabData tab)
        {
            currentTab = tab;

            // Update all tab buttons
            foreach (var child in TabPanel.Children)
            {
                if (child is StackPanel tabPanel && tabPanel.Tag is TabData buttonTab)
                {
                    tabPanel.Background = buttonTab == currentTab
                        ? new SolidColorBrush(ColorHelper.FromArgb(255, 246, 239, 223)) // Active tab background
                        : new SolidColorBrush(ColorHelper.FromArgb(255, 201, 99, 110)); // Inactive tab background
                }
            }

            RefreshNotes();
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

        private UIElement CreateCard(string title, string description, Action onClick, bool isAddCard = false)
        {
            var cardGrid = new Grid
            {
                Margin = new Thickness(5),
                Background = isAddCard
                    ? new SolidColorBrush(ColorHelper.FromArgb(255, 234, 178, 178)) // Add Note card color
                    : new SolidColorBrush(ColorHelper.FromArgb(255, 111, 79, 110)), // Default note color
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Determine final title and description based on input
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            {
                title = "EagleStelle";
            }
            else if (string.IsNullOrWhiteSpace(title))
            {
                title = description;
                description = string.Empty;
            }

            // Left section for showing details
            var detailArea = new StackPanel
            {
                Margin = new Thickness(0),
                Padding = new Thickness(10, 10, 40, 10), // Reserve space for drag area on the right
                Orientation = Orientation.Vertical
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
            detailArea.Children.Add(titleText);

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
            };
            detailArea.Children.Add(descriptionText);

            detailArea.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(detailArea).Properties.IsLeftButtonPressed && !e.Handled)
                {
                    e.Handled = true; // Prevent propagation
                    onClick();
                }
            };


            // Right section for drag-and-drop
            var dragArea = new Grid
            {
                Width = 30,
                Background = new SolidColorBrush(ColorHelper.FromArgb(50, 255, 255, 255)), // Semi-transparent background
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                AllowDrop = true // Enable drop
            };

            // Change cursor to indicate drag area (requires Pointer event handlers)
            dragArea.PointerEntered += (s, e) =>
            {
                var coreWindow = Microsoft.UI.Xaml.Window.Current?.CoreWindow;
                if (coreWindow != null)
                {
                    coreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeAll, 1);
                }
            };
            dragArea.PointerExited += (s, e) =>
            {
                var coreWindow = Microsoft.UI.Xaml.Window.Current?.CoreWindow;
                if (coreWindow != null)
                {
                    coreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
                }
            };

            // Enable drag-and-drop for the right area
            dragArea.CanDrag = !isAddCard;
            dragArea.DragStarting += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    e.Cancel = true; // Cancel drag for empty titles
                    return;
                }

                Debug.WriteLine($"DragStarting: {title}");
                e.Data.SetText(title);
                e.Data.RequestedOperation = DataPackageOperation.Move;
            };

            dragArea.DragOver += (s, e) =>
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                dragArea.Background = new SolidColorBrush(ColorHelper.FromArgb(100, 0, 255, 0)); // Highlight during drag
            };

            dragArea.DragLeave += (s, e) =>
            {
                dragArea.Background = new SolidColorBrush(ColorHelper.FromArgb(50, 255, 255, 255)); // Reset background
            };

            dragArea.Drop += async (s, e) =>
            {
                Debug.WriteLine("Drop event triggered.");
                if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    var droppedTitle = await e.DataView.GetTextAsync();
                    Debug.WriteLine($"Dropped Title: {droppedTitle}, Target Title: {title}");
                    ReorderCards(droppedTitle, title);
                }
            };

            // Add both areas to the main grid
            cardGrid.Children.Add(detailArea);
            cardGrid.Children.Add(dragArea);

            return cardGrid;
        }

        private void ReorderCards(string sourceTitle, string targetTitle)
        {
            if (currentTab == null) return;

            Debug.WriteLine($"Reordering: {sourceTitle} -> {targetTitle}");

            // Find source and target notes
            var sourceNote = currentTab.Notes.Find(n => n.Title == sourceTitle);
            var targetNote = currentTab.Notes.Find(n => n.Title == targetTitle);

            if (sourceNote == null)
            {
                Debug.WriteLine("Source note not found.");
                return;
            }

            if (targetNote == null)
            {
                Debug.WriteLine("Target note not found.");
                // If the target is invalid, add sourceNote to the end of the list
                currentTab.Notes.Remove(sourceNote);
                currentTab.Notes.Add(sourceNote);
            }
            else
            {
                // Remove the source note and reinsert it at the target's index
                currentTab.Notes.Remove(sourceNote);
                var targetIndex = currentTab.Notes.IndexOf(targetNote);

                if (targetIndex < 0 || targetIndex > currentTab.Notes.Count)
                {
                    Debug.WriteLine("Invalid target index.");
                    currentTab.Notes.Add(sourceNote); // Add to the end if the index is invalid
                }
                else
                {
                    currentTab.Notes.Insert(targetIndex, sourceNote);
                }
            }

            Debug.WriteLine($"Updated order: {string.Join(", ", currentTab.Notes.Select(n => n.Title))}");

            // Refresh the UI to reflect the new order
            RefreshNotes();
            SaveTabs();
        }
        private void RefreshNotes()
        {
            NotesPanel.Children.Clear();
            if (currentTab == null)
            {
                return;
            }

            foreach (var note in currentTab.Notes)
            {
                NotesPanel.Children.Add(CreateCard(note.Title, note.Description, () => ShowNoteDetails(note)));
            }

            NotesPanel.Children.Add(CreateCard("+ Add Note", "", () => AddNote_Click(this, new RoutedEventArgs()), isAddCard: true));
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
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

}
