using MahApps.Metro.Controls;
using SCRMTracker.Items;
using System.Windows;

namespace SCRMTracker
{
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            mainThemeComboBox.ItemsSource = ThemeColor.GetMainColors();
            subThemeComboBox.ItemsSource = ThemeColor.GetSubColors();

            mainThemeComboBox.SelectedIndex = Settings.GetInstance().MainThemeColor;
            subThemeComboBox.SelectedIndex = Settings.GetInstance().SubThemeColor;
            mainThemeComboBox.SelectionChanged += ThemeSelectionChanged;
            subThemeComboBox.SelectionChanged += ThemeSelectionChanged;

            rememberAdapterToggleSwitch.Toggled += rememberAdapterToggleSwitch_ToggledChanged;
            autoCaptureToggleSwitch.Toggled += autoCaptureToggleSwitch_ToggledChanged;
            blacklistSoundToggleSwitch.Toggled += blacklistSoundToggleSwitch_Toggled;
            rememberAdapterToggleSwitch.IsOn = Settings.GetInstance().RememberAdapter;
            autoCaptureToggleSwitch.IsOn = Settings.GetInstance().AutoCapture;
            blacklistSoundToggleSwitch.IsOn = Settings.GetInstance().BlackListWAV;
        }

        private void ThemeSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (mainThemeComboBox.SelectedItem == null || subThemeComboBox.SelectedItem == null)
                return;

            Settings instance = Settings.GetInstance();
            instance.ChagneTheme(mainThemeComboBox.SelectedIndex, subThemeComboBox.SelectedIndex);
            instance.UpdateSettingsJson();
        }

        private void rememberAdapterToggleSwitch_ToggledChanged(object sender, RoutedEventArgs e)
        {
            if (rememberAdapterToggleSwitch.IsOn == true)
            {
                autoCaptureToggleSwitch.IsEnabled = true;
            }
            else
            {
                autoCaptureToggleSwitch.IsEnabled = false;
                autoCaptureToggleSwitch.IsOn = false;
            }

            Settings instance = Settings.GetInstance();
            instance.RememberAdapter = rememberAdapterToggleSwitch.IsOn;
            instance.UpdateSettingsJson();
        }

        private void autoCaptureToggleSwitch_ToggledChanged(object sender, RoutedEventArgs e)
        {
            Settings instance = Settings.GetInstance();
            instance.AutoCapture = autoCaptureToggleSwitch.IsOn;
            instance.UpdateSettingsJson();
        }

        private void blacklistSoundToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings instance = Settings.GetInstance();
            instance.BlackListWAV = blacklistSoundToggleSwitch.IsOn;
            instance.UpdateSettingsJson();
        }
    }
}
