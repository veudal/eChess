using Dsafa.WpfColorPicker;
using eChess.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace eChess
{
    public partial class SettingsPage : Page
    {
        MainWindow window = null;

        public SettingsPage(MainWindow window_)
        {
            InitializeComponent();
            window = window_;
        }

        private void WhiteColorBtn_Click(object sender, RoutedEventArgs e)
        {
            var initialColor = (Color)ColorConverter.ConvertFromString(Settings.Default.WhiteColor);
            var dialog = new ColorPickerDialog(initialColor);
            dialog.MinWidth = 700;
            dialog.MinHeight = 500;
            dialog.Background = Brushes.SlateGray;
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Settings.Default.WhiteColor = dialog.Color.ToString();
                SettingsIO.Save();
            }
        }
        private void BlackColorBtn_Click(object sender, RoutedEventArgs e)
        {
            var initialColor = (Color)ColorConverter.ConvertFromString(Settings.Default.BlackColor);
            var dialog = new ColorPickerDialog(initialColor);
            dialog.Background = Brushes.SlateGray;
            dialog.MinWidth = 700;
            dialog.MinHeight = 500;
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Settings.Default.BlackColor = dialog.Color.ToString();
                SettingsIO.Save();
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.Sounds = true;
            SettingsIO.Save();
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.Sounds = false;
            SettingsIO.Save();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            window.SettingsFrame.Visibility = Visibility.Collapsed;
            window.Menu.Visibility = Visibility.Visible;
            window.SettingsFrame.Visibility = Visibility.Collapsed;
        }

        private void ResetWhiteBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.WhiteColor = "MintCream";
            SettingsIO.Save();
        }

        private void ResetBlackBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BlackColor = "SteelBlue";
            SettingsIO.Save();
        }
    }
}
