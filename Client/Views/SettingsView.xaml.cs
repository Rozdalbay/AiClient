using System.Windows;
using System.Windows.Controls;

namespace AiDesktopClient.Views;

// код-behind настроек: радио-кнопки слева переключают видимость панелей справа, одна хрень без логики
public partial class SettingsView : UserControl
{
    private StackPanel[] _panels;

    public SettingsView()
    {
        InitializeComponent();
        _panels = [GeneralPanel, AppearancePanel, ModelsPanel, BackendPanel, NotificationsPanel, AccountPanel, AboutPanel];
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string section)
        {
            foreach (var panel in _panels)
                panel.Visibility = Visibility.Collapsed;

            switch (section)
            {
                case "General":
                    GeneralPanel.Visibility = Visibility.Visible;
                    break;
                case "Appearance":
                    AppearancePanel.Visibility = Visibility.Visible;
                    break;
                case "Models":
                    ModelsPanel.Visibility = Visibility.Visible;
                    break;
                case "Backend":
                    BackendPanel.Visibility = Visibility.Visible;
                    break;
                case "Notifications":
                    NotificationsPanel.Visibility = Visibility.Visible;
                    break;
                case "Account":
                    AccountPanel.Visibility = Visibility.Visible;
                    break;
                case "About":
                    AboutPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
