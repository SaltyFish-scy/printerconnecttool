本文件夹内的是单独的UI版本，基本不用，你们直接用生成器去生成即可。






# 打印机自助连接工具 (PrinterConnectTool)

> **版本**: V3.0  
> **开发/维护**: 一条小咸鱼  
> **技术栈**: .NET 10 + C# + 独立 PowerShell 脚本

## 项目结构

```
PrinterConnectTool/
├── Program.cs                      # 入口：管理员检查 → 欢迎界面 → 主流程
├── Models/
│   ├── AppConfig.cs                # 全局配置根模型
│   ├── WorkplaceConfig.cs          # 职场数据模型
│   └── PrinterConfig.cs            # 打印机数据模型
├── Services/
│   ├── AdminChecker.cs             # 管理员权限检查 & 自动提权
│   ├── WelcomeScreen.cs            # 欢迎界面渲染
│   ├── ConfigLoader.cs             # 配置加载（外部优先 > 嵌入）
│   ├── WorkplaceDetector.cs        # 并行 Ping 职场探测
│   ├── DriverExtractor.cs          # 从嵌入 ZIP 解压驱动到 C:\Drivers\{brand}\
│   ├── PrinterInstaller.cs         # 释放 PS1 并弹出独立 PowerShell 窗口运行
│   └── Cleaner.cs                  # 预留清理逻辑
├── Config/
│   └── workplaces.json             # 职场 & 打印机配置（嵌入资源）
├── Drivers/                        # 驱动 ZIP 包（嵌入资源）
│   ├── TOSHIBA.zip                 # 东芝驱动压缩包（Windows 上压缩）
│   └── ADC225.zip                  # 震旦驱动压缩包（Windows 上压缩）
├── Scripts/                        # 打印机安装 PS1 脚本（每台打印机一个）
│   ├── install_shanghai_4f_xiaoweihui.ps1
│   ├── install_shanghai_4f_wenyin.ps1
│   ├── install_shanghai_qiche_3f.ps1
│   └── install_wuhan_gonggong.ps1
├── app.manifest                    # 管理员权限声明
└── PrinterConnectTool.csproj       # 项目文件
```

## 运行流程

