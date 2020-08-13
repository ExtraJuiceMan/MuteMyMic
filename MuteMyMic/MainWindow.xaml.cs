using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MuteMyMic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
        private readonly SoundPlayer soundPlayer = new SoundPlayer();
        private readonly List<int> keysPressedBuffer = new List<int>();
        private readonly IniFile config = new IniFile("./config.ini");
        private readonly Dispatcher dispatch = DispatcherBuilder.Build();

        private bool unmuted = false;
        private bool currentAudioDeviceWasMuted = false;
        private int[] keyCombo = new int[] { 0x12 };
        private MMDevice currentAudioDevice;

        public MainWindow()
        {
            InitializeComponent();

            MouseDown += (_, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
            TrayIcon.TrayMouseDoubleClick += OnTrayDoubleClick;

            foreach (var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
               InputSelect.Items.Add(device);

            InputSelect.SelectionChanged += OnInputSelectSelectionChanged;

            if (config.KeyExists("SoundOnUnmute"))
                PlaySoundsButton.IsChecked = Convert.ToBoolean(config.Read("SoundOnUnmute"));

            if (config.KeyExists("KeyCombo"))
                keyCombo = config.Read("KeyCombo").Split(',').Select(x => int.Parse(x)).ToArray();

            var id = config.Read("DeviceId");

            foreach (var item in InputSelect.Items.Cast<MMDevice>())
            {
                if (item.ID == id)
                {
                    currentAudioDeviceWasMuted = item.AudioEndpointVolume.Mute;
                    InputSelect.SelectedItem = item;
                    break;
                }
            }

            RunOnStartupButton.IsChecked = WillRunOnStartup();

            // Keep our mouse smooth by not running in UI thread
            dispatch.BeginInvoke(() =>
            {
                KeyboardHook.Hook();
            });

            KeyboardHook.OnKeyDownEvent += OnKeyDown;
            KeyboardHook.OnKeyUpEvent += OnKeyUp;

        }

        private void PlaySound(string location)
        {
            if (!PlaySoundsButton.IsChecked)
                return;

            soundPlayer.SoundLocation = location;
            soundPlayer.Play();
        }

        private void OnKeyDown(int key)
        {
            if (unmuted ||
                currentAudioDevice == null ||
                (keysPressedBuffer.Count == 0 && keyCombo.Length > 0 && key != keyCombo[0]))
                return;

            if (!keysPressedBuffer.Contains(key))
                keysPressedBuffer.Add(key);

            if (!unmuted && Enumerable.SequenceEqual(keysPressedBuffer, keyCombo))
            {
                unmuted = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MutedLabel.Content = "Unmuted";
                    currentAudioDevice.AudioEndpointVolume.Mute = false;
                    PlaySound("./unmute.wav");
                });

            }
        }

        private void OnKeyUp(int key)
        {
            if (currentAudioDevice == null)
                return;

            keysPressedBuffer.RemoveAll(x => x == key);

            if (unmuted && !Enumerable.SequenceEqual(keysPressedBuffer, keyCombo))
            {
                unmuted = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MutedLabel.Content = "Muted";
                    currentAudioDevice.AudioEndpointVolume.Mute = true;
                    PlaySound("./mute.wav");
                });
            }
        }

        private void OnAboutButtonClick(object _, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void OnSetHotkeyButtonClicked(object _, EventArgs e)
        {
            var dialog = new HotkeySelector(keyCombo, hotkeys =>
            {
                keyCombo = hotkeys;
                config.Write("KeyCombo", String.Join(',', keyCombo));
            });
            keyCombo = new int[0];
            dialog.ShowDialog();
        }

        private void OnPlaySoundsButtonClicked(object _, EventArgs e)
        {
            PlaySoundsButton.IsChecked = !PlaySoundsButton.IsChecked;
            config.Write("SoundOnUnmute", PlaySoundsButton.IsChecked.ToString());
        }

        private void OnRunOnStartupButtonClicked(object _, EventArgs e)
        {
            RunOnStartupButton.IsChecked = !RunOnStartupButton.IsChecked;
            SetStartupRegistryKey(RunOnStartupButton.IsChecked);
        }

        private void OnHideButtonClick(object _, EventArgs e)
        {
            Hide();
            TrayIcon.Visibility = Visibility.Visible;
        }

        private void SetStartupRegistryKey(bool willRunOnStartup)
        {
            var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (willRunOnStartup)
                key.SetValue("MuteMyMic", System.Reflection.Assembly.GetExecutingAssembly().Location);
            else
                key.DeleteValue("MuteMyMic", false);
        }

        private bool WillRunOnStartup()
        {
            var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);

            return key.GetValue("MuteMyMic") != null;
        }

        private void OnTrayDoubleClick(object _, RoutedEventArgs e)
        {
            Show();
            TrayIcon.Visibility = Visibility.Hidden;
        }

        private void OnExitButtonClick(object _, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnInputSelectSelectionChanged(object _, SelectionChangedEventArgs e) 
        { 
            if (currentAudioDevice != null)
                currentAudioDevice.AudioEndpointVolume.Mute = currentAudioDeviceWasMuted;

            currentAudioDevice = (MMDevice)InputSelect.SelectedItem;
            currentAudioDeviceWasMuted = currentAudioDevice.AudioEndpointVolume.Mute;
            currentAudioDevice.AudioEndpointVolume.Mute = true;
            MutedLabel.Content = "Muted";

            config.Write("DeviceId", currentAudioDevice.ID);
        }

        private void OnExit(object _, EventArgs e)
        {
            TrayIcon.Dispose();
            dispatch.InvokeShutdown();

            if (currentAudioDevice != null)
                currentAudioDevice.AudioEndpointVolume.Mute = currentAudioDeviceWasMuted;

            KeyboardHook.Unhook();
        }
    }
}
