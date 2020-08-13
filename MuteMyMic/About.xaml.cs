using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
