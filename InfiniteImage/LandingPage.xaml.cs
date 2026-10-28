using Microsoft.UI.Xaml.Input;

namespace InfiniteImage;

public sealed partial class LandingPage : Page
{
    public LandingPage()
    {
        this.InitializeComponent();
    }

    private void OnBeginClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MainPage), "random");
    }

    private void OnUploadClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MainPage), "library");
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Windows.System.VirtualKey.R)
        {
            Frame.Navigate(typeof(MainPage), "random");
            e.Handled = true;
        }
    }
}
