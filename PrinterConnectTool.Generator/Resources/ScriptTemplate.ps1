#Requires -RunAsAdministrator

# ==================== 配置区 ====================
$PrinterIP    = '{PRINTER_IP}'
$PrinterName  = '{PRINTER_NAME}'
$DriverName   = '{DRIVER_NAME}'
$DriverFolder = '{DRIVER_FOLDER}'
$PortName     = '{PORT_NAME}'
# ==============================================

Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  IP 打印机安装工具' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

# 检查管理员权限
$currentUser = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host ''
    Write-Host '错误：请以管理员身份运行 PowerShell！' -ForegroundColor Red
    pause
    exit 1
}

# 自动找 INF 文件
if (-not (Test-Path $DriverFolder)) {
    Write-Host ''
    Write-Host "错误：找不到驱动文件夹: $DriverFolder" -ForegroundColor Red
    pause
    exit 1
}

$infFiles = Get-ChildItem -Path $DriverFolder -Filter '*.inf' -File
if ($infFiles.Count -eq 0) {
    Write-Host ''
    Write-Host '错误：找不到 INF 文件' -ForegroundColor Red
    pause
    exit 1
}
$InfPath = $infFiles[0].FullName
Write-Host ''
Write-Host "找到驱动: $($infFiles[0].Name)" -ForegroundColor Green

# ==================== 1. 清理同名打印机 ====================
Write-Host ''
Write-Host '[1/5] 检查并清理旧打印机...' -ForegroundColor Cyan

$existingPrinter = Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue
if ($existingPrinter) {
    Write-Host '    发现同名打印机，正在删除...' -ForegroundColor Yellow
    Remove-Printer -Name $PrinterName -Confirm:$false
    Start-Sleep -Seconds 1
    Write-Host '    已删除'
} else {
    Write-Host '    无同名打印机'
}

# ==================== 2. 清理并重建端口 ====================
Write-Host ''
Write-Host '[2/5] 检查并重建端口...' -ForegroundColor Cyan

$port = Get-WmiObject -Class Win32_TCPIPPrinterPort -Filter "Name='$PortName'" -ErrorAction SilentlyContinue
if ($port) {
    Write-Host '    发现旧端口，检查占用情况...' -ForegroundColor Yellow
    
    # 先删除所有占用此端口的打印机（避免端口删不掉）
    $printersUsingPort = Get-WmiObject -Class Win32_Printer -Filter "PortName='$PortName'" -ErrorAction SilentlyContinue
    if ($printersUsingPort) {
        foreach ($p in $printersUsingPort) {
            Write-Host "    删除占用端口的打印机: $($p.Name)" -ForegroundColor Yellow
            Remove-Printer -Name $p.Name -Confirm:$false -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }
    }
    
    $port.Delete()
    Start-Sleep -Seconds 1
    Write-Host '    旧端口已删除'
}

# 创建新端口
$newPort = ([wmiclass]'Win32_TCPIPPrinterPort').CreateInstance()
$newPort.Name = $PortName
$newPort.Protocol = 1
$newPort.HostAddress = $PrinterIP
$newPort.PortNumber = 9100
$newPort.Put() | Out-Null
Write-Host '    新端口创建成功'

# ==================== 3. 驱动处理（有就用，没有就装）====================
Write-Host ''
Write-Host '[3/5] 检查驱动...' -ForegroundColor Cyan

$existingDriver = Get-PrinterDriver -Name $DriverName -ErrorAction SilentlyContinue
if ($existingDriver) {
    Write-Host '    驱动已存在，直接使用现有版本' -ForegroundColor Green
} else {
    Write-Host '    正在注册新驱动...' -ForegroundColor Cyan
    $result = & pnputil.exe /add-driver $InfPath /install 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host '    新驱动注册成功' -ForegroundColor Green
    } else {
        Write-Host "    驱动注册提示: $result" -ForegroundColor Yellow
        Write-Host '    尝试继续安装...' -ForegroundColor Yellow
    }
}

# ==================== 4. 安装打印机 ====================
Write-Host ''
Write-Host '[4/5] 安装打印机...' -ForegroundColor Cyan

$printuiArgs = @(
    'printui.dll,PrintUIEntry',
    '/if',
    '/b', ('"' + $PrinterName + '"'),
    '/f', ('"' + $InfPath + '"'),
    '/r', ('"' + $PortName + '"'),
    '/m', ('"' + $DriverName + '"')
)

Start-Process -FilePath 'rundll32.exe' -ArgumentList $printuiArgs -Wait -WindowStyle Hidden
Start-Sleep -Seconds 3

$installed = Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue
if (-not $installed) {
    Write-Host ''
    Write-Host '安装失败！请检查 DriverName 是否与 INF 中完全一致。' -ForegroundColor Red
    Write-Host "你填的是: $DriverName" -ForegroundColor Yellow
    pause
    exit 1
}
Write-Host "    打印机安装成功: $PrinterName"

# ==================== 5. 关闭共享 ====================
Write-Host ''
Write-Host '[5/5] 关闭打印机共享...' -ForegroundColor Cyan

$wmiPrinter = Get-WmiObject -Class Win32_Printer -Filter "Name='$PrinterName'"
if ($wmiPrinter) {
    $changed = $false
    if ($wmiPrinter.Shared) { $wmiPrinter.Shared = $false; $changed = $true; Write-Host '    已取消共享标记' }
    if ($wmiPrinter.ShareName) { $wmiPrinter.ShareName = $null; $changed = $true; Write-Host '    已清空共享名' }
    if ($wmiPrinter.Published) { $wmiPrinter.Published = $false; $changed = $true; Write-Host '    已取消目录发布' }
    if ($changed) { $wmiPrinter.Put() | Out-Null } else { Write-Host '    该打印机未共享' }
}

# ==================== 完成 ====================
Write-Host ''
Write-Host '========================================' -ForegroundColor Green
Write-Host '  安装完成！' -ForegroundColor Green
Write-Host "  打印机: $PrinterName" -ForegroundColor Green
Write-Host "  IP地址: $PrinterIP" -ForegroundColor Green
Write-Host '  共享状态: 已关闭' -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Green

Write-Host ''
Write-Host '提示：如果内网其他电脑仍能通过网络发现你的电脑，' -ForegroundColor Yellow
Write-Host '请手动到 设置 - 网络和 Internet - 高级共享设置' -ForegroundColor Yellow
Write-Host '关闭 文件和打印机共享。' -ForegroundColor Yellow

pause
