using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Microsoft.UI.Composition.Compositor _compositor;
        int Selected = 0;
        System.Timers.Timer _timer = new();
        Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
        int _SelectMargin = -2;
        Dictionary<string, int> _RunCount = new();
        TimeSpan _TimeSetting;
        TimeSpan _RestTime = TimeSpan.FromSeconds(0);
        bool _TimerRunning = false;
        bool _IsFocusEnable = true;
        public MainPage()
        {
            InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            _compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _timer.Interval = 1000;
            _timer.Elapsed += timer_Elapsed;
            _timer.Start();
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var targetFolder = await appInstalledFolder.GetFolderAsync(@"Assets\Games");
            var subfolders = await targetFolder.GetFoldersAsync();
            //ImageStack.Height = _size.Height / 4 * _magnification;
            this.Focus(FocusState.Programmatic);
            string documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (File.Exists(@$"{documentFolder}\RunCount.json"))
            {
                var json = File.ReadAllText(@$"{documentFolder}\RunCount.json");
                _RunCount = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,int>>(json) ?? new Dictionary<string,int>();
            }
            

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("TimeSetting"))
            {
                Debug.WriteLine("Time setting found in local settings.");
                _TimeSetting = TimeSpan.Parse(localSettings.Values["TimeSetting"].ToString() ?? "00:00:00");
                Time.Text = _TimeSetting.ToString(@"mm':'ss");
            }

            ImageStack.Children.Clear();
            for (int i = 0; i < subfolders.Count; i++)
            {
                StorageFile file;
                if (await subfolders[i].TryGetItemAsync("GameIcon.png") is StorageFile icon) file = icon;
                else file = await targetFolder.GetFileAsync("DefaultIcon.png");
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);

                var image = new Image
                {
                    Source = bitmap,
                    Height = 210,
                    Margin = new Thickness(5),
                    Tag = subfolders[i]
                };
                if (_RunCount is Dictionary<string, int> rc && !rc.ContainsKey(subfolders[i].Name))
                    rc.Add(subfolders[i].Name, 0);

                image.Tapped += Image_Tapped;
                image.PointerEntered += (s, e) =>
                {
                    SelectGame(ImageStack.Children.IndexOf(s as UIElement), Selected, true);
                    Selected = ImageStack.Children.IndexOf(s as UIElement);
                };
                ImageStack.Children.Add(image);
            }
            var anim = _compositor.CreateExpressionAnimation();
            anim.Expression = "(left.Scale.X - 1) * left.ActualSize.X + left.Translation.X";
            anim.Target = "Translation.X";
            var elements = ImageStack.Children;
            for (int j = 0; j < elements.Count - 1; j++)
            {
                anim.SetExpressionReferenceParameter("left", elements[j]);
                elements[j + 1].StartAnimation(anim);
            }
            SelectGame(Selected, Selected, false);
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Image? image = sender as Image;
            if (image != null)
            {
                RunGame((StorageFolder)image.Tag);
            }
        }
        List<VirtualKey> _Pressed = new();
        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (!_Pressed.Contains(e.Key)) _Pressed.Add(e.Key);
            switch (e.Key)
            {
                case VirtualKey.Left:
                    if (Selected > 0)
                    {
                        Selected--;
                        SelectGame(Selected, Selected + 1, false);
                    }
                    break;
                case VirtualKey.Right:
                    if (Selected < ImageStack.Children.Count - 1)
                    {
                        Selected++;
                        SelectGame(Selected, Selected - 1, false);
                    }
                    break;
                case VirtualKey.Enter:
                    RunGame(((Image)ImageStack.Children[Selected]).Tag as StorageFolder);
                    break;
            }
            if (_Pressed.Contains(VirtualKey.Control) && _Pressed.Contains(VirtualKey.Shift) && !_TimerRunning)
            {
                if (_Pressed.Contains(VirtualKey.Up))
                {
                    _TimeSetting += TimeSpan.FromSeconds(30);
                    Time.Text = _TimeSetting.ToString(@"mm':'ss");
                }
                if (_Pressed.Contains(VirtualKey.Down))
                {
                    _TimeSetting -= TimeSpan.FromSeconds(30);
                    if (_TimeSetting < TimeSpan.Zero) _TimeSetting = TimeSpan.Zero;
                    Time.Text = _TimeSetting.ToString(@"mm':'ss");
                }
                if (_Pressed.Contains(VirtualKey.S))
                {
                    _RestTime = _TimeSetting;
                    _TimerRunning = true;
                }
            }
        }
        private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (_Pressed.Contains(e.Key)) _Pressed.Remove(e.Key);
            //if (!(_Pressed.Contains(VirtualKey.Control) && _Pressed.Contains(VirtualKey.Shift)))
            //Settings.IsEnabled = false;
        }
        private void timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_TimerRunning)
            {
                if (_RestTime > TimeSpan.Zero)
                {
                    _RestTime -= TimeSpan.FromSeconds(1);
                    _TimerRunning = true;
                }
                else
                {
                    _RestTime = TimeSpan.Zero;
                    _TimerRunning = false;
                    _dispatcherQueue.TryEnqueue(ShowFinishedDialog);
                }
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Clock.Text = DateTime.Now.ToString("HH:mm");
                if (_TimerRunning)
                    Time.Text = _RestTime.ToString(@"mm':'ss");
            });
        }

        private void ShowFinishedDialog()
        {
            var finishedwindow = new OverlayDialog();
            finishedwindow.SetParent(this);
            finishedwindow.Activate(); // ウィンドウを表示してアクティブにする
            _IsFocusEnable = false;
        }
        public void Reset()
        {
            _RestTime = _TimeSetting;
            Time.Text = _TimeSetting.ToString(@"mm':'ss");
            SelectGame(0, Selected, false);
            _IsFocusEnable = true;
            Selected = 0;
        }
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta;
            if (delta > 0 && Selected > 0)
            {
                Selected--;
                SelectGame(Selected, Selected + 1, false);
            }
            else if (delta < 0 && Selected < ImageStack.Children.Count - 1)
            {
                Selected++;
                SelectGame(Selected, Selected - 1, false);
            }
        }
        private void SelectGame(int index, int before, bool pointer)
        {
            if ((ImageStack.Children[index] is Image img && img.Tag is StorageFolder tag))
            {
                Title.Text = tag.Name;
                RunCountTB.Text = _RunCount[tag.Name].ToString();
                if (File.Exists(tag.Path + @"\Description.txt"))
                {
                    var descriptionFile = tag.GetFileAsync("Description.txt").AsTask().Result;
                    if (descriptionFile != null)
                    {
                        using (var stream = descriptionFile.OpenStreamForReadAsync().Result)
                        using (var reader = new StreamReader(stream))
                        {
                            Subtitle.Text = reader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    Subtitle.Text = "";
                }
            }
            ApplySpringAnimation(ImageStack.Children[index], 1.2f);

            if (!pointer) Scroller.ChangeView((Selected + _SelectMargin) * ImageStack.Children[index].ActualSize.X + 10, null, null);
            if (before != index) ApplySpringAnimation(ImageStack.Children[before], 1.0f);
        }
        private async void RunGame(StorageFolder folder)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.FileName = (await folder.GetFileAsync("RunGame.exe")).Path;
            psi.WorkingDirectory = folder.Path;
            Process.Start(psi);
            _RunCount[folder.Name]++;
            Debug.WriteLine(_RunCount[folder.Name]);
        }
        public void KillGame()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = @$"/F /IM RunGame.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(psi);
        }
        private void ApplySpringAnimation(UIElement element, float scale)
        {
            var animation = _compositor.CreateSpringVector3Animation();
            animation.Target = "Scale";
            animation.FinalValue = new Vector3(scale);
            animation.DampingRatio = 0.9f;
            animation.Period = TimeSpan.FromMilliseconds(50);

            element.CenterPoint = new Vector3(0f, element.ActualSize.Y, 0f);
            element.StartAnimation(animation);
        }
        public void OnClose(object sender, WindowEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["TimeSetting"] = _TimeSetting;
            string json = System.Text.Json.JsonSerializer.Serialize(_RunCount);
            string documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText($@"{documentFolder}\RunCount.json", json);
            KillGame();
            //this.Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (_Pressed.Contains(VirtualKey.Control) && _Pressed.Contains(VirtualKey.Shift))
            {
                _Pressed.Clear();
                Frame.Navigate(typeof(SettingsPage));
            }
        }
        private void OnLoseFocus(object sender, RoutedEventArgs e)
        {
            if (_IsFocusEnable)
                this.Focus(FocusState.Programmatic);

        }
    }
}
