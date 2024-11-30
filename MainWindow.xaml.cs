using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace InStelle
{
    public sealed partial class MainWindow : Window
    {
        private bool _ctrlTHotkeyCooldown = false;
        private bool _ctrlNHotkeyCooldown = false;

        public MainWindow()
        {
            this.InitializeComponent();
            MainFrame.Navigate(typeof(MainPage));
            this.Activated += OnWindowActivated;
        }

        private void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != Microsoft.UI.Xaml.WindowActivationState.Deactivated)
            {
                if (this.Content is UIElement rootElement)
                {
                    rootElement.KeyDown += OnKeyDown;
                }
            }
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var currentFrame = this.Content as Frame;

            if (currentFrame == null) return;

            if (IsCtrlPressed())
            {
                HandleCtrlHotkeys(e, currentFrame);
            }
            else
            {
                HandleGlobalHotkeys(e, currentFrame);
            }
        }

        private void HandleCtrlHotkeys(KeyRoutedEventArgs e, Frame frame)
        {
            // Define a dictionary of Ctrl+Key actions for specific pages
            var ctrlActions = new Dictionary<Type, Action>
            {
                { typeof(MainPage), () => HandleMainPageCtrlHotkeys(e, (MainPage)frame.Content) },
                { typeof(NotePage), () => HandleNotePageCtrlHotkeys(e, (NotePage)frame.Content) }
            };

            // Execute the action if the current page type matches
            if (ctrlActions.TryGetValue(frame.Content.GetType(), out var action))
            {
                action();
                e.Handled = true; // Mark event as handled
            }
        }

        private void HandleMainPageCtrlHotkeys(KeyRoutedEventArgs e, MainPage mainPage)
        {
            switch (e.Key)
            {
                case VirtualKey.N:
                    if (!_ctrlNHotkeyCooldown)
                    {
                        _ctrlNHotkeyCooldown = true;
                        mainPage.AddTab_Click(this, new RoutedEventArgs());
                        ResetCooldownAsync(() => _ctrlNHotkeyCooldown = false, 300); // 300ms cooldown
                    }
                    break;

                case VirtualKey.T:
                    if (!_ctrlTHotkeyCooldown)
                    {
                        _ctrlTHotkeyCooldown = true;
                        mainPage.AddNote_Click(this, new RoutedEventArgs());
                        ResetCooldownAsync(() => _ctrlTHotkeyCooldown = false, 300); // 300ms cooldown
                    }
                    break;
            }
        }

        private void HandleNotePageCtrlHotkeys(KeyRoutedEventArgs e, NotePage notePage)
        {
            switch (e.Key)
            {
                case VirtualKey.S:
                    notePage.SaveNote_Click(this, new RoutedEventArgs());
                    break;
                case VirtualKey.D:
                    notePage.DeleteNote_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void HandleGlobalHotkeys(KeyRoutedEventArgs e, Frame frame)
        {
            if (e.Key == VirtualKey.Escape && frame.Content is NotePage notePage)
            {
                notePage.CancelNote_Click(this, new RoutedEventArgs());
                e.Handled = true; // Mark event as handled
            }
        }

        private bool IsCtrlPressed()
        {
            // Check if the Ctrl key is currently pressed
            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        private async void ResetCooldownAsync(Action resetAction, int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds);
            resetAction();
        }
    }
}
