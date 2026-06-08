$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'DesktopIconManager.csproj'
$scriptPath = Join-Path $PSScriptRoot 'DesktopIconManager.iss'
$publishDir = if ($env:DICM_PUBLISH_DIR) {
    Resolve-Path -LiteralPath $env:DICM_PUBLISH_DIR
} else {
    Join-Path $repoRoot 'publish\win-x64-manager-ui-fluent'
}
$exePath = Join-Path $publishDir 'DesktopIconManager.exe'
$distDir = Join-Path $repoRoot 'dist'

function Get-ProjectVersion {
    [xml]$project = Get-Content -LiteralPath $projectPath
    $version = $project.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = '1.0.0'
    }

    return $version
}

function Find-InnoCompiler {
    $isccCandidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 5\ISCC.exe',
        'C:\Program Files\Inno Setup 5\ISCC.exe'
    )

    $iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $iscc) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($command) {
            $iscc = $command.Source
        }
    }

    if (-not $iscc) {
        throw '未找到 Inno Setup 编译器 ISCC.exe。请先安装 Inno Setup 6，然后重新运行 installer\build-installer.ps1。'
    }

    return $iscc
}

function Find-SignTool {
    if ($env:DICM_SIGNTOOL_PATH -and (Test-Path -LiteralPath $env:DICM_SIGNTOOL_PATH)) {
        return $env:DICM_SIGNTOOL_PATH
    }

    $candidates = @(
        'C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe',
        'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe',
        'C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe',
        'C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe'
    )

    return $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

function Invoke-OptionalSign {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not $env:DICM_SIGN_CERT_PATH) {
        Write-Host "未配置签名证书，跳过签名：$Path"
        return
    }

    if (-not (Test-Path -LiteralPath $env:DICM_SIGN_CERT_PATH)) {
        Write-Host "签名证书不存在，跳过签名：$env:DICM_SIGN_CERT_PATH"
        return
    }

    $signtool = Find-SignTool
    if (-not $signtool) {
        Write-Host '未找到 signtool.exe，跳过签名。'
        return
    }

    $timestamp = if ($env:DICM_SIGN_TIMESTAMP_URL) { $env:DICM_SIGN_TIMESTAMP_URL } else { 'http://timestamp.digicert.com' }
    $args = @('sign', '/fd', 'SHA256', '/f', $env:DICM_SIGN_CERT_PATH, '/tr', $timestamp, '/td', 'SHA256')
    if ($env:DICM_SIGN_CERT_PASSWORD) {
        $args += @('/p', $env:DICM_SIGN_CERT_PASSWORD)
    }
    $args += $Path

    & $signtool @args
    if ($LASTEXITCODE -ne 0) {
        throw "签名失败：$Path"
    }
}

$version = Get-ProjectVersion
$iscc = Find-InnoCompiler

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "未找到发布版 exe：$exePath。请先运行 dotnet publish。"
}

New-Item -ItemType Directory -Force -Path $distDir | Out-Null

Invoke-OptionalSign -Path $exePath

& $iscc "/DMyAppVersion=$version" "/DMyAppSourceDir=$publishDir" $scriptPath
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 编译失败，退出代码：$LASTEXITCODE"
}

$installerPath = Join-Path $distDir "DesktopIconManagerSetup-$version-x64.exe"
if (Test-Path -LiteralPath $installerPath) {
    Invoke-OptionalSign -Path $installerPath
}

$portablePath = Join-Path $distDir "DesktopIconManagerPortable-$version-x64.zip"
if (Test-Path -LiteralPath $portablePath) {
    Remove-Item -LiteralPath $portablePath -Force
}
$portableNames = @(
    'DesktopIconManager.exe',
    '使用说明.md',
    'README.md',
    'CHANGELOG.md',
    'PRIVACY.md',
    'UNINSTALL.md',
    'update-manifest.example.json'
)
$portableItems = foreach ($name in $portableNames) {
    $itemPath = Join-Path $publishDir $name
    if (Test-Path -LiteralPath $itemPath) {
        Get-Item -LiteralPath $itemPath
    }
}
Compress-Archive -LiteralPath $portableItems.FullName -DestinationPath $portablePath -Force

Write-Host "版本号：$version"
Write-Host "安装包：$installerPath"
Write-Host "便携包：$portablePath"
