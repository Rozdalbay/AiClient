using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

public partial class ModelInfo : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _provider = string.Empty;

    [ObservableProperty]
    private double _inputTokenPrice;

    [ObservableProperty]
    private double _outputTokenPrice;

    [ObservableProperty]
    private bool _isAvailable = true;

    [ObservableProperty]
    private int _contextWindow;

    [ObservableProperty]
    private string _icon = "\uE99A"; 
}
