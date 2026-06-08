using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Text.Json;

namespace DesktopIconManager;

public static class UpdateChecker
{
    private const string OfficialManifestUrl = "https://sorrowgod.github.io/desktop-icon-manager/update.json";
    private const string GitHubRawManifestUrl = "https://raw.githubusercontent.com/SorrowGod/desktop-icon-manager/main/update.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static AppVersionInfo GetCurrentVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = assembly.GetName().Version?.ToString(3) ?? "1.1.0";
        }

        return new AppVersionInfo
        {
            Version = version,
            BuildTime = GetBuildTime(Environment.ProcessPath ?? AppContext.BaseDirectory),
            ReleaseChannel = "官网安装包",
            IsSigned = false,
        };
    }

    public static async Task<UpdateCheckResult> CheckAsync(string manifestUrl, CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersionInfo().Version;
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            return new UpdateCheckResult
            {
                CurrentVersion = current,
                IsConfigured = false,
                Message = "还没有配置官网更新清单地址。正式发布时把官网 update.json 地址填到设置里即可。",
            };
        }

        var sources = BuildCandidateSources(manifestUrl);
        var attemptedSources = new List<string>();
        var attemptErrors = new List<string>();
        string? sourceUsed = null;
        string? json = null;

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attemptedSources.Add(source);
            try
            {
                json = await ReadManifestTextAsync(source, cancellationToken).ConfigureAwait(false);
                sourceUsed = source;
                break;
            }
            catch (Exception ex) when (IsRecoverableAttemptFailure(ex))
            {
                attemptErrors.Add($"{source}\r\n{BuildExceptionSummary(ex)}");
            }
        }

        if (json is null || sourceUsed is null)
        {
            throw new UpdateCheckException(
                BuildFailureMessage(attemptedSources),
                attemptedSources,
                attemptErrors);
        }

        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("更新清单格式不正确。");

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("更新清单缺少 version 字段。");
        }

        var comparison = CompareVersions(current, manifest.Version);
        return new UpdateCheckResult
        {
            IsConfigured = true,
            CurrentVersion = current,
            Manifest = manifest,
            ManifestSourceUrl = sourceUsed,
            AttemptedSources = attemptedSources,
            AttemptErrors = attemptErrors,
            HasUpdate = comparison < 0,
            IsLatest = comparison >= 0,
            Message = comparison < 0
                ? $"发现新版本 {manifest.Version}。"
                : $"当前已是最新版本 {current}。",
        };
    }

    public static string BuildUserFriendlyFailureMessage(Exception exception, string? manifestUrl = null)
    {
        if (exception is UpdateCheckException updateException)
        {
            return updateException.Message;
        }

        if (ContainsSslError(exception))
        {
            return
                "更新清单地址可以正常公开访问，但这台电脑没有通过 HTTPS 安全连接验证，所以软件还没读到 update.json。\r\n\r\n" +
                "常见原因：系统日期时间不正确、Windows 根证书过旧、网络代理或杀毒软件拦截 HTTPS、校园/公司网络限制 GitHub、TLS 设置异常。\r\n\r\n" +
                "你可以先在浏览器打开更新清单地址确认网络，再尝试同步系统时间、切换网络或更新 Windows。";
        }

        if (!string.IsNullOrWhiteSpace(manifestUrl))
        {
            return $"无法读取更新清单：{manifestUrl}\r\n\r\n{exception.Message}";
        }

        return exception.Message;
    }

    public static string BuildDiagnosticDetails(Exception exception, string? manifestUrl = null)
    {
        var lines = new List<string>
        {
            "检查更新诊断信息",
            $"当前版本：{GetCurrentVersionInfo().Version}",
            $"配置地址：{manifestUrl ?? string.Empty}",
        };

        if (exception is UpdateCheckException updateException)
        {
            lines.Add("尝试过的地址：");
            lines.AddRange(updateException.AttemptedSources.Select(source => $"- {source}"));
            if (updateException.AttemptErrors.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("每个地址的错误：");
                lines.AddRange(updateException.AttemptErrors);
            }
        }

        lines.Add(string.Empty);
        lines.Add("异常链：");
        lines.Add(BuildExceptionSummary(exception));
        return string.Join("\r\n", lines);
    }

    private static List<string> BuildCandidateSources(string manifestUrl)
    {
        var sources = new List<string>();
        AddSource(sources, manifestUrl.Trim());

        if (ShouldAddOfficialFallback(manifestUrl))
        {
            AddSource(sources, OfficialManifestUrl);
        }

        if (ShouldAddGitHubRawFallback(manifestUrl))
        {
            AddSource(sources, GitHubRawManifestUrl);
        }

        return sources;
    }

    private static void AddSource(List<string> sources, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        if (sources.Any(existing => existing.Equals(source, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        sources.Add(source);
    }

    private static bool ShouldAddOfficialFallback(string manifestUrl)
    {
        return manifestUrl.Contains("sorrowgod.github.io", StringComparison.OrdinalIgnoreCase) ||
               manifestUrl.Contains("desktop-icon-manager", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAddGitHubRawFallback(string manifestUrl)
    {
        return manifestUrl.Contains("sorrowgod.github.io", StringComparison.OrdinalIgnoreCase) ||
               manifestUrl.Contains("github.com/SorrowGod/desktop-icon-manager", StringComparison.OrdinalIgnoreCase) ||
               manifestUrl.Contains("raw.githubusercontent.com/SorrowGod/desktop-icon-manager", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadManifestTextAsync(string source, CancellationToken cancellationToken)
    {
        if (File.Exists(source))
        {
            return await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("更新清单地址不是有效的网址或文件路径。");
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("更新清单地址只支持 http、https 或本地文件路径。");
        }

        using var handler = new SocketsHttpHandler
        {
            SslOptions =
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            },
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        using var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsRecoverableAttemptFailure(Exception exception)
    {
        return exception is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException or JsonException;
    }

    private static string BuildFailureMessage(IReadOnlyList<string> attemptedSources)
    {
        var sourceText = attemptedSources.Count == 0
            ? "未生成可用的更新清单地址。"
            : string.Join("\r\n", attemptedSources.Select(source => $"• {source}"));

        return
            "检查更新失败：软件无法读取更新清单。\r\n\r\n" +
            "已尝试这些地址：\r\n" +
            sourceText +
            "\r\n\r\n如果浏览器能打开这些地址，但软件仍失败，通常是这台电脑的 HTTPS/证书/TLS 环境或网络代理拦截导致。请检查系统日期时间、Windows 更新、根证书、代理/杀毒软件 HTTPS 扫描，或换一个网络再试。";
    }

    private static bool ContainsSslError(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AuthenticationException)
            {
                return true;
            }

            if (current.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildExceptionSummary(Exception exception)
    {
        var lines = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            lines.Add($"{current.GetType().FullName}: {current.Message}");
        }

        return string.Join("\r\n", lines);
    }

    private static int CompareVersions(string current, string latest)
    {
        if (Version.TryParse(TrimVersion(current), out var currentVersion) &&
            Version.TryParse(TrimVersion(latest), out var latestVersion))
        {
            return currentVersion.CompareTo(latestVersion);
        }

        return string.Compare(current, latest, StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimVersion(string version)
    {
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }

    private static DateTime GetBuildTime(string location)
    {
        try
        {
            return File.Exists(location) ? File.GetLastWriteTime(location) : DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }
}

public sealed class UpdateCheckException : Exception
{
    public UpdateCheckException(
        string message,
        IReadOnlyList<string> attemptedSources,
        IReadOnlyList<string> attemptErrors)
        : base(message)
    {
        AttemptedSources = attemptedSources.ToList();
        AttemptErrors = attemptErrors.ToList();
    }

    public List<string> AttemptedSources { get; }

    public List<string> AttemptErrors { get; }

    public override string ToString()
    {
        var details = new List<string> { base.ToString() };
        if (AttemptedSources.Count > 0)
        {
            details.Add("Attempted sources:");
            details.AddRange(AttemptedSources);
        }

        if (AttemptErrors.Count > 0)
        {
            details.Add("Attempt errors:");
            details.AddRange(AttemptErrors);
        }

        return string.Join(Environment.NewLine, details);
    }
}
