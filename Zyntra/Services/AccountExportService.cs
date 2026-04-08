using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public class ExportedAccount
{
    public string DisplayName { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Cookie { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public static class AccountExportService
{
    private static readonly byte[] ExportKey = Encoding.UTF8.GetBytes("Zyntra_Export_Key_32B!@#$%^&*()12");
    private static readonly byte[] ExportIV = Encoding.UTF8.GetBytes("Zyntra_IV_16B!@#");

    public static void Export(List<RobloxAccount> accounts, string filePath)
    {
        var exportList = accounts.Select(a => new ExportedAccount
        {
            DisplayName = a.DisplayName,
            UserId = a.UserId,
            Cookie = CryptoService.Decrypt(a.EncryptedCookie),
            AvatarUrl = a.AvatarUrl,
        }).ToList();

        string json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });
        byte[] plainBytes = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = ExportKey;
        aes.IV = ExportIV;

        using var fs = new FileStream(filePath, FileMode.Create);
        // Write magic header
        byte[] magic = "ZYNTRA"u8.ToArray();
        fs.Write(magic, 0, magic.Length);

        using var encryptor = aes.CreateEncryptor();
        using var cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
        cs.Write(plainBytes, 0, plainBytes.Length);
    }

    public static List<RobloxAccount> Import(string filePath)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);

        // Verify magic header
        string header = Encoding.UTF8.GetString(fileBytes, 0, 6);
        if (header != "ZYNTRA")
            throw new InvalidDataException("Not a valid Zyntra export file.");

        byte[] encrypted = fileBytes[6..];

        using var aes = Aes.Create();
        aes.Key = ExportKey;
        aes.IV = ExportIV;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encrypted);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        string json = sr.ReadToEnd();

        var exported = JsonSerializer.Deserialize<List<ExportedAccount>>(json)
            ?? throw new InvalidDataException("Failed to parse export data.");

        return exported.Select(e => new RobloxAccount
        {
            DisplayName = e.DisplayName,
            UserId = e.UserId,
            EncryptedCookie = CryptoService.Encrypt(e.Cookie),
            AvatarUrl = e.AvatarUrl ?? string.Empty,
        }).ToList();
    }
}
