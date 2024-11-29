using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
                NotesPanel.Children.Add(CreateNoteButton(note));
            }

            var addNoteButton = new Button
            {
                Content = "+",
                Width = 100,
                Height = 50,
            };
            addNoteButton.Click += AddNote_Click;
            NotesPanel.Children.Add(addNoteButton);
        }

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == null) return;

            var newNote = $"Note {currentTab.Notes.Count + 1}";
            currentTab.Notes.Add(newNote);

            NotesPanel.Children.Insert(NotesPanel.Children.Count - 1, CreateNoteButton(newNote));
            SaveTabs();
        }

        private Button CreateNoteButton(string note)
        {
            var button = new Button
            {
                Content = note,
                Width = 200,
                Height = 50,
                Margin = new Thickness(5)
            };
            button.Click += (s, e) => OpenNoteDialog(note);
            return button;
        }

        private async void OpenNoteDialog(string note)
        {
            var dialog = new ContentDialog
            {
                Title = "Note",
                Content = note,
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
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
        public List<string> Notes { get; set; }

        public TabData(string icon)
        {
            Icon = icon ?? throw new ArgumentNullException(nameof(icon));
            Notes = new List<string>();
        }
    }
}