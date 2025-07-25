using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        SizeInt32 _size = new();
        public static new MainWindow? Current { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            MainFrame.Navigate(typeof(MainPage));

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // Get display information for the primary monitor
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            _size = new((int)(workArea.Height * 1.42), (int)(workArea.Height * 0.8));
            appWindow.Resize(_size);
            Current = this;
        }
        
        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            MainPage? mainPage = MainFrame.Content as MainPage;
            mainPage?.OnClose(sender,e);
        }
    }
}
