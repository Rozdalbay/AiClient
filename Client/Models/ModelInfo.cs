using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

// модель в выпадашке чата: имя, провайдер, цены за токен и контекстное окно; IsAvailable=false - модель серая и недоступная
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
    private bool _isFree;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _icon = "\uE99A"; 
}
