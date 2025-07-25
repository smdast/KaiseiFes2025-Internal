using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.System;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OverlayDialog : Window
    {
        MainPage? _parent;
        public OverlayDialog()
        {
            this.InitializeComponent();
        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
    int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_SHOWWINDOW = 0x0040;

        List<VirtualKey> Pressed = new List<VirtualKey>();
        public void SetParent(MainPage parent)
        {
            _parent = parent;
        }
        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this); // this: ���݂�Window�C���X�^���X
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
        SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            var dialog = new ContentDialog
            {
                Title = "�������Ԃ��I�����܂����B",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Name = "SubText",
                            TextWrapping = TextWrapping.Wrap,
                            Text = "�����ꂠ�肪�Ƃ��������܂����B���y���݂��������܂������H���ЎQ���c�̃R���e�X�g�ւ̓��[�����肢���܂��B",
                            Margin = new Thickness(0, 0, 0, 20)
                        },
                        new CheckBox
                        {
                            Content = "�N�����̃Q�[�����I������",
                            IsChecked = true,
                            IsTabStop = false
                        }
                    }
                },
                PrimaryButtonText = "����",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (Pressed.Contains(VirtualKey.Control) && Pressed.Contains(VirtualKey.Shift))
                {
                    if (dialog.Content is StackPanel stackPanel)
                    {
                        var checkBox = stackPanel.Children.OfType<CheckBox>().FirstOrDefault();
                        if (checkBox != null && checkBox.IsChecked == true)
                        {
                            _parent?.KillGame();
                        }
                    }
                    _parent?.Reset();
                    this.Close();
                }
                else
                {
                    args.Cancel = true;
                }
            };
            dialog.Closing += (s, args) =>
            {
                if (args.Result == ContentDialogResult.None)
                {
                    // Esc �� Back �L�[�A�O���^�b�v�ŕ��悤�Ƃ���
                    args.Cancel = true; // �� ���Ȃ�
                }
            };
            var result = await dialog.ShowAsync();

#pragma warning disable CS8602 // null �Q�Ƃ̉\����������̂̋t�Q�Ƃł��B
            (dialog.Content as StackPanel).Children
                .OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "SubText").Width = dialog.ActualWidth;
#pragma warning restore CS8602 // null �Q�Ƃ̉\����������̂̋t�Q�Ƃł��B

        }
        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (!Pressed.Contains(e.Key))  Pressed.Add(e.Key);
        }
        private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (Pressed.Contains(e.Key)) Pressed.Remove(e.Key);
        }
    }
}
