using System;
using System.Windows;
using System.Windows.Input;

namespace MuteMyMic
{
    /// <summary>
    /// Interaction logic for NumberSelector.xaml
    /// </summary>
    public partial class NumberSelector : Window
    {
        private readonly Action<int> setNumber;

        public NumberSelector(string prompt, int currentNumber, Action<int> setNumber)
        {
            InitializeComponent();

            this.setNumber = setNumber;
            Prompt.Content = prompt;
            NumberBox.Text = currentNumber.ToString();

            MouseDown += (_, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
        }

        private void OnDoneButtonClicked(object _, RoutedEventArgs e)
        {
            setNumber(int.Parse(NumberBox.Text));
            Close();
        }

        private void OnNumberBoxPreviewTextInput(object _, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out var _);
    }
}
