using System;

namespace Flow.Launcher.Plugin.PortViewer.Util;

public enum LoggerLevel
{
    DEBUG,
    INFO,
    WARN,
    ERROR,
    OFF
}

public class InnerLogger
{
    public static LoggerLevel Level { get; set; } = LoggerLevel.OFF;
    public static ILogger Logger { get; set; } = new NoneLogger();

    public static void SetAsFlowLauncherLogger(IPublicAPI publicApi, LoggerLevel level = LoggerLevel.ERROR)
    {
        Level = level;
        Logger = new LoggerImpl(new FlowLauncherLogger(publicApi), Level);
    }

    public static void SetAsConsoleLogger(LoggerLevel level = LoggerLevel.ERROR)
    {
        Level = level;
        Logger = new LoggerImpl(new ConsoleLogger(), Level);
    }

    public static void ResetLogger()
    {
        Logger = new NoneLogger();
    }
}

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception ex = null);
}

internal class LoggerImpl : ILogger
{
    private readonly ILogger _logger;
    private readonly LoggerLevel _level;

    public LoggerImpl(ILogger logger, LoggerLevel level)
    {
        _logger = logger;
        _level = level;
    }

    public void Debug(string message)
    {
        if (_level <= LoggerLevel.DEBUG)
            _logger.Debug(message);
    }

    public void Info(string message)
    {
        if (_level <= LoggerLevel.INFO)
            _logger.Info(message);
    }

    public void Warn(string message)
    {
        if (_level <= LoggerLevel.WARN)
            _logger.Warn(message);
    }

    public void Error(string message, Exception ex = null)
    {
        if (_level <= LoggerLevel.ERROR)
            _logger.Error(message, ex);
    }
}

internal class ConsoleLogger : ILogger
{
    internal static readonly string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public void Debug(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [DEBUG] {message}");
    }

    public void Info(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [INFO] {message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [WARN] {message}");
    }

    public void Error(string message, Exception ex = null)
    {
        if (ex != null)
        {
            Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [ERROR] {message}. cause: {ex.Message}");
        }

        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [ERROR] {message}");
    }
}

internal class FlowLauncherLogger : ILogger
{
    private readonly IPublicAPI _publicApi;

    public FlowLauncherLogger(IPublicAPI publicApi)
    {
        _publicApi = publicApi;
    }

    public void Debug(string message)
    {
        _publicApi.LogDebug("PortViewer", message);
    }

    public void Info(string message)
    {
        _publicApi.LogInfo("PortViewer", message);
    }

    public void Warn(string message)
    {
        _publicApi.LogWarn("PortViewer", message);
    }

    public void Error(string message, Exception ex = null)
    {
        _publicApi.LogException("PortViewer", message, ex);
    }
}

internal class NoneLogger : ILogger
{
    public void Debug(string message)
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception ex = null)
    {
    }
}