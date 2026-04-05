using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KBlazor.Services;

/// <summary>
/// Machine-locked license provider for KBlazor.
///
/// Flow:
/// 1. On startup, computes a machine fingerprint (hash of hardware identifiers).
/// 2. Looks for a license file (kblazor.lic) in the application base directory.
/// 3. If no license found → logs the fingerprint as a Key Signing Request.
/// 4. If license found → validates RSA signature and checks fingerprint match.
///
/// The license file is a JSON object with a detached RSA-SHA256 signature:
/// {
///   "fingerprint": "ABC123...",
///   "licensee": "Acme Corp",
///   "expiry": "2027-12-31",
///   "features": ["save-views", "save-edits"],
///   "signature": "base64..."
/// }
/// </summary>
public class KBlazorLicenseProvider : ILicenseProvider
{
    private readonly ILogger<KBlazorLicenseProvider> _logger;
    private readonly string _fingerprint;
    private readonly bool _isLicensed;
    private readonly string _statusMessage;

    // RSA public key for verifying license signatures (PEM format).
    // The corresponding private key is held by the KBlazor licensing service.
    // Replace this with your actual public key.
    private const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoPnC53Y1Nl5OLWNLt6Cb
        /jiotxSPacvf0ukMPSMoqrLnnNrxRQD7Tv9DkeQAsCeTDXxmr8MOfnlglVOeIUYO
        MkeHK1r7PS+C4Mzi9z6ZeGALpi6+sMKM++MXqkZjuFAUn7MnQpe74kNJS3IpSrq3
        AvEx6PBSek5kIQV7k50lMjiajZWEAupZoDbDbuTF7w+SxahZHdxXx+7CP6Z5Dndk
        MHNBnMRkDAGW1O7nBls0cqcR9SUNVo6helC29P6fb1bYTTzBA4rZT4ANTclzdzVk
        QD/SCg2IQbzF7y0bAWQ5pI/eO2i3OGKGdLrGTKO6EE0bfEdUTgyZ56sOWyGFgRis
        tQIDAQAB
        -----END PUBLIC KEY-----
        """;

    public KBlazorLicenseProvider(ILogger<KBlazorLicenseProvider> logger)
    {
        _logger = logger;
        _fingerprint = ComputeFingerprint();

        var licPath = FindLicenseFile();
        if (licPath != null)
        {
            (_isLicensed, _statusMessage) = ValidateLicense(licPath);
        }
        else
        {
            _isLicensed = false;
            _statusMessage = "Unlicensed — saving disabled";
            LogKeySigningRequest();
        }
    }

    public bool IsLicensed => _isLicensed;
    public string StatusMessage => _statusMessage;
    public string MachineFingerprint => _fingerprint;

    // ═══════════════════════════════════════════════════════
    //  MACHINE FINGERPRINT
    // ═══════════════════════════════════════════════════════

    private static string ComputeFingerprint()
    {
        var sb = new StringBuilder();

        // Machine name
        sb.Append(Environment.MachineName);
        sb.Append('|');

        // OS description
        sb.Append(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        sb.Append('|');

        // Processor count (stable across reboots)
        sb.Append(Environment.ProcessorCount);
        sb.Append('|');

        // First non-loopback MAC address (stable hardware identifier)
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderBy(n => n.Id) // deterministic ordering
                .Select(n => n.GetPhysicalAddress().ToString())
                .FirstOrDefault(m => !string.IsNullOrEmpty(m));

            sb.Append(mac ?? "NO-MAC");
        }
        catch
        {
            sb.Append("NO-MAC");
        }

        // Hash the composite string
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }

    // ═══════════════════════════════════════════════════════
    //  LICENSE FILE DISCOVERY
    // ═══════════════════════════════════════════════════════

    private static string? FindLicenseFile()
    {
        // Search in order: app base dir, working dir, up to 3 parent dirs
        var searchPaths = new List<string>
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        var dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 3; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            searchPaths.Add(parent.FullName);
            dir = parent.FullName;
        }

        foreach (var path in searchPaths.Distinct())
        {
            var file = Path.Combine(path, "kblazor.lic");
            if (File.Exists(file)) return file;
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════
    //  LICENSE VALIDATION
    // ═══════════════════════════════════════════════════════

    private (bool isValid, string message) ValidateLicense(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract fields
            var fingerprint = root.GetProperty("fingerprint").GetString() ?? "";
            var licensee = root.GetProperty("licensee").GetString() ?? "Unknown";
            var expiry = root.GetProperty("expiry").GetString() ?? "";
            var signature = root.GetProperty("signature").GetString() ?? "";

            // Check fingerprint match
            if (!string.Equals(fingerprint, _fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("KBlazor license fingerprint mismatch. Expected: {Expected}, Got: {Got}",
                    _fingerprint, fingerprint);
                LogKeySigningRequest();
                return (false, $"License fingerprint mismatch — not valid for this machine");
            }

            // Check expiry
            if (DateTime.TryParse(expiry, out var expiryDate) && expiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("KBlazor license expired on {Expiry}", expiry);
                return (false, $"License expired on {expiry}");
            }

            // Verify RSA signature over the payload (everything except the signature field)
            var payload = BuildSignaturePayload(fingerprint, licensee, expiry, root);
            if (!VerifySignature(payload, signature))
            {
                _logger.LogWarning("KBlazor license signature verification failed");
                return (false, "License signature invalid");
            }

            _logger.LogInformation("KBlazor licensed to {Licensee} (expires {Expiry})", licensee, expiry);
            return (true, $"Licensed to {licensee} (expires {expiry})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read KBlazor license file at {Path}", path);
            LogKeySigningRequest();
            return (false, "License file could not be read");
        }
    }

    private static string BuildSignaturePayload(string fingerprint, string licensee, string expiry, JsonElement root)
    {
        // Deterministic payload: fingerprint + licensee + expiry + features (sorted)
        var sb = new StringBuilder();
        sb.Append(fingerprint);
        sb.Append('|');
        sb.Append(licensee);
        sb.Append('|');
        sb.Append(expiry);

        if (root.TryGetProperty("features", out var featuresEl) && featuresEl.ValueKind == JsonValueKind.Array)
        {
            var features = featuresEl.EnumerateArray()
                .Select(f => f.GetString() ?? "")
                .OrderBy(f => f, StringComparer.Ordinal);
            foreach (var f in features)
            {
                sb.Append('|');
                sb.Append(f);
            }
        }

        return sb.ToString();
    }

    private static bool VerifySignature(string payload, string signatureBase64)
    {
        try
        {
            // Strip PEM headers/whitespace for import
            var keyText = PublicKeyPem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "").Replace("\r", "").Replace(" ", "");

            // If the key is a placeholder, skip verification (development mode)
            if (keyText.Contains("PLACEHOLDER"))
                return false;

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keyText), out _);

            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  KEY SIGNING REQUEST OUTPUT
    // ═══════════════════════════════════════════════════════

    private void LogKeySigningRequest()
    {
        _logger.LogWarning(
            """

            ╔══════════════════════════════════════════════════════════════════╗
            ║                    KBlazor — Key Signing Request                ║
            ╠══════════════════════════════════════════════════════════════════╣
            ║                                                                  ║
            ║  Machine Fingerprint:                                            ║
            ║  {Fingerprint}
            ║                                                                  ║
            ║  Send this fingerprint to your KBlazor license provider          ║
            ║  to receive a signed kblazor.lic file for this machine.          ║
            ║                                                                  ║
            ║  Place the file in your application's root directory.            ║
            ║                                                                  ║
            ║  Without a license, saving views and edits is disabled.          ║
            ║                                                                  ║
            ╚══════════════════════════════════════════════════════════════════╝

            """, _fingerprint);
    }
}
