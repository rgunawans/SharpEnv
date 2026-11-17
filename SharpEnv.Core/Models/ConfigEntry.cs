using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SharpEnv.Core.Models;

public sealed class ConfigEntry : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _value = string.Empty;
    private bool _isSelected = true;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public ConfigSourceType SourceType { get; init; }

    public string ProjectName { get; set; } = "Standalone Files";

    public string RelativeFilePath { get; init; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
