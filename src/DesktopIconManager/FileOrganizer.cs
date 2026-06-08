namespace DesktopIconManager;

public static class FileOrganizer
{
    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".rtf",
        ".md",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".pdf",
        ".csv",
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp",
        ".svg",
        ".ico",
    };

    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".flac",
        ".aac",
        ".mp4",
        ".mov",
        ".avi",
        ".mkv",
        ".wmv",
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip",
        ".rar",
        ".7z",
        ".tar",
        ".gz",
    };

    private static readonly HashSet<string> InstallerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe",
        ".msi",
        ".msix",
        ".appx",
        ".iso",
    };

    public static List<FileOrganizeSuggestion> GenerateSuggestions(
        IReadOnlyList<DesktopIconInfo> icons,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(icons);
        ArgumentNullException.ThrowIfNull(settings);
        AppSettingsStore.Normalize(settings);

        var desktop = GetDesktopDirectory();
        var targetDirectories = GetTargetDirectories(settings);
        var targetDirectorySet = targetDirectories.Values
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suggestions = new List<FileOrganizeSuggestion>();
        foreach (var file in EnumerateDesktopFiles())
        {
            if (ShouldSkipFile(file, targetDirectorySet))
            {
                continue;
            }

            var extension = file.Extension;
            var targetKind = GetTargetKind(extension);
            if (targetKind == FileOrganizeTargetKind.Other)
            {
                continue;
            }

            var targetDirectory = targetDirectories[targetKind];
            var targetPath = Path.Combine(targetDirectory, file.Name);
            suggestions.Add(new FileOrganizeSuggestion
            {
                FileName = file.Name,
                OriginalPath = file.FullName,
                Category = ToCategory(targetKind),
                TargetKind = targetKind,
                SuggestedTargetPath = targetPath,
                SizeBytes = SafeGetLength(file),
                LastModifiedAt = SafeGetLastWriteTime(file),
                DefaultSelected = true,
                Reason = targetKind switch
                {
                    FileOrganizeTargetKind.Document => "文档文件，建议收纳到桌面\\文档",
                    FileOrganizeTargetKind.Image => "图片文件，建议收纳到桌面\\图片",
                    FileOrganizeTargetKind.Media => "视频或音频文件，建议收纳到桌面\\视频音频",
                    FileOrganizeTargetKind.ArchiveOrInstaller => "压缩包或安装包，建议收纳到桌面\\压缩包安装包",
                    _ => "建议收纳",
                },
            });
        }

        return suggestions
            .OrderByDescending(item => item.DefaultSelected)
            .ThenBy(item => item.TargetKind)
            .ThenBy(item => item.FileName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static FileOrganizeOperation Execute(
        IEnumerable<FileOrganizeSuggestion> suggestions,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        ArgumentNullException.ThrowIfNull(settings);

        var operation = new FileOrganizeOperation
        {
            CreatedAt = DateTime.Now,
            IsUndoOperation = false,
        };

        foreach (var suggestion in suggestions)
        {
            operation.Results.Add(MoveOne(suggestion.OriginalPath, suggestion.SuggestedTargetPath));
        }

        FileOrganizeOperationStore.Add(operation, settings.FileOrganizeUndoRetentionCount);
        return operation;
    }

    public static FileOrganizeOperation? UndoLatest(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var latest = FileOrganizeOperationStore.GetLatestUndoable();
        if (latest is null)
        {
            return null;
        }

        var undo = new FileOrganizeOperation
        {
            CreatedAt = DateTime.Now,
            IsUndoOperation = true,
        };

        foreach (var result in latest.Results.Where(result => result.Success))
        {
            undo.Results.Add(MoveBackOne(result.TargetPath, result.OriginalPath));
        }

        if (undo.Results.Any(result => result.Success))
        {
            FileOrganizeOperationStore.MarkUndone(latest.Id);
        }

        FileOrganizeOperationStore.Add(undo, settings.FileOrganizeUndoRetentionCount);
        return undo;
    }

    public static Dictionary<FileOrganizeTargetKind, string> GetTargetDirectories(AppSettings settings)
    {
        var desktop = GetDesktopDirectory();
        var names = settings.DesktopOrganizeFolderNames;
        return new Dictionary<FileOrganizeTargetKind, string>
        {
            [FileOrganizeTargetKind.Document] = Path.Combine(desktop, SanitizeFolderName(names.Documents, "文档")),
            [FileOrganizeTargetKind.Image] = Path.Combine(desktop, SanitizeFolderName(names.Images, "图片")),
            [FileOrganizeTargetKind.Media] = Path.Combine(desktop, SanitizeFolderName(names.Media, "视频音频")),
            [FileOrganizeTargetKind.ArchiveOrInstaller] = Path.Combine(desktop, SanitizeFolderName(names.ArchivesAndInstallers, "压缩包安装包")),
        };
    }

    public static string GetDesktopDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private static FileOrganizeMoveResult MoveOne(string sourcePath, string targetPath)
    {
        var result = new FileOrganizeMoveResult
        {
            FileName = Path.GetFileName(sourcePath),
            OriginalPath = sourcePath,
            TargetPath = targetPath,
        };

        try
        {
            if (!File.Exists(sourcePath))
            {
                result.Status = "跳过";
                result.ErrorMessage = "原文件不存在";
                return result;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                result.Status = "失败";
                result.ErrorMessage = "目标目录无效";
                return result;
            }

            Directory.CreateDirectory(targetDirectory);
            if (File.Exists(targetPath) || Directory.Exists(targetPath))
            {
                result.Status = "冲突";
                result.ErrorMessage = "目标位置已有同名项目，未覆盖";
                return result;
            }

            File.Move(sourcePath, targetPath);
            result.Success = true;
            result.Status = "已移动";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = "失败";
            result.ErrorMessage = ex.Message;
            AppLogger.Log($"文件收纳失败：{sourcePath}", ex);
            return result;
        }
    }

    private static FileOrganizeMoveResult MoveBackOne(string sourcePath, string targetPath)
    {
        var result = new FileOrganizeMoveResult
        {
            FileName = Path.GetFileName(sourcePath),
            OriginalPath = sourcePath,
            TargetPath = targetPath,
        };

        try
        {
            if (!File.Exists(sourcePath))
            {
                result.Status = "跳过";
                result.ErrorMessage = "目标位置文件已不存在";
                return result;
            }

            if (File.Exists(targetPath) || Directory.Exists(targetPath))
            {
                result.Status = "冲突";
                result.ErrorMessage = "原位置已有同名项目，未覆盖";
                return result;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Move(sourcePath, targetPath);
            result.Success = true;
            result.Status = "已撤销";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = "失败";
            result.ErrorMessage = ex.Message;
            AppLogger.Log($"撤销文件收纳失败：{sourcePath}", ex);
            return result;
        }
    }

    private static FileOrganizeTargetKind GetTargetKind(string extension)
    {
        if (DocumentExtensions.Contains(extension))
        {
            return FileOrganizeTargetKind.Document;
        }

        if (ImageExtensions.Contains(extension))
        {
            return FileOrganizeTargetKind.Image;
        }

        if (MediaExtensions.Contains(extension))
        {
            return FileOrganizeTargetKind.Media;
        }

        if (ArchiveExtensions.Contains(extension) || InstallerExtensions.Contains(extension))
        {
            return FileOrganizeTargetKind.ArchiveOrInstaller;
        }

        return FileOrganizeTargetKind.Other;
    }

    private static IconCategory ToCategory(FileOrganizeTargetKind targetKind) => targetKind switch
    {
        FileOrganizeTargetKind.Document => IconCategory.Document,
        FileOrganizeTargetKind.Image => IconCategory.Image,
        FileOrganizeTargetKind.Media => IconCategory.Media,
        FileOrganizeTargetKind.ArchiveOrInstaller => IconCategory.Archive,
        _ => IconCategory.Other,
    };

    private static IEnumerable<FileInfo> EnumerateDesktopFiles()
    {
        foreach (var directory in EnumerateDesktopDirectories())
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<FileInfo> files;
            try
            {
                files = new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"扫描桌面文件失败：{directory}", ex);
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateDesktopDirectories()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
    }

    private static bool ShouldSkipFile(FileInfo file, HashSet<string> targetDirectories)
    {
        if (string.Equals(file.Name, "desktop.ini", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (file.Name.StartsWith("~$", StringComparison.Ordinal) ||
            file.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (file.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ||
            file.Extension.Equals(".url", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (file.Attributes.HasFlag(FileAttributes.System))
        {
            return true;
        }

        if (targetDirectories.Contains(NormalizePath(file.FullName)))
        {
            return true;
        }

        return IsInsideAnyTargetDirectory(file.FullName, targetDirectories);
    }

    private static long SafeGetLength(FileInfo file)
    {
        try
        {
            file.Refresh();
            return file.Length;
        }
        catch
        {
            return 0;
        }
    }

    private static DateTime? SafeGetLastWriteTime(FileInfo file)
    {
        try
        {
            file.Refresh();
            return file.LastWriteTime;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsDirectChildOf(string path, string parentDirectory)
    {
        var parent = Directory.GetParent(path);
        return parent is not null &&
            string.Equals(NormalizePath(parent.FullName), NormalizePath(parentDirectory), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideAnyTargetDirectory(string path, HashSet<string> targetDirectories)
    {
        var directory = Directory.GetParent(path);
        while (directory is not null)
        {
            if (targetDirectories.Contains(NormalizePath(directory.FullName)))
            {
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    private static string SanitizeFolderName(string? value, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            text = text.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
