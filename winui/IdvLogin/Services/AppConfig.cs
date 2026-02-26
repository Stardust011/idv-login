// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdvLogin.Services;

/// <summary>
/// 应用配置持久化服务（对应 Python 版 envmgr.genv）
/// 配置存储在 %LOCALAPPDATA%\IdvLogin\config.json
/// </summary>
public class AppConfig
{
    private static readonly string WorkDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IdvLogin");

    private static readonly string ConfigFile = Path.Combine(WorkDir, "config.json");

    private readonly Dictionary<string, JsonNode?> _memory = new();
    private JsonObject _disk = new();

    // 常用路径常量
    public string WorkDirectory    => WorkDir;
    public string FakeDeviceFile   => Path.Combine(WorkDir, "fakeDevice.json");
    public string ChannelRecord    => Path.Combine(WorkDir, "channels.json");
    public string CaCertFile       => Path.Combine(WorkDir, "root_ca.pem");
    /// <summary>CA 私钥（仅供本程序签发域名证书使用，不导出）</summary>
    public string CaKeyFile        => Path.Combine(WorkDir, "root_ca_key.pem");
    public string WebCertFile      => Path.Combine(WorkDir, "domain_cert.pem");
    public string WebKeyFile       => Path.Combine(WorkDir, "domain_key.pem");
    public string CloudResCacheFile => Path.Combine(WorkDir, "cache.json");
    public string ProfileDirectory => Path.Combine(WorkDir, "profile");

    public AppConfig()
    {
        Directory.CreateDirectory(WorkDir);
        LoadDisk();
    }

    private void LoadDisk()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var text = File.ReadAllText(ConfigFile);
                _disk = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
            }
        }
        catch { _disk = new JsonObject(); }
    }

    /// <summary>设置内存值，可选同时持久化到磁盘</summary>
    public void Set<T>(string key, T value, bool persist = false)
    {
        var node = JsonSerializer.SerializeToNode(value);
        _memory[key] = node;
        if (persist)
        {
            _disk[key] = node?.DeepClone();
            Save();
        }
    }

    /// <summary>读取值，优先内存，其次磁盘文件</summary>
    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_memory.TryGetValue(key, out var memNode) && memNode is not null)
        {
            try { return memNode.Deserialize<T>(); }
            catch { }
        }
        if (_disk.TryGetPropertyValue(key, out var diskNode) && diskNode is not null)
        {
            try { return diskNode.Deserialize<T>(); }
            catch { }
        }
        return defaultValue;
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(ConfigFile, _disk.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
