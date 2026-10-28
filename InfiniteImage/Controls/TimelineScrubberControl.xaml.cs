using InfiniteImage.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InfiniteImage.Controls;

public sealed partial class TimelineScrubberControl : UserControl, INotifyPropertyChanged
{
    private const double TotalBarWidth = 240.0;

    private string _currentDateText = string.Empty;
    private string _earliestDateText = string.Empty;
    private string _latestDateText = string.Empty;
    private double _progressWidth;

    public TimelineScrubberControl()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty CurrentZProperty =
        DependencyProperty.Register(
            nameof(CurrentZ),
            typeof(float),
            typeof(TimelineScrubberControl),
            new PropertyMetadata(0f, OnCurrentZChanged));

    public static readonly DependencyProperty MinZProperty =
        DependencyProperty.Register(
            nameof(MinZ),
            typeof(float),
            typeof(TimelineScrubberControl),
            new PropertyMetadata(0f));

    public static readonly DependencyProperty MaxZProperty =
        DependencyProperty.Register(
            nameof(MaxZ),
            typeof(float),
            typeof(TimelineScrubberControl),
            new PropertyMetadata(1000f));

    public static readonly DependencyProperty EarliestDateProperty =
        DependencyProperty.Register(
            nameof(EarliestDate),
            typeof(DateTimeOffset),
            typeof(TimelineScrubberControl),
            new PropertyMetadata(DateTimeOffset.Now, OnDateChanged));

    public static readonly DependencyProperty LatestDateProperty =
        DependencyProperty.Register(
            nameof(LatestDate),
            typeof(DateTimeOffset),
            typeof(TimelineScrubberControl),
            new PropertyMetadata(DateTimeOffset.Now, OnDateChanged));

    public float CurrentZ
    {
        get => (float)GetValue(CurrentZProperty);
        set => SetValue(CurrentZProperty, value);
    }

    public float MinZ
    {
        get => (float)GetValue(MinZProperty);
        set => SetValue(MinZProperty, value);
    }

    public float MaxZ
    {
        get => (float)GetValue(MaxZProperty);
        set => SetValue(MaxZProperty, value);
    }

    public DateTimeOffset EarliestDate
    {
        get => (DateTimeOffset)GetValue(EarliestDateProperty);
        set => SetValue(EarliestDateProperty, value);
    }

    public DateTimeOffset LatestDate
    {
        get => (DateTimeOffset)GetValue(LatestDateProperty);
        set => SetValue(LatestDateProperty, value);
    }

    public string CurrentDateText
    {
        get => _currentDateText;
        private set => SetField(ref _currentDateText, value);
    }

    public string EarliestDateText
    {
        get => _earliestDateText;
        private set => SetField(ref _earliestDateText, value);
    }

    public string LatestDateText
    {
        get => _latestDateText;
        private set => SetField(ref _latestDateText, value);
    }

    public double ProgressWidth
    {
        get => _progressWidth;
        private set => SetField(ref _progressWidth, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static void OnCurrentZChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineScrubberControl control)
        {
            control.UpdateCurrentDate();
            control.UpdateProgressWidth();
        }
    }

    private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineScrubberControl control)
        {
            control.UpdateDateLabels();
            control.UpdateCurrentDate();
            control.UpdateProgressWidth();
        }
    }

    private void UpdateCurrentDate()
    {
        var date = TimelineConfig.CalculateDateForZ(CurrentZ, EarliestDate);
        CurrentDateText = date.ToString("yyyy");
    }

    private void UpdateDateLabels()
    {
        EarliestDateText = EarliestDate.ToString("yyyy");
        LatestDateText = LatestDate.ToString("yyyy");
    }

    private void UpdateProgressWidth()
    {
        ProgressWidth = MaxZ > 0
            ? Math.Clamp(CurrentZ / MaxZ, 0.0, 1.0) * TotalBarWidth
            : 0;
    }
}
