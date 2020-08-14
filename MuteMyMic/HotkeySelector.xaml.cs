using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace MuteMyMic
{
    /// <summary>
    /// Interaction logic for HotkeySelector.xaml
    /// </summary>
    public partial class HotkeySelector : Window
    {
        private readonly Action<int[]> setHotkey;
        private readonly List<int> newHotkeys = new List<int>();

        private int[] currentHotkeys;
        private bool recording = false;

        public HotkeySelector(int[] currentHotkeys, Action<int[]> setHotkey)
        {
            InitializeComponent();

            this.currentHotkeys = currentHotkeys;
            this.setHotkey = setHotkey;

            MouseDown += (_, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
            MouseDown += OnMouseDown;
            HotkeyBox.MouseDown += OnMouseDown;

            HotkeyBox.Text = HotkeyUtility.FormatHotkeys(currentHotkeys);
        }

        private void OnDoneButtonClicked(object _, RoutedEventArgs e)
        {
            setHotkey(currentHotkeys);
            Close();
        }

        private void OnHotkeyBoxDoubleClicked(object _, RoutedEventArgs e)
        {
            if (!recording)
            {
                recording = true;
                DoneButton.IsEnabled = false;
                HotkeyBox.Text = String.Empty;
                HotkeyBox.Focus();
            }
        }

        private void OnHotkeyBoxPreviewKeyDown(object _, KeyEventArgs e)
        {
            e.Handled = true;

            if (recording)
            {
                if (e.Key == Key.Escape)
                {
                    recording = false;
                    DoneButton.IsEnabled = true;
                    currentHotkeys = newHotkeys.ToArray();
                    setHotkey(currentHotkeys);

                    newHotkeys.Clear();
                    return;
                }

                var key = KeyInterop.VirtualKeyFromKey(e.Key);

                if (!newHotkeys.Contains(key))
                    newHotkeys.Add(key);

                HotkeyBox.Text = HotkeyUtility.FormatHotkeys(newHotkeys.ToArray());
            }
        }

        private void OnMouseDown(object _, MouseEventArgs e)
        {
            if (recording)
            {
                if (e.XButton1 == MouseButtonState.Pressed && !newHotkeys.Contains(0x05))
                    newHotkeys.Add(0x05);

                if (e.XButton2 == MouseButtonState.Pressed && !newHotkeys.Contains(0x06))
                    newHotkeys.Add(0x06);

                HotkeyBox.Text = HotkeyUtility.FormatHotkeys(newHotkeys.ToArray());
            }
        }
    }
}