1. **管理员提权** — 自动 UAC 提权（`app.manifest` + `AdminChecker`）
2. **欢迎界面** — 显示工具名称、版本号
3. **职场探测** — 并行 Ping 所有 `gatewayIp`，3 秒内确定所在职场
4. **打印机列表** — 显示该职场下所有可用打印机
5. **用户选择** — 输入数字选择要连接的打印机
6. **驱动解压** — 从嵌入资源提取 `{brand}.zip`，解压到 `C:\Drivers\{brand}\`
7. **释放 PS1** — 从嵌入资源原样复制对应的 PS1 脚本到 `C:\Drivers\{brand}\install.ps1`
8. **弹出 PowerShell 窗口运行 PS1** — 使用 `-ExecutionPolicy Bypass`，独立窗口
9. **C# 退出** — PS1 自带结果提示和 `pause`，用户按回车后窗口关闭

## 关键技术决策

### 为什么要用 ZIP 包存放驱动？

- 早期版本逐个嵌入驱动文件，在 macOS 开发、Windows 运行环境下出现过文件不一致问题
- 改为在 **Windows 上直接压缩原始驱动文件夹**，C# 只负责解压，确保字节级一致
- ZIP 包结构：`TOSHIBA.zip/TOSHIBA/eSf6u.inf`，解压到 `C:\Drivers\` 后得到 `C:\Drivers\TOSHIBA\eSf6u.inf`

### 为什么要用独立 PS1 脚本？

- 每个打印机对应一个**写死配置**的 PS1，不再用 C# 模板生成
- PS1 内的 `printui` 参数、引号、执行策略等保持与人工测试通过的脚本完全一致
- C# 只负责"选对脚本并弹窗运行"，不干预 PS1 内部逻辑

### 为什么 PS1 要保存到 `C:\Drivers\{brand}\`？

- 与驱动文件放在一起，方便排查
- `printui` 安装后需要引用同目录下的 UI 插件 DLL，删除会导致打印首选项异常

## 开发环境

- **IDE**: JetBrains Rider
- **SDK**: .NET 10 SDK
- **开发 OS**: macOS
- **运行 OS**: Windows 10/11

## 发布命令

**发布给别人的 EXE 必须用 Release。**

```bash
dotnet publish -c Release
```

发布后 EXE 位置：

```
bin/Release/net10.0-windows/win-x64/publish/PrinterConnectTool.exe
```

## 如何新增职场

编辑 `Config/workplaces.json`，在 `workplaces` 数组中添加：

```json
{
  "name": "新职场名称",
  "gatewayIp": "10.x.x.1",
  "printers": []
}
```

## 如何新增打印机

### 情况 1：复用已有驱动品牌

1. 复制一个 `Scripts/` 下已有的 PS1，改名并修改配置区：
   - `$PrinterIP` — 打印机 IP
   - `$PrinterName` — 打印机显示名称
   - `$DriverName` — 与 INF 中驱动名完全一致
   - `$DriverFolder` — `C:\Drivers\{brand}\`
   - `$PortName` — `IP_{PrinterIP}`
2. 在 `Config/workplaces.json` 对应职场的 `printers` 数组中添加：

```json
{
  "name": "打印机显示名称",
  "ip": "10.x.x.x",
  "driverName": "驱动名称（需与 INF 中一致）",
  "brand": "TOSHIBA",
  "script": "install_xxx.ps1",
  "portNumber": 9100
}
```

3. 在 `PrinterConnectTool.csproj` 的 Scripts 段确保自动包含所有 `.ps1`（已配置）
4. 重新发布

### 情况 2：使用全新驱动品牌

1. 在 Windows 上准备驱动文件夹，例如 `NewBrand/`，里面包含 `.inf`、`.dll`、`.cat` 等文件
2. 在 Windows 上右键 `NewBrand` 文件夹 → 发送到 → 压缩文件夹，得到 `NewBrand.zip`
3. 将 `NewBrand.zip` 放入 `Drivers/NewBrand.zip`
4. 为该品牌下的每台打印机创建 PS1 脚本，放到 `Scripts/`
5. 修改 `PrinterConnectTool.csproj`，在驱动 ZIP 段添加：

```xml
<EmbeddedResource Include="Drivers\NewBrand.zip"/>
```

6. 修改 `Config/workplaces.json`，添加职场和打印机，printer 的 `brand` 填 `NewBrand`
7. 重新发布

## 如何更新驱动

1. 在 Windows 上替换原始驱动文件夹内容
2. 在 Windows 上重新压缩该文件夹（保留原始文件夹名）
3. 用新的 ZIP 替换项目中的 `Drivers/{brand}.zip`
4. 重新发布

## 注意事项

- PS1 源文件**必须带 UTF-8 BOM**，确保 PowerShell 5.1 正确识别中文
- ZIP 包**必须在 Windows 上压缩**，避免跨平台文件差异
- `workplaces.json` 支持外部覆盖：发布后的 EXE 同目录下放置 `workplaces.json` 会优先读取
- 发布时 Rider 的"配置"下拉框如果为空，直接用命令行 `dotnet publish -c Release`

## 常见问题

### Q: 为什么 C# 不直接安装打印机，而是弹出一个 PowerShell 窗口？
A: 因为 PowerShell 脚本中的 `printui` 参数引号极易被 C# 字符串转义破坏。改为弹窗运行原 PS1 后，引号环境就和人工测试通过的环境完全一致，避免驱动安装后无法打印的问题。

### Q: 新增打印机后 PS1 命名有什么规范？
A: 没有强制规范，但建议用 `install_{职场}_{位置}.ps1`，并在 `workplaces.json` 的 `script` 字段中对应填写完整文件名。

### Q: 为什么 PS1 里的路径要用 `C:\Drivers\{brand}\` 写死？
A: 这是工具约定的驱动解压路径。C# 保证把 ZIP 解压到这里，PS1 直接读取即可，无需接收参数。
