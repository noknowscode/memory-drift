using InfiniteImage.Services;
using InfiniteImage.ViewModels;
using Uno.Resizetizer;

namespace InfiniteImage;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    public static Window? CurrentWindow => (Current as App)?.MainWindow;
    public static IServiceProvider? Services => (Current as App)?.Host?.Services;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<PhotoLibraryService>();
                    services.AddSingleton<ChunkService>();
                    services.AddSingleton<ProjectionService>();
                    services.AddSingleton<ImageCacheService>();
                    services.AddSingleton<PerformanceTelemetry>();
                    services.AddTransient<CanvasViewModel>();
                })
            );

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(LandingPage), args.Arguments);
        }

        MainWindow.Activate();
    }
}
