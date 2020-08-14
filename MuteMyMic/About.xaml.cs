using System;
using System.Windows;
using System.Windows.Input;

namespace MuteMyMic
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            MouseDown += (_, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
        }

        public void OnDoneButtonClicked(object _, EventArgs e)
        {
            Close();
        }
    }
}
