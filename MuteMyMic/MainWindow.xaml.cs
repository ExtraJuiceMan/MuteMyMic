using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private bool onDelayPeriod = false;
        private int[] keyCombo = new int[] { 0x12 };
        private CancellationTokenSource unmuteDelayCancellation = new CancellationTokenSource();
        private int unmuteMsDelay = 0;
        private AudioDevice currentDevice;

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

            if (config.KeyExists("UnmuteDelay"))
                unmuteMsDelay = int.Parse(config.Read("UnmuteDelay"));

            var id = config.Read("DeviceId");

            InputSelect.SelectedItem = InputSelect.Items.Cast<MMDevice>().FirstOrDefault(x => x.ID == id);

            RunOnStartupButton.IsChecked = StartupManager.WillRunOnStartup;

            SetUnmuteDelayButton.Header = $"Set Unmute Delay: {unmuteMsDelay}ms";
            SetHotkeyButton.Header = $"Set Hotkey: {HotkeyUtility.FormatHotkeys(keyCombo)}";


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
            lock (soundPlayer)
            {
                if (!PlaySoundsButton.IsChecked)
                    return;

                soundPlayer.SoundLocation = location;
                soundPlayer.Play();
            }
        }

        private void OnKeyDown(int key)
        {
            if (unmuted ||
                currentDevice == null ||
                (keysPressedBuffer.Count == 0 && keyCombo.Length > 0 && key != keyCombo[0]))
                return;

            if (!keysPressedBuffer.Contains(key))
                keysPressedBuffer.Add(key);

            if (!unmuted && Enumerable.SequenceEqual(keysPressedBuffer, keyCombo) && !onDelayPeriod)
            {
                unmuted = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    currentDevice.Unmute();
                    PlaySound("./unmute.wav");
                });
            }
        }

        private void OnKeyUp(int key)
        {
            if (currentDevice == null)
                return;

            keysPressedBuffer.Remove(key);

            if (unmuted && !Enumerable.SequenceEqual(keysPressedBuffer, keyCombo))
            {
                unmuted = false;
                if (!onDelayPeriod)
                {
                    onDelayPeriod = true;
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await Task.Delay(unmuteMsDelay, unmuteDelayCancellation.Token);
                        }
                        catch (TaskCanceledException) { }

                        onDelayPeriod = false;
                        currentDevice.Mute();
                        PlaySound("./mute.wav");
                    });
                }
            }
        }

        private void OnAboutButtonClick(object _, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void OnSetUnmuteDelayButtonClicked(object _, EventArgs e)
        {
            new NumberSelector("Input an unmute delay in milliseconds.", unmuteMsDelay, delay =>
            {
                unmuteDelayCancellation.Cancel();
                unmuteDelayCancellation = new CancellationTokenSource();

                unmuteMsDelay = delay;
                config.Write("UnmuteDelay", unmuteMsDelay.ToString());
                SetUnmuteDelayButton.Header = $"Set Unmute Delay: {unmuteMsDelay}ms";
            }).ShowDialog();
        }

        private void OnSetHotkeyButtonClicked(object _, EventArgs e)
        {
            var dialog = new HotkeySelector(keyCombo, hotkeys =>
            {
                keyCombo = hotkeys;
                config.Write("KeyCombo", String.Join(',', keyCombo));
                SetHotkeyButton.Header = $"Set Hotkey: {HotkeyUtility.FormatHotkeys(keyCombo)}";
            });

            // reset keys so we don't trigger unmute during dialog
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
            StartupManager.WillRunOnStartup = RunOnStartupButton.IsChecked;
        }

        private void SetWindowVisible(bool visible)
        {
            if (visible)
            {
                Show();
                TrayIcon.Visibility = Visibility.Hidden;
            }
            else
            {
                Hide();
                TrayIcon.Visibility = Visibility.Visible;
            }
        }

        private void OnHideButtonClick(object _, EventArgs e) => SetWindowVisible(false);
        private void OnTrayDoubleClick(object _, RoutedEventArgs e) => SetWindowVisible(true);
        private void OnExitButtonClick(object _, EventArgs e) => Application.Current.Shutdown();

        private void OnInputSelectSelectionChanged(object _, SelectionChangedEventArgs e) 
        {
            currentDevice?.RestoreToPreviousState();

            currentDevice = new AudioDevice((MMDevice)InputSelect.SelectedItem, MutedLabel);
            currentDevice.Mute();

            config.Write("DeviceId", currentDevice.DeviceId);
        }

        private void OnExit(object _, EventArgs e)
        {
            TrayIcon.Dispose();
            dispatch.InvokeShutdown();

            currentDevice?.RestoreToPreviousState();

            KeyboardHook.Unhook();
        }
    }
}
