using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// ═══════════════════════════════════════════════════════════════════
//  KBlazor License Tool
//
//  Commands:
//    generate-keys              Generate RSA key pair (private.pem + public.pem)
//    sign <fingerprint>         Sign a license for a machine fingerprint
//      --licensee <name>        Licensee name (required)
//      --expiry <date>          Expiry date, e.g. 2027-12-31 (required)
//      --features <csv>         Comma-separated features (default: save-views,save-edits)
//      --private-key <path>     Path to private key PEM (default: private.pem)
//      --output <path>          Output license file (default: kblazor.lic)
//    verify <license-file>      Verify a license file
//      --public-key <path>      Path to public key PEM (default: public.pem)
// ═══════════════════════════════════════════════════════════════════

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();

return command switch
{
    "generate-keys" => GenerateKeys(),
    "sign" => Sign(args),
    "verify" => Verify(args),
    _ => PrintUsage()
};

// ── Commands ─────────────────────────────────────────────

static int GenerateKeys()
{
    using var rsa = RSA.Create(2048);

    var privatePem = rsa.ExportRSAPrivateKeyPem();
    var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

    File.WriteAllText("private.pem", privatePem);
    File.WriteAllText("public.pem", publicPem);

    Console.WriteLine("Generated RSA-2048 key pair:");
    Console.WriteLine("  private.pem — KEEP SECRET, used to sign licenses");
    Console.WriteLine("  public.pem  — embed in KBlazorLicenseProvider.cs for verification");
    Console.WriteLine();
    Console.WriteLine("Public key (copy into KBlazorLicenseProvider.cs):");
    Console.WriteLine(publicPem);

    return 0;
}

static int Sign(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: sign <fingerprint> --licensee <name> --expiry <date>");
        return 1;
    }

    var fingerprint = args[1];
    var licensee = GetArg(args, "--licensee") ?? GetArg(args, "-l");
    var expiry = GetArg(args, "--expiry") ?? GetArg(args, "-e");
    var featuresStr = GetArg(args, "--features") ?? "save-views,save-edits";
    var privateKeyPath = GetArg(args, "--private-key") ?? "private.pem";
    var outputPath = GetArg(args, "--output") ?? "kblazor.lic";

    if (string.IsNullOrEmpty(licensee))
    {
        Console.Error.WriteLine("--licensee is required");
        return 1;
    }
    if (string.IsNullOrEmpty(expiry))
    {
        Console.Error.WriteLine("--expiry is required (e.g. 2027-12-31)");
        return 1;
    }
    if (!DateTime.TryParse(expiry, out _))
    {
        Console.Error.WriteLine($"Invalid expiry date: {expiry}");
        return 1;
    }
    if (!File.Exists(privateKeyPath))
    {
        Console.Error.WriteLine($"Private key not found: {privateKeyPath}");
        Console.Error.WriteLine("Run 'generate-keys' first.");
        return 1;
    }

    var features = featuresStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToArray();

    // Build deterministic payload
    var payload = BuildPayload(fingerprint, licensee, expiry, features);

    // Sign with RSA private key
    var privateKeyPem = File.ReadAllText(privateKeyPath);
    using var rsa = RSA.Create();
    rsa.ImportFromPem(privateKeyPem);

    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var signatureBase64 = Convert.ToBase64String(signatureBytes);

    // Build license JSON
    var license = new
    {
        fingerprint,
        licensee,
        expiry,
        features,
        signature = signatureBase64
    };

    var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(outputPath, json);

    Console.WriteLine($"License signed successfully:");
    Console.WriteLine($"  Licensee:    {licensee}");
    Console.WriteLine($"  Fingerprint: {fingerprint}");
    Console.WriteLine($"  Expiry:      {expiry}");
    Console.WriteLine($"  Features:    {string.Join(", ", features)}");
    Console.WriteLine($"  Output:      {outputPath}");

    return 0;
}

static int Verify(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: verify <license-file> [--public-key <path>]");
        return 1;
    }

    var licensePath = args[1];
    var publicKeyPath = GetArg(args, "--public-key") ?? "public.pem";

    if (!File.Exists(licensePath))
    {
        Console.Error.WriteLine($"License file not found: {licensePath}");
        return 1;
    }
    if (!File.Exists(publicKeyPath))
    {
        Console.Error.WriteLine($"Public key not found: {publicKeyPath}");
        return 1;
    }

    try
    {
        var json = File.ReadAllText(licensePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var fingerprint = root.GetProperty("fingerprint").GetString() ?? "";
        var licensee = root.GetProperty("licensee").GetString() ?? "";
        var expiry = root.GetProperty("expiry").GetString() ?? "";
        var signature = root.GetProperty("signature").GetString() ?? "";

        var features = Array.Empty<string>();
        if (root.TryGetProperty("features", out var featuresEl))
        {
            features = featuresEl.EnumerateArray()
                .Select(f => f.GetString() ?? "")
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();
        }

        // Check expiry
        if (DateTime.TryParse(expiry, out var expiryDate) && expiryDate < DateTime.UtcNow)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"EXPIRED — License expired on {expiry}");
            Console.ResetColor();
            return 1;
        }

        // Verify signature
        var payload = BuildPayload(fingerprint, licensee, expiry, features);
        var publicKeyPem = File.ReadAllText(publicKeyPath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = Convert.FromBase64String(signature);
        var isValid = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (isValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("VALID");
            Console.ResetColor();
            Console.WriteLine($"  Licensee:    {licensee}");
            Console.WriteLine($"  Fingerprint: {fingerprint}");
            Console.WriteLine($"  Expiry:      {expiry}");
            Console.WriteLine($"  Features:    {string.Join(", ", features)}");
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("INVALID — Signature verification failed");
            Console.ResetColor();
            return 1;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error reading license: {ex.Message}");
        return 1;
    }
}

// ── Helpers ──────────────────────────────────────────────

static string BuildPayload(string fingerprint, string licensee, string expiry, string[] features)
{
    var sb = new StringBuilder();
    sb.Append(fingerprint);
    sb.Append('|');
    sb.Append(licensee);
    sb.Append('|');
    sb.Append(expiry);
    foreach (var f in features)
    {
        sb.Append('|');
        sb.Append(f);
    }
    return sb.ToString();
}

static string? GetArg(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return null;
}

static int PrintUsage()
{
    Console.WriteLine("""
        KBlazor License Tool

        Commands:
          generate-keys                    Generate RSA key pair (private.pem + public.pem)

          sign <fingerprint>               Sign a license for a machine fingerprint
            --licensee <name>              Licensee name (required)
            --expiry <date>                Expiry date, e.g. 2027-12-31 (required)
            --features <csv>               Features (default: save-views,save-edits)
            --private-key <path>           Private key PEM (default: private.pem)
            --output <path>                Output file (default: kblazor.lic)

          verify <license-file>            Verify a license file
            --public-key <path>            Public key PEM (default: public.pem)

        Workflow:
          1. Run 'generate-keys' once to create your RSA key pair
          2. Embed public.pem contents in KBlazorLicenseProvider.cs
          3. Customer starts their app -> console shows Key Signing Request with fingerprint
          4. Run 'sign <fingerprint> --licensee "Acme" --expiry 2027-12-31'
          5. Send kblazor.lic to customer -> they place it in their app directory
        """);
    return 1;
}
