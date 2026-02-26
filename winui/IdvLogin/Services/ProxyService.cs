// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

namespace IdvLogin.Services;

/// <summary>
/// 代理服务存根
///
/// 原 Python 版通过修改 hosts 文件 + 占用 80/443 端口实现 MITM 代理，
/// 此处留空——用户将通过外部工具（如 Proxifier、tun2socks、Fiddler 等）
/// 对指定进程进行透明代理，无需本程序干预。
///
/// 若需要在 WinUI3 内内置代理能力，可在此实现：
///   - 调用 WinDivert / clrzmq 监听流量
///   - 或通过 Windows Filtering Platform API 注入规则
/// </summary>
public class ProxyService
{
    // 留空，由调用者在外部对目标进程设置代理
}
