using Microsoft.UI.Xaml;
using System;

namespace InStelle
{
    public sealed partial class NoteWindow : Window
    {
        private Note _note;
        private TabData _currentTab;
        private Action _refreshNotes;
        private Action _saveTabs;

        // Property to indicate if the note was saved
        public bool NoteSaved { get; private set; } = false;

        public NoteWindow(Note note, TabData currentTab, Action refreshNotes, Action saveTabs)
        {
            this.InitializeComponent();

            _note = note;
            _currentTab = currentTab;
            _refreshNotes = refreshNotes;
            _saveTabs = saveTabs;

            // Initialize the UI with the note's data
            TitleTextBox.Text = note.Title;
            DescriptionTextBox.Text = note.Description;
        }

        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            // Update the note's data
            _note.Title = TitleTextBox.Text;
            _note.Description = DescriptionTextBox.Text;

            // Mark the note as saved
            NoteSaved = true;

            // Refresh notes and save changes
            _refreshNotes();
            _saveTabs();

            this.Close();
        }

        private void CancelNote_Click(object sender, RoutedEventArgs e)
        {
            // Close without saving
            NoteSaved = false;
            this.Close();
        }

        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            // Remove the note from the current tab
            _currentTab.Notes.Remove(_note);

            // Refresh notes and save changes
            _refreshNotes();
            _saveTabs();

            // Close the window
            this.Close();
        }
    }
}
