#define MyAppName "桌面图标整理"
#ifndef MyAppVersion
#define MyAppVersion "1.1.5"
#endif
#define MyAppPublisher "Desktop Icon Manager"
#define MyAppExeName "DesktopIconManager.exe"
#ifndef MyAppSourceDir
#define MyAppSourceDir "..\publish\win-x64-manager-ui-fluent"
#endif

[Setup]
AppId={{7BDB9CC4-FE3D-4A9F-9503-A92F31B7A46C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppVerName={#MyAppName} {#MyAppVersion}
AppCopyright=Copyright 2026 Desktop Icon Manager
DefaultDirName={localappdata}\Programs\DesktopIconManager
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=DesktopIconManagerSetup-{#MyAppVersion}-x64
SetupIconFile=..\Assets\app-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UsePreviousAppDir=yes
CloseApplications=yes
RestartApplications=no

[LangOptions]
LanguageName=简体中文
LanguageID=$0804
LanguageCodePage=65001
DialogFontName=Microsoft YaHei UI
DialogFontSize=9
WelcomeFontName=Microsoft YaHei UI
WelcomeFontSize=14

[Messages]
SetupAppTitle=安装
SetupWindowTitle=安装 - %1
UninstallAppTitle=卸载
UninstallAppFullTitle=卸载 %1
InformationTitle=信息
ConfirmTitle=确认
ErrorTitle=错误
SetupLdrStartupMessage=即将安装 %1。是否继续？
LdrCannotCreateTemp=无法创建临时文件。安装已中止
LdrCannotExecTemp=无法执行临时目录中的文件。安装已中止
LastErrorMessage=%1.%n%n错误 %2：%3
SetupFileMissing=安装目录中缺少文件 %1。请修复问题或重新获取程序。
SetupFileCorrupt=安装文件已损坏。请重新获取程序。
SetupFileCorruptOrWrongVer=安装文件已损坏，或与此版本安装程序不兼容。请重新获取程序。
InvalidParameter=命令行参数无效：%n%n%1
SetupAlreadyRunning=安装程序已经在运行。
WindowsVersionNotSupported=此程序不支持当前 Windows 版本。
WindowsServicePackRequired=此程序需要 %1 Service Pack %2 或更高版本。
NotOnThisPlatform=此程序不能在 %1 上运行。
OnlyOnThisPlatform=此程序必须在 %1 上运行。
OnlyOnTheseArchitectures=此程序只能安装在以下处理器架构的 Windows 版本上：%n%n%1
WinVersionTooLowError=此程序需要 %1 版本 %2 或更高版本。
AdminPrivilegesRequired=安装此程序需要管理员权限。
SetupAppRunningError=安装程序检测到 %1 正在运行。%n%n请先关闭所有相关窗口，然后点击“确定”继续，或点击“取消”退出。
UninstallAppRunningError=卸载程序检测到 %1 正在运行。%n%n请先关闭所有相关窗口，然后点击“确定”继续，或点击“取消”退出。
ErrorCreatingDir=安装程序无法创建目录“%1”
ExitSetupTitle=退出安装
ExitSetupMessage=安装尚未完成。如果现在退出，程序将不会被安装。%n%n你可以稍后重新运行安装程序完成安装。%n%n确定退出安装吗？
AboutSetupMenuItem=关于安装程序(&A)...
AboutSetupTitle=关于安装程序
AboutSetupMessage=%1 版本 %2%n%3%n%n%1 主页：%n%4
ButtonBack=< 上一步(&B)
ButtonNext=下一步(&N) >
ButtonInstall=安装(&I)
ButtonOK=确定
ButtonCancel=取消
ButtonYes=是(&Y)
ButtonYesToAll=全部是(&A)
ButtonNo=否(&N)
ButtonNoToAll=全部否(&O)
ButtonFinish=完成(&F)
ButtonBrowse=浏览(&B)...
ButtonWizardBrowse=浏览(&R)...
ButtonNewFolder=新建文件夹(&M)
SelectLanguageTitle=选择安装语言
SelectLanguageLabel=请选择安装过程中使用的语言。
ClickNext=点击“下一步”继续，或点击“取消”退出安装。
BrowseDialogTitle=浏览文件夹
BrowseDialogLabel=在下面列表中选择文件夹，然后点击“确定”。
NewFolderName=新建文件夹
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=此向导将在你的电脑上安装 [name/ver]。%n%n建议继续前关闭其他正在运行的应用程序。
WizardSelectDir=选择安装位置
SelectDirDesc=[name] 应该安装到哪里？
SelectDirLabel3=安装程序将把 [name] 安装到以下文件夹。
SelectDirBrowseLabel=点击“下一步”继续。如需选择其他文件夹，请点击“浏览”。
DiskSpaceGBLabel=至少需要 [gb] GB 可用磁盘空间。
DiskSpaceMBLabel=至少需要 [mb] MB 可用磁盘空间。
CannotInstallToNetworkDrive=安装程序不能安装到网络驱动器。
CannotInstallToUNCPath=安装程序不能安装到 UNC 路径。
InvalidPath=必须输入带驱动器盘符的完整路径，例如：%n%nC:\APP%n%n或 UNC 路径，例如：%n%n\\server\share
InvalidDrive=选择的驱动器或 UNC 共享不存在或不可访问。请选择其他位置。
DiskSpaceWarningTitle=磁盘空间不足
DiskSpaceWarning=安装程序至少需要 %1 KB 可用空间，但所选驱动器只有 %2 KB 可用。%n%n是否仍要继续？
DirExistsTitle=文件夹已存在
DirExists=文件夹：%n%n%1%n%n已经存在。是否仍安装到此文件夹？
DirDoesntExistTitle=文件夹不存在
DirDoesntExist=文件夹：%n%n%1%n%n不存在。是否创建该文件夹？
WizardSelectTasks=选择附加任务
SelectTasksDesc=需要执行哪些附加任务？
SelectTasksLabel2=请选择安装 [name] 时需要执行的附加任务，然后点击“下一步”。
WizardReady=准备安装
ReadyLabel1=安装程序已准备好在你的电脑上安装 [name]。
ReadyLabel2a=点击“安装”开始安装，或点击“上一步”检查或更改设置。
ReadyLabel2b=点击“安装”开始安装。
ReadyMemoDir=安装位置：
ReadyMemoGroup=开始菜单文件夹：
ReadyMemoTasks=附加任务：
WizardPreparing=正在准备安装
PreparingDesc=安装程序正在准备安装 [name]。
CannotContinue=安装程序无法继续。请点击“取消”退出。
ApplicationsFound=以下应用程序正在使用需要更新的文件。建议允许安装程序自动关闭这些应用程序。
CloseApplications=自动关闭应用程序(&A)
DontCloseApplications=不关闭应用程序(&D)
ErrorCloseApplications=安装程序无法自动关闭所有应用程序。建议继续前手动关闭相关应用程序。
WizardInstalling=正在安装
InstallingLabel=请稍候，安装程序正在安装 [name]。
FinishedHeadingLabel=正在完成 [name] 安装向导
FinishedLabelNoIcons=安装程序已完成 [name] 的安装。
FinishedLabel=安装程序已完成 [name] 的安装。你可以通过已创建的快捷方式启动应用。
ClickFinish=点击“完成”退出安装程序。
ShowReadmeCheck=是，我想查看 README 文件
SetupAborted=安装未完成。%n%n请修复问题后重新运行安装程序。
StatusExtractFiles=正在解压文件...
StatusCreateDirs=正在创建目录...
StatusCreateIcons=正在创建快捷方式...
StatusCreateIniEntries=正在创建 INI 项...
StatusCreateRegistryEntries=正在创建注册表项...
StatusRegisterFiles=正在注册文件...
StatusSavingUninstall=正在保存卸载信息...
StatusRunProgram=正在完成安装...
StatusRestartingApplications=正在重新启动应用程序...
UninstallAppFullTitle=卸载 %1
UninstallStatusLabel=正在从你的电脑中卸载 %1，请稍候。
UninstalledAll=%1 已成功从你的电脑中卸载。

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标："; Flags: checkedonce

[Files]
Source: "{#MyAppSourceDir}\DesktopIconManager.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\使用说明.md"; DestDir: "{app}"; DestName: "使用说明.md"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.md"; Flags: ignoreversion
Source: "..\CHANGELOG.md"; DestDir: "{app}"; DestName: "更新日志.md"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\PRIVACY.md"; DestDir: "{app}"; DestName: "隐私说明.md"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\UNINSTALL.md"; DestDir: "{app}"; DestName: "卸载说明.md"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\使用说明"; Filename: "{app}\使用说明.md"
Name: "{group}\更新日志"; Filename: "{app}\更新日志.md"
Name: "{group}\隐私说明"; Filename: "{app}\隐私说明.md"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{userstartup}\桌面图标整理-自动整理.lnk"
