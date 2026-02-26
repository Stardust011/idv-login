// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IdvLogin.Services;

/// <summary>
/// CA 证书生成与安装管理（对应 Python 版 certmgr.py）
/// 使用 .NET 内置的 System.Security.Cryptography 实现，无需第三方库
/// </summary>
public class CertService
{
    private readonly AppConfig _config;

    public CertService(AppConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 检查本地 CA 证书是否存在且在有效期内
    /// </summary>
    public bool IsCaCertValid()
    {
        var path = _config.CaCertFile;
        if (!File.Exists(path)) return false;
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(path);
            return cert.NotAfter > DateTime.UtcNow.AddDays(30);
        }
        catch { return false; }
    }

    /// <summary>
    /// 生成自签名 CA 证书并写入磁盘（证书和私钥分开存储）
    /// </summary>
    public void GenerateCa()
    {
        using var key = RSA.Create(2048);

        var req = new CertificateRequest(
            "CN=Netease Login Helper CA, O=Netease Login Helper CA, C=US",
            key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-3);
        var notAfter  = DateTimeOffset.UtcNow.AddDays(360);

        using var cert = req.CreateSelfSigned(notBefore, notAfter);

        // 将 CA 证书和 CA 私钥分别写入各自文件
        File.WriteAllText(_config.CaCertFile, ExportCertPem(cert));
        File.WriteAllText(_config.CaKeyFile,  ExportKeyPem(key));
    }

    /// <summary>
    /// 为指定域名列表签发服务端证书（由本地 CA 签名）
    /// </summary>
    public void GenerateDomainCert(IEnumerable<string> hostnames, X509Certificate2 caCert, RSA caKey)
    {
        using var key = RSA.Create(2048);
        var sans = new SubjectAlternativeNameBuilder();
        foreach (var host in hostnames)
            sans.AddDnsName(host);

        var firstHost = hostnames.First();
        var req = new CertificateRequest(
            $"CN={firstHost}, O=Netease Login Helper Web, C=US",
            key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(sans.Build());
        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-3);
        var notAfter  = DateTimeOffset.UtcNow.AddDays(198);

        using var cert = req.Create(caCert, notBefore, notAfter,
            RandomNumberGenerator.GetBytes(16));

        File.WriteAllText(_config.WebCertFile, ExportCertPem(cert));
        File.WriteAllText(_config.WebKeyFile,  ExportKeyPem(key));
    }

    /// <summary>
    /// 将 CA 证书导入 Windows 受信任根证书存储
    /// </summary>
    public bool ImportCaToWindowsStore()
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var cert = X509Certificate2.CreateFromPemFile(_config.CaCertFile);
            store.Add(cert);
            store.Close();
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// 检查 CA 证书是否已在 Windows 受信任根存储中
    /// </summary>
    public bool IsCaInstalledInStore()
    {
        if (!File.Exists(_config.CaCertFile)) return false;
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(_config.CaCertFile);
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, cert.Thumbprint, false);
            return found.Count > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 加载 CA 证书和私钥（供域名证书签发使用）
    /// </summary>
    public (X509Certificate2 cert, RSA key)? LoadCa()
    {
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(_config.CaCertFile, _config.CaKeyFile);
            using var rsa = cert.GetRSAPrivateKey();
            return rsa is null ? null : (cert, rsa);
        }
        catch { return null; }
    }

    private static string ExportCertPem(X509Certificate2 cert)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN CERTIFICATE-----");
        sb.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        sb.AppendLine("-----END CERTIFICATE-----");
        return sb.ToString();
    }

    private static string ExportKeyPem(RSA key)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
        sb.AppendLine(Convert.ToBase64String(
            key.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks));
        sb.AppendLine("-----END RSA PRIVATE KEY-----");
        return sb.ToString();
    }
}
