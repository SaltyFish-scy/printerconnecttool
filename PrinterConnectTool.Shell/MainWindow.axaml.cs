using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PrinterConnectTool.Models;
using PrinterConnectTool.Services;
using PrinterConnectTool.Shell.Services;

namespace PrinterConnectTool.Shell;

public partial class MainWindow : Window
{
    private readonly GuiLogger _logger;
    private CancellationTokenSource? _installCts;

    public MainWindow()
    {
        InitializeComponent();
        _logger = new GuiLogger(OnLogReceived);
        Loaded += OnLoaded;

        // 在构造函数末尾一次性提取 Payload，并同步等待，确保后续所有服务都能读取 payload 目录
        ExtractPayloadOnce();
    }

    private void ExtractPayloadOnce()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
            var exeDir = Path.GetDirectoryName(exePath)!;
            var payloadRoot = Path.Combine(exeDir, "payload");

            var extracted = SelfPayloadExtractor.ExtractPayload(exePath, payloadRoot);
            if (!string.IsNullOrEmpty(extracted))
            {
                PayloadLocator.SetPayloadRoot(extracted);
                _logger.Info($"Payload 已解压到: {extracted}");
            }
            else
            {
                _logger.Warning("未找到 Payload 尾部标记。程序可能不是由生成器生成。");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Payload 提取失败: {ex.Message}");
        }
    }

    private void OnLogReceived(string message, LogLevel level)
    {
        Dispatcher.UIThread.Post(() => AppendLog(message, level));
    }

    private void AppendLog(string message, LogLevel level)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.Text += $"[{time}] {message}\n";
        LogScrollViewer.ScrollToEnd();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!PayloadLocator.HasPayloadRoot)
            {
                ShowError("未找到 Payload。请确认本程序是由生成器生成的 EXE。");
                _logger.Error("未找到 Payload。请确认本程序是由生成器生成的 EXE。");
                return;
            }

            var payloadRoot = PayloadLocator.PayloadRoot;
            _logger.Info("Payload 已解压，正在加载配置...");
            var config = PayloadConfigLoader.Load(payloadRoot);

            _logger.Info("正在探测职场网络...");
            var detector = new WorkplaceDetector(config);
            var workplace = await detector.DetectAsync();
            await OnWorkplaceDetectedAsync(workplace);
        }
        catch (Exception ex)
        {
            ShowError($"初始化失败: {ex.Message}");
            _logger.Error($"初始化失败: {ex.Message}");
        }
    }

    private async Task OnWorkplaceDetectedAsync(WorkplaceConfig? workplace)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusText.IsVisible = false;

            if (workplace == null)
            {
                WorkplaceText.Text = "无法探测到所在职场";
                WorkplaceText.Foreground = Brushes.OrangeRed;
                WorkplaceText.IsVisible = true;
                HintText.Text = "请关闭代理软件，或检查是否连接公司内网。";
                HintText.IsVisible = true;
                _logger.Error("无法探测到所在职场，请关闭代理软件或检查公司内网连接。");
                return;
            }

            WorkplaceText.Text = $"已探测到所在职场：{workplace.Name}";
            WorkplaceText.IsVisible = true;
            _logger.Success($"已探测到所在职场：{workplace.Name}");

            if (workplace.Printers.Count == 0)
            {
                HintText.Text = $"职场 [{workplace.Name}] 暂无可用打印机配置。";
                HintText.IsVisible = true;
                _logger.Warning($"职场 [{workplace.Name}] 暂无可用打印机配置。");
                AdjustWindowSizeForPrinters(0);
                return;
            }

            HintText.Text = "请点击上方按钮选择要连接的打印机：";
            HintText.IsVisible = true;
            _logger.Info("请点击上方按钮选择要连接的打印机。");

            AdjustWindowSizeForPrinters(workplace.Printers.Count);

            foreach (var printer in workplace.Printers)
            {
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = RelativePoint.TopLeft,
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#B3E5FC"), 0.0),
                        new GradientStop(Color.Parse("#E6F6FF"), 0.20),
                        new GradientStop(Color.Parse("#FFFFFF"), 0.25),
                        new GradientStop(Color.Parse("#FFFFFF"), 1.0)
                    }
                };

                var hoverBrush = new LinearGradientBrush
                {
                    StartPoint = RelativePoint.TopLeft,
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#81D4FA"), 0.0),
                        new GradientStop(Color.Parse("#B3E5FC"), 0.20),
                        new GradientStop(Color.Parse("#E1F5FE"), 0.25),
                        new GradientStop(Color.Parse("#E1F5FE"), 1.0)
                    }
                };

                var button = new Button
                {
                    Content = $"{printer.Name}\nIP: {printer.Ip}",
                    Width = 170,
                    Height = 64,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(8, 10),
                    Margin = new Thickness(6),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#004C8C")),
                    CornerRadius = new CornerRadius(10),
                    Background = gradientBrush,
                    BorderBrush = new SolidColorBrush(Color.Parse("#81D4FA")),
                    BorderThickness = new Thickness(1),
                    Tag = printer
                };

                button.PointerEntered += (_, _) => button.Background = hoverBrush;
                button.PointerExited += (_, _) => button.Background = gradientBrush;

                button.Click += OnPrinterButtonClick;
                PrintersPanel.Children.Add(button);
            }
        });
    }

    private async void OnPrinterButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PrinterConfig printer }) return;

        SetButtonsEnabled(false);
        _installCts = new CancellationTokenSource();

        try
        {
            AppendLog($"开始安装: {printer.Name}", LogLevel.Info);
            await PrinterConnectWorkflow.InstallAsync(printer, _logger, true, _installCts.Token);
        }
        catch (Exception ex)
        {
            _logger.Error($"安装异常: {ex.Message}");
        }
        finally
        {
            _installCts?.Dispose();
            _installCts = null;
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var child in PrintersPanel.Children)
                if (child is Button button)
                    button.IsEnabled = enabled;
        });
    }

    private void AdjustWindowSizeForPrinters(int printerCount)
    {
        Dispatcher.UIThread.Post(() =>
        {
            const double baseHeight = 360;
            const double rowHeight = 76;
            const int maxVisibleRows = 3;
            const double buttonWidth = 170;
            const double buttonMargin = 12;
            const double paddingAndBorder = 48; // 14*2 margin + 10*2 padding

            var rows = (printerCount + 3) / 4;
            var visibleRows = Math.Clamp(rows, 1, maxVisibleRows);
            var targetHeight = baseHeight + visibleRows * rowHeight;

            var buttonsInFirstRow = Math.Min(printerCount, 4);
            var targetWidth = buttonsInFirstRow * (buttonWidth + buttonMargin) - buttonMargin + paddingAndBorder + 40;
            targetWidth = Math.Max(targetWidth, 520); // 最小宽度
            targetWidth = Math.Min(targetWidth, 820); // 最大宽度（4 按钮）

            this.Height = targetHeight;
            this.MinHeight = targetHeight;
            this.Width = targetWidth;
            this.MinWidth = targetWidth;
        });
    }

    private void ShowError(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = message;
            StatusText.Foreground = Brushes.OrangeRed;
        });
    }
}
