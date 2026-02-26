// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Diagnostics;
using System.Security.Principal;

namespace IdvLogin.Services;

/// <summary>
/// 管理员权限工具（对应 Python 版 main.py 中的 IsUserAnAdmin / ShellExecuteW 逻辑）
/// </summary>
public static class AdminHelper
{
    /// <summary>判断当前进程是否以管理员身份运行</summary>
    public static bool IsAdmin()
    {
        using var identity  = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>以管理员身份重新启动当前可执行文件</summary>
    public static void RelaunchAsAdmin()
    {
        var exe  = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "";
        var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
        var psi  = new ProcessStartInfo
        {
            FileName        = exe,
            Arguments       = args,
            UseShellExecute = true,
            Verb            = "runas",
        };
        try { Process.Start(psi); }
        catch { /* 用户拒绝 UAC 弹窗 */ }
    }
}
