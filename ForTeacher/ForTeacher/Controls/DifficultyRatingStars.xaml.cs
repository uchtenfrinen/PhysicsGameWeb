using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForTeacher.Controls;

public partial class DifficultyRatingStars : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(DifficultyRatingStars),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty ReasoningToolTipProperty = DependencyProperty.Register(
        nameof(ReasoningToolTip),
        typeof(string),
        typeof(DifficultyRatingStars),
        new PropertyMetadata(string.Empty, OnReasoningChanged));

    public DifficultyRatingStars()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyStars();
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string ReasoningToolTip
    {
        get => (string)GetValue(ReasoningToolTipProperty);
        set => SetValue(ReasoningToolTipProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DifficultyRatingStars c)
        {
            c.ApplyStars();
        }
    }

    private static void OnReasoningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DifficultyRatingStars c)
        {
            c.UpdateToolTip();
        }
    }

    private void UpdateToolTip()
    {
        var t = ReasoningToolTip?.Trim();
        Root.ToolTip = string.IsNullOrEmpty(t) ? null : t;
    }

    private void ApplyStars()
    {
        var v = Math.Clamp(Value, 0, 5);
        foreach (var child in StarsRow.Children)
        {
            if (child is not Button b || b.Tag is not string ts || !int.TryParse(ts, out var star))
            {
                continue;
            }

            var filled = v > 0 && star <= v;
            b.Foreground = filled
                ? new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42))
                : new SolidColorBrush(Color.FromRgb(0x55, 0x5F, 0x72));
        }

        UpdateToolTip();
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string ts } && int.TryParse(ts, out var n))
        {
            Value = n;
        }
    }
}
