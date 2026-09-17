using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

    // ShowEditor 给编辑器字段赋值期间置位，阻止事件把同值写回模型造成连锁副作用
    private bool _loadingEditor;
    // RefreshComboBoxes 重建下拉框期间置位，阻止选中变化事件静默改写正在编辑的打印机
    private bool _refreshingCombos;
    // ShowEditor 清理其他列表选中态期间置位，防止 SelectionChanged 递归触发
    private bool _syncingSelection;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        ShellTitleText.Text = _project.ShellTitle;
        BindLists();
        SubscribeEditorEvents();
        SubscribeListClickFallback();
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
        _refreshingCombos = true;
        try
        {
            var selectedOffice = PrinterOfficeCombo.SelectedItem;
            var selectedDriver = PrinterDriverCombo.SelectedItem;

            PrinterOfficeCombo.ItemsSource = _project.Offices;
            PrinterOfficeCombo.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(OfficeDefinition.Name));

            PrinterDriverCombo.ItemsSource = _project.Drivers;
            PrinterDriverCombo.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(DriverPackage.Brand));

            if (selectedOffice != null && _project.Offices.Contains(selectedOffice))
                PrinterOfficeCombo.SelectedItem = selectedOffice;
            else if (_project.Offices.Count > 0 && _selectedItem is PrinterDefinition)
                PrinterOfficeCombo.SelectedIndex = 0;

            if (selectedDriver != null && _project.Drivers.Contains(selectedDriver))
                PrinterDriverCombo.SelectedItem = selectedDriver;
            else if (_project.Drivers.Count > 0 && _selectedItem is PrinterDefinition)
                PrinterDriverCombo.SelectedIndex = 0;
        }
        finally
        {
            _refreshingCombos = false;
        }
    }

    /// <summary>
    ///     ListBox 点击已选中项不会触发 SelectionChanged，这里用 PointerReleased 兜底：
    ///     只要点中列表项且编辑器当前不是它，就强制切换编辑器。
    /// </summary>
    private void SubscribeListClickFallback()
    {
        DriversList.PointerReleased += (_, _) => ReopenIfNeeded(DriversList);
        OfficesList.PointerReleased += (_, _) => ReopenIfNeeded(OfficesList);
        PrintersList.PointerReleased += (_, _) => ReopenIfNeeded(PrintersList);
    }

    private void ReopenIfNeeded(ListBox list)
    {
        if (_syncingSelection) return;
        if (list.SelectedItem != null && !ReferenceEquals(_selectedItem, list.SelectedItem))
            ShowEditor(list.SelectedItem);
    }

    private void SubscribeEditorEvents()
    {
        ShellTitleText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            _project.ShellTitle = ShellTitleText.Text ?? "";
            UpdatePreview();
        };

        DriverBrandText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is DriverPackage d)
            {
                d.Brand = DriverBrandText.Text ?? "";
                SyncPrintersDriverSnapshot(d);
            }
            UpdateBrandError();
            if (_selectedItem is DriverPackage driver2) UpdateZipError(driver2);
            UpdatePreview();
        };
        DriverDisplayNameText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is DriverPackage d) d.DisplayName = DriverDisplayNameText.Text ?? "";
            UpdatePreview();
        };
        DriverDefaultNameText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is DriverPackage d) d.DefaultDriverName = DriverDefaultNameText.Text ?? "";
            UpdatePreview();
        };
        OfficeNameText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is OfficeDefinition o) o.Name = OfficeNameText.Text ?? "";
            UpdatePreview();
        };
        OfficeGatewayText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is OfficeDefinition o) o.GatewayIp = OfficeGatewayText.Text ?? "";
            UpdatePreview();
        };
        PrinterNameText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is PrinterDefinition p) p.Name = PrinterNameText.Text ?? "";
            UpdatePreview();
        };
        PrinterIpText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is PrinterDefinition p) p.Ip = PrinterIpText.Text ?? "";
            UpdatePreview();
        };
        PrinterPortNumber.ValueChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is PrinterDefinition p) p.PortNumber = (int)(PrinterPortNumber.Value ?? 9100);
            UpdatePreview();
        };
        PrinterOfficeCombo.SelectionChanged += (_, _) =>
        {
            if (_loadingEditor || _refreshingCombos) return;
            if (_selectedItem is PrinterDefinition p && PrinterOfficeCombo.SelectedItem is OfficeDefinition o)
                p.OfficeId = o.Id;
            UpdatePreview();
        };
        PrinterDriverCombo.SelectionChanged += (_, _) =>
        {
            if (_loadingEditor || _refreshingCombos) return;
            if (_selectedItem is PrinterDefinition p && PrinterDriverCombo.SelectedItem is DriverPackage d)
            {
                p.DriverId = d.Id;
                p.DriverBrand = d.Brand;
                // 驱动名未自定义（为空或仍是某个驱动的默认名）时跟随新驱动，否则保留用户自定义值
                if (string.IsNullOrWhiteSpace(p.DriverName) || _project.Drivers.Any(x => x.DefaultDriverName == p.DriverName))
                {
                    p.DriverName = "";
                    PrinterDriverNameText.Text = "";
                }
            }
            UpdatePreview();
        };
        PrinterDriverNameText.TextChanged += (_, _) =>
        {
            if (_loadingEditor) return;
            if (_selectedItem is PrinterDefinition p) p.DriverName = PrinterDriverNameText.Text ?? "";
            UpdatePreview();
        };
    }

    /// <summary>驱动 Brand 改动后，同步所有引用该驱动的打印机的冗余快照字段，保持项目文件一致。</summary>
    private void SyncPrintersDriverSnapshot(DriverPackage driver)
    {
        foreach (var p in _project.Printers)
            if (p.DriverId == driver.Id)
                p.DriverBrand = driver.Brand;
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
            DriverId = driver?.Id ?? "",
            DriverBrand = driver?.Brand ?? "",
            DriverName = "" // 留空跟随驱动包默认驱动名
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

        if (files.Count == 0) return;
        if (_selectedItem is not DriverPackage driver) return;

        var path = files[0].Path.LocalPath;
        if (!ValidationService.IsValidZipFileName(path))
        {
            DriverZipError.Text = "ZIP 文件名只能包含英文、数字、短横线和下划线，且以 .zip 结尾。";
            DriverZipError.IsVisible = true;
            StatusText.Text = "选择的 ZIP 文件名不合法";
            return;
        }

        var brand = driver.Brand;
        if (!ValidationService.IsValidZipStructure(path, brand))
        {
            DriverZipError.Text = $"ZIP 顶层缺少与 Brand “{brand}” 同名的文件夹，请检查压缩包结构。";
            DriverZipError.IsVisible = true;
            StatusText.Text = "选择的 ZIP 压缩包结构不正确";
            return;
        }

        DriverZipError.IsVisible = false;
        driver.ZipFilePath = path;
        driver.ZipData = await File.ReadAllBytesAsync(path);
        DriverZipText.Text = driver.ZipFilePath;
        AppendLog($"已读取 ZIP 文件：{path} ({driver.ZipData.Length} 字节)");
        UpdatePreview();
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

        var outputPath = file.Path.LocalPath;
        if (!outputPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            outputPath += ".exe";
            AppendLog($"输出路径已自动补全为 .exe：{outputPath}");
        }

        try
        {
            AppendLog("开始生成 EXE...");
            StatusText.Text = "正在生成...";
            ShellPublisher.Publish(_project, outputPath);
            StatusText.Text = "生成成功";
            AppendLog($"已生成：{outputPath}");
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
        ShellTitleText.Text = _project.ShellTitle;
        BindLists();
        RefreshComboBoxes();
        ClearEditor();
        UpdatePreview();
        StatusText.Text = $"项目已加载：{files[0].Path.LocalPath}";
    }

    private void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        _project = new GeneratorProject();
        ShellTitleText.Text = _project.ShellTitle;
        BindLists();
        RefreshComboBoxes();
        ClearEditor();
        UpdatePreview();
        StatusText.Text = "新建项目";
    }

    private void DriversList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection) return;
        ShowEditor(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);
    }

    private void OfficesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection) return;
        ShowEditor(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);
    }

    private void PrintersList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection) return;
        ShowEditor(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);
    }

    private void ClearEditor()
    {
        EditorPanel.IsEnabled = false;
        DriverFields.IsVisible = false;
        OfficeFields.IsVisible = false;
        PrinterFields.IsVisible = false;
        DriverBrandError.IsVisible = false;
        DriverZipError.IsVisible = false;
        EditorTitle.Text = "选择左侧项目进行编辑";
    }

    private void ShowEditor(object? item)
    {
        _selectedItem = item;

        // 三个列表的选中高亮互斥：编辑器切到哪类项，就清掉另外两个列表的选中态，
        // 避免"另一个列表还亮着"造成误导；清理期间阻止 SelectionChanged 递归
        _syncingSelection = true;
        try
        {
            switch (item)
            {
                case DriverPackage:
                    OfficesList.SelectedItem = null;
                    PrintersList.SelectedItem = null;
                    break;
                case OfficeDefinition:
                    DriversList.SelectedItem = null;
                    PrintersList.SelectedItem = null;
                    break;
                case PrinterDefinition:
                    DriversList.SelectedItem = null;
                    OfficesList.SelectedItem = null;
                    break;
            }
        }
        finally
        {
            _syncingSelection = false;
        }

        ClearEditor();
        if (item == null) return;

        EditorPanel.IsEnabled = true;

        _loadingEditor = true;
        try
        {
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
                    // 按 DriverId 活引用解析，驱动改名后仍能找回；旧数据回退 Brand 匹配
                    PrinterDriverCombo.SelectedItem = _project.FindDriver(printer);
                    PrinterDriverNameText.Text = printer.DriverName;
                    break;
            }
        }
        finally
        {
            _loadingEditor = false;
        }

        if (item is DriverPackage d)
        {
            UpdateBrandError();
            UpdateZipError(d);
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            var config = PayloadBuilder.BuildAppConfig(_project);
            // 预览除最终 workplaces.json 外，附带驱动包信息，保证任何字段的修改都有可见反馈
            var preview = new
            {
                settings = config.Settings,
                drivers = _project.Drivers.Select(d => new
                {
                    d.Brand,
                    d.DisplayName,
                    d.DefaultDriverName,
                    zip = d.ZipData is { Length: > 0 } z
                        ? $"已嵌入项目（{z.Length:N0} 字节）"
                        : string.IsNullOrWhiteSpace(d.ZipFilePath) ? "（未选择 ZIP）" : d.ZipFilePath
                }),
                config.Workplaces
            };
            PreviewJson.Text = System.Text.Json.JsonSerializer.Serialize(preview, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (Exception ex)
        {
            PreviewJson.Text = $"预览生成失败：{ex.Message}";
        }
    }

    private void UpdateBrandError()
    {
        var brand = DriverBrandText.Text ?? "";
        if (string.IsNullOrWhiteSpace(brand) || ValidationService.IsValidBrand(brand))
        {
            DriverBrandError.IsVisible = false;
            return;
        }

        DriverBrandError.Text = "Brand 只能包含英文、数字、短横线和下划线。";
        DriverBrandError.IsVisible = true;
    }

    private void UpdateZipError(string zipPath, string brand)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            DriverZipError.IsVisible = false;
            return;
        }

        if (!ValidationService.IsValidZipFileName(zipPath))
        {
            DriverZipError.Text = "ZIP 文件名只能包含英文、数字、短横线和下划线，且以 .zip 结尾。";
            DriverZipError.IsVisible = true;
            return;
        }

        if (!ValidationService.IsValidBrand(brand))
        {
            DriverZipError.IsVisible = false;
            return;
        }

        if (ValidationService.IsValidZipStructure(zipPath, brand))
        {
            DriverZipError.IsVisible = false;
        }
        else
        {
            DriverZipError.Text = $"ZIP 顶层缺少与 Brand “{brand}” 同名的文件夹，请检查压缩包结构。";
            DriverZipError.IsVisible = true;
        }
    }

    private void UpdateZipError(DriverPackage driver)
    {
        if (driver.ZipData is { Length: > 0 } zipData)
        {
            if (!ValidationService.IsValidBrand(driver.Brand))
            {
                DriverZipError.IsVisible = false;
                return;
            }

            if (ValidationService.IsValidZipStructure(zipData, driver.Brand))
            {
                DriverZipError.IsVisible = false;
            }
            else
            {
                DriverZipError.Text = $"ZIP 顶层缺少与 Brand “{driver.Brand}” 同名的文件夹，请检查压缩包结构。";
                DriverZipError.IsVisible = true;
            }
            return;
        }

        UpdateZipError(driver.ZipFilePath, driver.Brand);
    }

    private void AppendLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.Text += $"[{time}] {message}\n";
        LogScrollViewer.ScrollToEnd();
    }
}
