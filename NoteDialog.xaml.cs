using Microsoft.UI.Xaml.Controls;

namespace InStelle
{
    public sealed partial class NoteDialog : ContentDialog
    {
        public string? NoteTitle => TitleTextBox.Text;
        public string? NoteDescription => DescriptionTextBox.Text;

        public NoteDialog(string? title = null, string? description = null)
        {
            this.InitializeComponent();
            TitleTextBox.Text = title ?? string.Empty;
            DescriptionTextBox.Text = description ?? string.Empty;
        }
    }
}
