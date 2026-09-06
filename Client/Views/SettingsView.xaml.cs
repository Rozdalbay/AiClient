using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDesktopClient.ViewModels;

namespace AiDesktopClient.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string section)
        {
            GeneralPanel.Visibility = Visibility.Collapsed;
            BackendPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            switch (section)
            {
                case "General":
                case "Appearance":
                case "Models":
                case "Notifications":
                    GeneralPanel.Visibility = Visibility.Visible;
                    break;
                case "Backend":
                    BackendPanel.Visibility = Visibility.Visible;
                    break;
                case "About":
                    AboutPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
