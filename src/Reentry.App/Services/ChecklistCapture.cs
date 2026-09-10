using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Reentry.App.ViewModels;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Reentry.App.Services;

/// <summary>
/// Best-effort HUD PNG. Prefers a non-virtualized off-window clone so both
/// lists render at full height; falls back to the live HUD viewport.
/// Caller must pause HUD ticks for the capture. The off-tree clone copies
/// row text at BuildCaptureVisual; the viewport fallback is still live-bound.
/// </summary>
internal static class ChecklistCapture
{
    private const int CaptureWidth = 540;
    private const int MaxPixelExtent = 8192;

    public static async Task<bool> TrySaveAsync(MainWindow hud, HudViewModel vm, string pngPath)
    {
        ArgumentNullException.ThrowIfNull(hud);
        ArgumentNullException.ThrowIfNull(vm);
        ArgumentException.ThrowIfNullOrWhiteSpace(pngPath);

        try
        {
            if (await TrySaveFullContentAsync(hud, vm, pngPath) && IsUsablePng(pngPath))
                return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            TryDelete(pngPath);
        }

        try
        {
            return await TryRenderElementAsync(hud.CaptureRoot, pngPath) && IsUsablePng(pngPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            TryDelete(pngPath);
            return false;
        }
    }

    private static async Task<bool> TrySaveFullContentAsync(MainWindow hud, HudViewModel vm, string pngPath)
    {
        var xamlRoot = hud.Content?.XamlRoot;
        if (xamlRoot is null)
            return false;

        var view = BuildCaptureVisual(vm);
        var popup = new Popup
        {
            XamlRoot = xamlRoot,
            ShouldConstrainToRootBounds = false,
            IsLightDismissEnabled = false,
            IsHitTestVisible = false,
            Child = view,
            HorizontalOffset = 10000,
            VerticalOffset = 0,
        };

        popup.IsOpen = true;
        try
        {
            await WaitLoadedAsync(view);
            view.Measure(new Windows.Foundation.Size(CaptureWidth, double.PositiveInfinity));
            var height = Math.Max(view.DesiredSize.Height, 1);
            view.Width = CaptureWidth;
            view.Height = height;
            view.UpdateLayout();
            await YieldLowAsync(view);

            if (view.ActualWidth < 1 || view.ActualHeight < 1)
                return false;

            return await TryRenderElementAsync(view, pngPath);
        }
        finally
        {
            popup.IsOpen = false;
            popup.Child = null;
        }
    }

    private static FrameworkElement BuildCaptureVisual(HudViewModel vm)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new TextBlock
        {
            Text = "Reentry",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(new TextBlock
        {
            Text = vm.BootBanner,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(new TextBlock
        {
            Text = vm.BootDetail,
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
        });

        var progress = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 4, 0, 0) };
        progress.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progress.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progress.Children.Add(new ProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = vm.SettledFraction,
            Height = 4,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var summary = new TextBlock
        {
            Text = vm.SettledSummary,
            FontSize = 12,
            Opacity = 0.8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(summary, 1);
        progress.Children.Add(summary);
        stack.Children.Add(progress);

        stack.Children.Add(SectionHeader("Session restore (inferred)"));
        foreach (var row in vm.RestoreRows.ToArray())
            stack.Children.Add(BuildRow(row));

        stack.Children.Add(SectionHeader("Startup apps"));
        foreach (var row in vm.StartupRows.ToArray())
            stack.Children.Add(BuildRow(row));

        var footer = new Grid();
        footer.Children.Add(new TextBlock
        {
            Text = "Session elapsed",
            Opacity = 0.7,
            VerticalAlignment = VerticalAlignment.Center,
        });
        footer.Children.Add(new TextBlock
        {
            Text = vm.FooterElapsed,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(footer);

        Brush background;
        if (Application.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out var brush)
            && brush is Brush theme)
            background = theme;
        else
            background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32));

        return new Border
        {
            Background = background,
            Padding = new Thickness(16),
            Width = CaptureWidth,
            Child = stack,
        };
    }

    private static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 4, 0, 0),
    };

    private static FrameworkElement BuildRow(TrackedAppRow row)
    {
        var grid = new Grid
        {
            ColumnSpacing = 8,
            MinHeight = 22,
            Margin = new Thickness(6, 1, 6, 1),
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new TextBlock
        {
            Text = row.Name,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var managed = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, 0x2D, 0xB4, 0xA0)),
            Padding = new Thickness(5, 1, 5, 1),
            CornerRadius = new CornerRadius(3),
            Visibility = row.ManagedVisibility,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "managed", FontSize = 10, Opacity = 0.9 },
        };
        Grid.SetColumn(managed, 1);
        grid.Children.Add(managed);

        var source = new TextBlock
        {
            Text = row.Source,
            Opacity = 0.6,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(source, 2);
        grid.Children.Add(source);

        var chip = new Border
        {
            Background = row.StateBrush,
            Padding = new Thickness(6, 1, 6, 1),
            CornerRadius = new CornerRadius(3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = row.State,
                Foreground = row.ChipForeground,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
            },
        };
        Grid.SetColumn(chip, 3);
        grid.Children.Add(chip);
        return grid;
    }

    internal static async Task<bool> TryRenderElementAsync(FrameworkElement element, string pngPath)
    {
        if (element.ActualWidth < 1 || element.ActualHeight < 1)
            return false;

        var scale = element.XamlRoot?.RasterizationScale ?? 1.0;
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * scale));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * scale));
        if (height > MaxPixelExtent)
        {
            var shrink = MaxPixelExtent / (double)height;
            width = Math.Max(1, (int)Math.Ceiling(width * shrink));
            height = MaxPixelExtent;
        }

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(element, width, height);
        if (rtb.PixelWidth < 1 || rtb.PixelHeight < 1)
            return false;

        await EncodePngAsync(rtb, pngPath);
        return IsUsablePng(pngPath);
    }

    private static async Task EncodePngAsync(RenderTargetBitmap rtb, string pngPath)
    {
        var buffer = await rtb.GetPixelsAsync();
        var pixels = new byte[buffer.Length];
        using (var reader = DataReader.FromBuffer(buffer))
            reader.ReadBytes(pixels);

        var directory = Path.GetDirectoryName(pngPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var mem = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mem);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)rtb.PixelWidth,
            (uint)rtb.PixelHeight,
            96,
            96,
            pixels);
        await encoder.FlushAsync();
        mem.Seek(0);

        using var output = File.Create(pngPath);
        using var source = mem.AsStreamForRead();
        await source.CopyToAsync(output);
    }

    private static async Task WaitLoadedAsync(FrameworkElement element)
    {
        if (element.IsLoaded)
            return;

        var tcs = new TaskCompletionSource();
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            element.Loaded -= OnLoaded;
            tcs.TrySetResult();
        }

        element.Loaded += OnLoaded;
        if (element.IsLoaded)
        {
            element.Loaded -= OnLoaded;
            tcs.TrySetResult();
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            element.Loaded -= OnLoaded;
        }
    }

    private static async Task YieldLowAsync(FrameworkElement element)
    {
        var tcs = new TaskCompletionSource();
        if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.TrySetResult()))
            tcs.TrySetResult();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsUsablePng(string pngPath)
    {
        try
        {
            return File.Exists(pngPath) && new FileInfo(pngPath).Length > 32;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void TryDelete(string pngPath)
    {
        try
        {
            if (File.Exists(pngPath))
                File.Delete(pngPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
