namespace InfiniteImage.Controls;

public sealed partial class HudPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(HudPanel),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(nameof(PanelContent), typeof(object), typeof(HudPanel),
            new PropertyMetadata(null, OnContentChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public HudPanel()
    {
        this.InitializeComponent();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HudPanel panel)
        {
            panel.TitleText.Text = e.NewValue?.ToString() ?? string.Empty;
        }
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HudPanel panel)
        {
            panel.ContentArea.Content = e.NewValue;
        }
    }
}
