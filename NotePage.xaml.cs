using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InStelle
{
    public sealed partial class NotePage : Page
    {
        private Note _note = null!;
        private TabData _currentTab = null!;
        private Action _refreshNotes = null!;
        private Action _saveTabs = null!;

        public NotePage()
        {
            this.InitializeComponent();
            this.Loaded += NotePage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Unpack the parameters passed during navigation
            var parameters = (Tuple<Note, TabData, Action, Action>)e.Parameter;
            _note = parameters.Item1;
            _currentTab = parameters.Item2;
            _refreshNotes = parameters.Item3;
            _saveTabs = parameters.Item4;

            // Initialize UI with the note's data
            TitleTextBox.Text = _note.Title;
            DescriptionTextBox.Text = _note.Description;
        }

        private void NotePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the DescriptionTextBox programmatically
            DescriptionTextBox.Focus(FocusState.Programmatic);
        }

        public void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            // Update the note's properties
            _note.Title = TitleTextBox.Text;
            _note.Description = DescriptionTextBox.Text;

            // Add the note to the current tab if it's new
            if (!_currentTab.Notes.Contains(_note))
            {
                _currentTab.Notes.Add(_note);
            }

            // Save and refresh
            _refreshNotes();
            _saveTabs();

            // Navigate back
            Frame.GoBack();
        }

        public void CancelNote_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back without saving
            Frame.GoBack();
        }

        public async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            // Confirm deletion
            var dialog = new ContentDialog
            {
                Title = "Delete Note",
                Content = "Are you sure you want to delete this note?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Delete",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Show the dialog and wait for user response
            var result = await dialog.ShowAsync();

            // Check if the user clicked the "Delete" button
            if (result == ContentDialogResult.Primary)
            {
                // Remove the note from the current tab
                if (_currentTab.Notes.Contains(_note))
                {
                    _currentTab.Notes.Remove(_note);
                }

                // Save and refresh
                _refreshNotes();
                _saveTabs();

                // Navigate back
                Frame.GoBack();
            }
        }
    }
}
