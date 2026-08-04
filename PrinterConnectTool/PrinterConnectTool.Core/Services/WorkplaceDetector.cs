using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Services;

/// <summary>
///     职场并行探测：同时 Ping 所有职场网关，命中即返回
/// </summary>
[SupportedOSPlatform("windows")]
public class WorkplaceDetector
{
    private readonly AppConfig _config;

    public WorkplaceDetector(AppConfig config)
    {
        _config = config;
    }

    /// <summary>
    ///     并行探测所有职场，返回命中的职场；全部不通返回 null
    /// </summary>
    public async Task<WorkplaceConfig?> DetectAsync()
    {
        var timeout = _config.Settings.PingTimeoutMs;
        var overallTimeout = _config.Settings.OverallTimeoutMs;

        // 为每个职场创建 Ping 任务
        var pingTasks = _config.Workplaces.Select(async wp =>
        {
            var success = await PingHostAsync(wp.GatewayIp, timeout);
            return (Workplace: wp, Success: success);
        }).ToList();

        // 整体超时兜底
        var overallTask = Task.Delay(overallTimeout + 1000);

        // 逐个完成时检查，第一个成功的就返回
        while (pingTasks.Count > 0)
        {
            var completed = await Task.WhenAny(pingTasks);
            pingTasks.Remove(completed);

            var result = await completed;
            if (result.Success) return result.Workplace;

            // 如果整体超时已到，不再等待剩余任务
            if (overallTask.IsCompleted) break;
        }

        return null;
    }

    /// <summary>
    ///     探测外网连通性（www.sogo.com）
    /// </summary>
    /// <returns>true=外网通，false=外网不通</returns>
    public async Task<bool> CheckInternetAsync()
    {
        return await PingHostAsync("www.sogo.com", 3000);
    }

    /// <summary>
    ///     Ping 指定主机
    /// </summary>
    private static async Task<bool> PingHostAsync(string host, int timeoutMs)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, timeoutMs);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}