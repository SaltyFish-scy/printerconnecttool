using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PrinterConnectTool.Generator.Models;
using PrinterConnectTool.Generator.Services;

namespace PrinterConnectTool.Generator;

public partial class MainWindow : Window
{
    private GeneratorProject _project = new();
    private object? _selectedItem;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        BindLists();
        SubscribeEditorEvents();
        UpdatePreview();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RefreshComboBoxes();
    }

    private void BindLists()
    {
        DriversList.ItemsSource = _project.Drivers;
        DriversList.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(DriverPackage.DisplayName));

        OfficesList.ItemsSource = _project.Offices;
        OfficesList.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(OfficeDefinition.Name));

        PrintersList.ItemsSource = _project.Printers;
        PrintersList.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(PrinterDefinition.Name));
    }

    private void RefreshComboBoxes()
    {
        var selectedOffice = PrinterOfficeCombo.SelectedItem;
        var selectedDriver = PrinterDriverCombo.SelectedItem;

        PrinterOfficeCombo.ItemsSource = _project.Offices;
        PrinterOfficeCombo.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(OfficeDefinition.Name));

        PrinterDriverCombo.ItemsSource = _project.Drivers;
        PrinterDriverCombo.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(DriverPackage.Brand));

        if (selectedOffice != null && _project.Offices.Contains(selectedOffice))
            PrinterOfficeCombo.SelectedItem = selectedOffice;
        else if (_project.Offices.Count > 0)
            PrinterOfficeCombo.SelectedIndex = 0;

        if (selectedDriver != null && _project.Drivers.Contains(selectedDriver))
            PrinterDriverCombo.SelectedItem = selectedDriver;
        else if (_project.Drivers.Count > 0)
            PrinterDriverCombo.SelectedIndex = 0;
    }

    private void SubscribeEditorEvents()
    {
        DriverBrandText.TextChanged += (_, _) => { if (_selectedItem is DriverPackage d) d.Brand = DriverBrandText.Text ?? ""; UpdatePreview(); };
        DriverDisplayNameText.TextChanged += (_, _) => { if (_selectedItem is DriverPackage d) d.DisplayName = DriverDisplayNameText.Text ?? ""; UpdatePreview(); };
        DriverDefaultNameText.TextChanged += (_, _) => { if (_selectedItem is DriverPackage d) d.DefaultDriverName = DriverDefaultNameText.Text ?? ""; UpdatePreview(); };
        OfficeNameText.TextChanged += (_, _) => { if (_selectedItem is OfficeDefinition o) o.Name = OfficeNameText.Text ?? ""; UpdatePreview(); };
        OfficeGatewayText.TextChanged += (_, _) => { if (_selectedItem is OfficeDefinition o) o.GatewayIp = OfficeGatewayText.Text ?? ""; UpdatePreview(); };
        PrinterNameText.TextChanged += (_, _) => { if (_selectedItem is PrinterDefinition p) p.Name = PrinterNameText.Text ?? ""; UpdatePreview(); };
        PrinterIpText.TextChanged += (_, _) => { if (_selectedItem is PrinterDefinition p) p.Ip = PrinterIpText.Text ?? ""; UpdatePreview(); };
        PrinterPortNumber.ValueChanged += (_, _) => { if (_selectedItem is PrinterDefinition p) p.PortNumber = (int)(PrinterPortNumber.Value ?? 9100); UpdatePreview(); };
        PrinterOfficeCombo.SelectionChanged += (_, _) => { if (_selectedItem is PrinterDefinition p && PrinterOfficeCombo.SelectedItem is OfficeDefinition o) p.OfficeId = o.Id; UpdatePreview(); };
        PrinterDriverCombo.SelectionChanged += (_, _) =>
        {
            if (_selectedItem is PrinterDefinition p && PrinterDriverCombo.SelectedItem is DriverPackage d)
            {
                p.DriverBrand = d.Brand;
                if (string.IsNullOrWhiteSpace(PrinterDriverNameText.Text) || _project.Drivers.Any(x => x.DefaultDriverName == PrinterDriverNameText.Text))
                {
                    p.DriverName = d.DefaultDriverName;
                    PrinterDriverNameText.Text = d.DefaultDriverName;
                }
            }
            UpdatePreview();
        };
        PrinterDriverNameText.TextChanged += (_, _) => { if (_selectedItem is PrinterDefinition p) p.DriverName = PrinterDriverNameText.Text ?? ""; UpdatePreview(); };
    }

    private void AddDriver_Click(object? sender, RoutedEventArgs e)
    {
        var driver = new DriverPackage { Brand = "NewBrand", DisplayName = "新驱动包", DefaultDriverName = "驱动名" };
        _project.Drivers.Add(driver);
        DriversList.SelectedItem = driver;
        RefreshComboBoxes();
        UpdatePreview();
    }

    private void AddOffice_Click(object? sender, RoutedEventArgs e)
    {
        var office = new OfficeDefinition { Name = "新职场", GatewayIp = "10.0.0.1" };
        _project.Offices.Add(office);
        OfficesList.SelectedItem = office;
        RefreshComboBoxes();
        UpdatePreview();
    }

    private void AddPrinter_Click(object? sender, RoutedEventArgs e)
    {
        var office = _project.Offices.FirstOrDefault();
        var driver = _project.Drivers.FirstOrDefault();
        var printer = new PrinterDefinition
        {
            Name = "新打印机",
            Ip = "10.0.0.10",
            OfficeId = office?.Id ?? "",
            DriverBrand = driver?.Brand ?? "",
            DriverName = driver?.DefaultDriverName ?? ""
        };
        _project.Printers.Add(printer);
        PrintersList.SelectedItem = printer;
        UpdatePreview();
    }

    private void RemoveDriver_Click(object? sender, RoutedEventArgs e)
    {
        if (DriversList.SelectedItem is DriverPackage d)
        {
            _project.Drivers.Remove(d);
            _selectedItem = null;
            ClearEditor();
            RefreshComboBoxes();
            UpdatePreview();
        }
    }

    private void RemoveOffice_Click(object? sender, RoutedEventArgs e)
    {
        if (OfficesList.SelectedItem is OfficeDefinition o)
        {
            _project.Offices.Remove(o);
            _selectedItem = null;
            ClearEditor();
            RefreshComboBoxes();
            UpdatePreview();
        }
    }

    private void RemovePrinter_Click(object? sender, RoutedEventArgs e)
    {
        if (PrintersList.SelectedItem is PrinterDefinition p)
        {
            _project.Printers.Remove(p);
            _selectedItem = null;
            ClearEditor();
            UpdatePreview();
        }
    }

    private async void BrowseDriverZip_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择驱动 ZIP",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("ZIP 压缩包") { Patterns = new[] { "*.zip" } } }
        });

        if (files.Count > 0 && _selectedItem is DriverPackage driver)
        {
            driver.ZipFilePath = files[0].Path.LocalPath;
            DriverZipText.Text = driver.ZipFilePath;
            UpdatePreview();
        }
    }

    private async void Generate_Click(object? sender, RoutedEventArgs e)
    {
        var validation = ValidationService.Validate(_project);
        if (!validation.IsValid)
        {
            StatusText.Text = $"生成失败：{string.Join("；", validation.Errors)}";
            AppendLog("验证失败：");
            foreach (var error in validation.Errors)
                AppendLog("  - " + error);
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存生成的 EXE",
            DefaultExtension = "exe",
            SuggestedFileName = "PrinterConnectTool.Desktop.exe"
        });

        if (file == null) return;

        try
        {
            AppendLog("开始生成 EXE...");
            StatusText.Text = "正在生成...";
            ShellPublisher.Publish(_project, file.Path.LocalPath);
            StatusText.Text = "生成成功";
            AppendLog($"已生成：{file.Path.LocalPath}");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"生成失败：{ex.Message}";
            AppendLog($"生成失败：{ex}");
        }
    }

    private async void SaveProject_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存项目",
            DefaultExtension = "json",
            SuggestedFileName = "printer-project.json"
        });

        if (file == null) return;

        ProjectSerializer.Save(_project, file.Path.LocalPath);
        StatusText.Text = $"项目已保存：{file.Path.LocalPath}";
    }

    private async void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开项目",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON 项目") { Patterns = new[] { "*.json" } } }
        });

        if (files.Count == 0) return;

        _project = ProjectSerializer.Load(files[0].Path.LocalPath);
        BindLists();
        RefreshComboBoxes();
        ClearEditor();
        UpdatePreview();
        StatusText.Text = $"项目已加载：{files[0].Path.LocalPath}";
    }

    private void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        _project = new GeneratorProject();
        BindLists();
        RefreshComboBoxes();
        ClearEditor();
        UpdatePreview();
        StatusText.Text = "新建项目";
    }

    private void DriversList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedItem = DriversList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void OfficesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedItem = OfficesList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void PrintersList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedItem = PrintersList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void EditSelectedDriver_Click(object? sender, RoutedEventArgs e)
    {
        _selectedItem = DriversList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void EditSelectedOffice_Click(object? sender, RoutedEventArgs e)
    {
        _selectedItem = OfficesList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void EditSelectedPrinter_Click(object? sender, RoutedEventArgs e)
    {
        _selectedItem = PrintersList.SelectedItem;
        ShowEditor(_selectedItem);
    }

    private void ClearEditor()
    {
        EditorPanel.IsEnabled = false;
        DriverFields.IsVisible = false;
        OfficeFields.IsVisible = false;
        PrinterFields.IsVisible = false;
        EditorTitle.Text = "选择左侧项目进行编辑";
    }

    private void ShowEditor(object? item)
    {
        ClearEditor();
        if (item == null) return;

        EditorPanel.IsEnabled = true;

        switch (item)
        {
            case DriverPackage driver:
                EditorTitle.Text = "编辑驱动包";
                DriverFields.IsVisible = true;
                DriverBrandText.Text = driver.Brand;
                DriverDisplayNameText.Text = driver.DisplayName;
                DriverZipText.Text = driver.ZipFilePath;
                DriverDefaultNameText.Text = driver.DefaultDriverName;
                break;
            case OfficeDefinition office:
                EditorTitle.Text = "编辑职场";
                OfficeFields.IsVisible = true;
                OfficeNameText.Text = office.Name;
                OfficeGatewayText.Text = office.GatewayIp;
                break;
            case PrinterDefinition printer:
                EditorTitle.Text = "编辑打印机";
                PrinterFields.IsVisible = true;
                PrinterNameText.Text = printer.Name;
                PrinterIpText.Text = printer.Ip;
                PrinterPortNumber.Value = printer.PortNumber;
                PrinterOfficeCombo.SelectedItem = _project.Offices.FirstOrDefault(o => o.Id == printer.OfficeId);
                PrinterDriverCombo.SelectedItem = _project.Drivers.FirstOrDefault(d => d.Brand == printer.DriverBrand);
                PrinterDriverNameText.Text = printer.DriverName;
                break;
        }
    }

    private void UpdatePreview()
    {
        try
        {
            var config = PayloadBuilder.BuildAppConfig(_project);
            PreviewJson.Text = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            PreviewJson.Text = $"预览生成失败：{ex.Message}";
        }
    }

    private void AppendLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.Text += $"[{time}] {message}\n";
        LogScrollViewer.ScrollToEnd();
    }
}
