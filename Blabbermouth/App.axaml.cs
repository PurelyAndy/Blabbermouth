using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Blabbermouth;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // async/Task exceptions
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            ShowException(e.Exception);
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        ShowException(e.ExceptionObject as Exception);
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        ShowException(e.Exception);
    }

    private static void ShowException(Exception? ex)
    {
        if (ex == null)
            return;

        File.WriteAllText($"error_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.log", ex.ToString());
        Dispatcher.UIThread.Invoke(() =>
        {
            Button copyButton = new()
            {
                Content = "Copy to Clipboard",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new(10),
                Padding = new(10, 5),
            };
            copyButton.Click += (_, _) =>
            {
                MainWindow.I.Clipboard!.SetTextAsync(ex.ToString());
            };
            Window errorWindow = new()
            {
                Title = "An error occurred",
                Content = new StackPanel
                {
                    Children =
                    {
                        new SelectableTextBlock
                        {
                            Text = ex.ToString(),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new(10),
                        },
                        copyButton,
                    },
                },
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            errorWindow.ShowDialog(MainWindow.I);
        });
    }
}