namespace LynxTranscribe.Controls;

public partial class CustomToggle : ContentView
{
    public static readonly BindableProperty IsToggledProperty =
        BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(CustomToggle), false,
            propertyChanged: OnIsToggledChanged);

    public static readonly BindableProperty OnColorProperty =
        BindableProperty.Create(nameof(OnColor), typeof(Color), typeof(CustomToggle), Color.FromArgb("#F59E0B"));

    public static readonly BindableProperty OffColorProperty =
        BindableProperty.Create(nameof(OffColor), typeof(Color), typeof(CustomToggle), Color.FromArgb("#3F3F46"));

    public static readonly BindableProperty ThumbColorProperty =
        BindableProperty.Create(nameof(ThumbColor), typeof(Color), typeof(CustomToggle), Colors.White);

    public event EventHandler<ToggledEventArgs>? Toggled;

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public Color OnColor
    {
        get => (Color)GetValue(OnColorProperty);
        set => SetValue(OnColorProperty, value);
    }

    public Color OffColor
    {
        get => (Color)GetValue(OffColorProperty);
        set => SetValue(OffColorProperty, value);
    }

    public Color ThumbColor
    {
        get => (Color)GetValue(ThumbColorProperty);
        set => SetValue(ThumbColorProperty, value);
    }

    public CustomToggle()
    {
        InitializeComponent();
        UpdateVisualState(false);
    }

    private static void OnIsToggledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CustomToggle toggle)
        {
            toggle.UpdateVisualState(true);
            toggle.Toggled?.Invoke(toggle, new ToggledEventArgs((bool)newValue));
        }
    }

    private void OnToggleTapped(object? sender, TappedEventArgs e)
    {
        IsToggled = !IsToggled;
    }

    private void UpdateVisualState(bool animate)
    {
        var targetColor = IsToggled ? OnColor : OffColor;
        var targetMargin = IsToggled ? new Thickness(22, 0, 0, 0) : new Thickness(2, 0, 0, 0);

        if (animate)
        {
            var colorAnimation = new Animation(v =>
            {
                var currentColor = IsToggled
                    ? Color.FromRgba(
                        OffColor.Red + (OnColor.Red - OffColor.Red) * v,
                        OffColor.Green + (OnColor.Green - OffColor.Green) * v,
                        OffColor.Blue + (OnColor.Blue - OffColor.Blue) * v,
                        1)
                    : Color.FromRgba(
                        OnColor.Red + (OffColor.Red - OnColor.Red) * v,
                        OnColor.Green + (OffColor.Green - OnColor.Green) * v,
                        OnColor.Blue + (OffColor.Blue - OnColor.Blue) * v,
                        1);
                ToggleBackground.BackgroundColor = currentColor;
            }, 0, 1);

            var marginAnimation = new Animation(v =>
            {
                var margin = IsToggled ? 2 + (20 * v) : 22 - (20 * v);
                ToggleThumb.Margin = new Thickness(margin, 0, 0, 0);
            }, 0, 1);

            colorAnimation.Commit(this, "ColorAnimation", 16, 150, Easing.CubicInOut);
            marginAnimation.Commit(this, "MarginAnimation", 16, 150, Easing.CubicInOut);
        }
        else
        {
            ToggleBackground.BackgroundColor = targetColor;
            ToggleThumb.Margin = targetMargin;
        }

        ToggleThumb.BackgroundColor = ThumbColor;
    }
}
