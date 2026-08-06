# 打印机自助连接工具

一个面向企业内网的打印机自助连接解决方案。

> 项目来源：Gitee-一条小咸鱼@salte-fish
>  @一条小咸鱼 
## 仓库说明

本仓库包含两部分：

| 目录 | 说明 |
|------|------|
| `PrinterConnectTool.Generator` | 生成器源码：可视化配置驱动包、职场、打印机，生成独立的单文件 EXE |
| `PrinterConnectTool` | 原打印机自助连接工具源码（Console + UI 双入口） |

## 下载

发布版本及编译好的生成器 EXE 请见右侧 **Release** 页面。

## 使用方式

细节我放在 打印机自助连接工具及打印机自助工具生成器使用说明.docx
了
### 一、IT 管理员使用生成器制作分发包



1. 下载 `打印机连接生成器.exe`，双击运行。
2. 在生成器中添加：
   - 驱动包：各品牌打印机驱动的 ZIP 文件
   - 职场：公司名称与对应网关 IP
   - 打印机：每台打印机的名称、IP、所属职场和驱动
3. 点击「生成 EXE」，保存得到 `PrinterConnectTool.Desktop.exe`。

（此处待补截图，标题：添加配置）

### 二、员工使用分发包连接打印机



1. 将 `PrinterConnectTool.Desktop.exe` 发送给员工。
2. 员工双击运行，程序会自动探测所在职场并列出可用打印机。
3. 点击对应打印机按钮，按提示完成安装。


## 环境要求

- Windows 10 / Windows 11
- 处于企业内网环境，可访问对应打印机 IP
- 运行程序需要管理员权限（安装打印机驱动需要）

## 项目结构

```
PrinterConnectTool.Generator/    生成器（可视化配置 + 打包）
PrinterConnectTool/              原工具源码（Console + Core + UI）
```

## 开源协议

本项目采用 [Apache License 2.0](LICENSE)。

复制分发修改项目及对应软件，请保留本项目作者署名。

## 支持一下
如果觉得本项目有用的话，求支持喝个水


<img src="%E6%B1%82%E6%94%AF%E6%8C%81.jpg" width="200" />
