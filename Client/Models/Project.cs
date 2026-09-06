using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

public partial class Project : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _color = "#7C5CFC";

    [ObservableProperty]
    private ObservableCollection<string> _chatIds = [];
}
