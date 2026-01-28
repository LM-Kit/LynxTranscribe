namespace LynxTranscribe.Localization;

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: Text="{l:Translate Key=Transcribe}"
/// Or shorter: Text="{l:Translate Transcribe}"
/// </summary>
[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string? Key { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Key}]",
            Source = LocalizationService.Instance
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Static markup extension that returns the current value (doesn't update on language change).
/// Use for one-time bindings where dynamic update isn't needed.
/// Usage: Text="{l:T Key=Transcribe}"
/// </summary>
[ContentProperty(nameof(Key))]
public class TExtension : IMarkupExtension<string>
{
    public string? Key { get; set; }

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
        {
            return string.Empty;
        }

        return LocalizationService.Instance.Get(Key);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
