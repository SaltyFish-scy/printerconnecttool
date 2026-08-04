# 驱动文件目录

将各品牌打印机的驱动文件放入对应文件夹。

## 目录结构

```
Drivers/
├── TOSHIBA/          ← 东芝打印机驱动（.inf, .dll, .cat 等）
├── ADC225/           ← 震旦(Aurora)打印机驱动（.INF, .dll, .cat 等）
└── (新品牌)/          ← 后续扩展时新建文件夹
```

## 使用方法

1. 把驱动文件（INF、DLL、CAT 等全部文件）直接放入对应文件夹
2. 不需要创建子目录，直接平铺即可
3. 程序运行时会自动找到第一个 `.inf`（或 `.INF`）文件作为驱动入口

## 新增品牌

1. 在此目录下新建文件夹（如 `HP`）
2. 放入驱动文件
3. 打开 `PrinterConnectTool.csproj`，在 `<ItemGroup>` 中添加一行：
   ```xml
   <EmbeddedResource Include="Drivers\HP\**\*.*" />
   ```
4. 在 `workplaces.json` 中配置打印机时，`brand` 字段填文件夹名（如 `"brand": "HP"`）
