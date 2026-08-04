using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PrinterConnectTool.Models;
using PrinterConnectTool.Services;

namespace PrinterConnectTool.UI;

public partial class MainWindow : Window
{
    private readonly GuiLogger _logger;
    private CancellationTokenSource? _installCts;

    public MainWindow()
    {
        InitializeComponent();
        _logger = new GuiLogger(OnLogReceived);

        Loaded += OnLoaded;
    }

    private void OnLogReceived(string message, LogLevel level)
    {
        Dispatcher.UIThread.Post(() => AppendLog(message, level));
    }

    private void AppendLog(string message, LogLevel level)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.Text += $"[{time}] {message}\n";

        // 自动滚动到底部
        LogScrollViewer.ScrollToEnd();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            var (_, workplace) = await PrinterConnectWorkflow.DetectWorkplaceAsync(_logger);
            await OnWorkplaceDetectedAsync(workplace);
        }
        catch (Exception ex)
        {
            ShowError($"初始化失败: {ex.Message}");
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
                return;
            }

            HintText.Text = "请点击上方按钮选择要连接的打印机：";
            HintText.IsVisible = true;
            _logger.Info("请点击上方按钮选择要连接的打印机。");

            foreach (var printer in workplace.Printers)
            {
                var button = new Button
                {
                    Content = $"{printer.Name}\nIP: {printer.Ip}",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(10, 6),
                    Margin = new Thickness(4),
                    FontSize = 12,
                    Tag = printer
                };
                button.Click += OnPrinterButtonClick;
                PrintersPanel.Children.Add(button);
            }
        });
    }

    private async void OnPrinterButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PrinterConfig printer }) return;

        // 禁用所有按钮，避免重复点击
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

    private void ShowError(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = message;
            StatusText.Foreground = Brushes.OrangeRed;
        });
    }
}