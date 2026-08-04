# 打印机连接生成器

用于为各地区 IT 快速生成独立的「打印机自助连接工具」单文件 EXE。

## 运行要求

- Windows 10/11
- 无需 .NET SDK，生成器自身是自包含单文件 EXE
- 需要 Windows 管理员权限才能安装打印机（生成的 EXE 会请求 UAC）

## 快速开始

1. 打开「打印机连接生成器.exe」。
2. 添加驱动包：点击「+ 添加驱动包」，选择 Windows 上压缩好的 `{Brand}.zip`，填写 Brand 和默认驱动名。
3. 添加职场：填写职场名称和用于探测的网关 IP。
4. 添加打印机：填写名称、IP、选择所属职场和驱动包。
5. 点击「生成 EXE」，保存到任意位置。
6. 将生成的 EXE 分发给员工使用。

## 驱动 ZIP 准备

- 在 Windows 上把原始驱动文件夹压缩为 `{Brand}.zip`。
- ZIP 内部应包含 `{Brand}` 文件夹，INF 文件在 `{Brand}` 根目录下。
- 示例：`TOSHIBA.zip` 解压后得到 `TOSHIBA/eSf6u.inf`。

## 多职场/多打印机

- 同一驱动包可以被多个打印机复用。
- 每个打印机只能属于一个职场。
- 生成器会为每台打印机自动生成一个 PS1 安装脚本。

## 保存项目

建议点击「保存项目」把当前配置保存为 `.json` 文件，方便以后修改。

## 常见问题

### 生成器提示找不到壳资源

请确保生成器 Resources 目录下有 `Shell.exe`，或重新编译壳项目。

### 生成的 EXE 无法运行

- 确认驱动 ZIP 在 Windows 上压缩。
- 确认 `DriverName` 与 INF 文件中的驱动名完全一致。
- 确认目标电脑已连接对应职场的内网。

## 开发说明

- 项目代码名：`PrinterConnectTool.Generator`
- 壳项目：`PrinterConnectTool.Shell`
- 修改壳或原 `PrinterConnectTool.Core` 后，需重新编译并复制 `Shell.exe` 到生成器 Resources 目录，再重新发布生成器。
