namespace DesktopIconManager;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, eventArgs) => HandleUiException(eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                AppLogger.Log("发生未处理异常。", exception);
            }
        };

        if (args.Any(arg => string.Equals(arg, "--auto-arrange", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = AutoArrangeRunner.Run(GetArgumentValue(args, "--profile"));
            return;
        }

        Application.Run(new Form1());

        static void HandleUiException(Exception exception)
        {
            AppLogger.Log("界面操作失败。", exception);
            AppDialog.ShowError(null, "桌面管理器", $"操作失败：{exception.Message}", exception);
        }

        static string? GetArgumentValue(string[] arguments, string name)
        {
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
