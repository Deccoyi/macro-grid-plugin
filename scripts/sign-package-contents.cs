// Signs the CONTENTS of a staged plugin folder, so the signature can still be checked after the zip is extracted.
//
//   dotnet run scripts/sign-package-contents.cs -- sign <stage-folder> <private-key.pem>
//   dotnet run scripts/sign-package-contents.cs -- verify <folder> <public-key.pem>
//   dotnet run scripts/sign-package-contents.cs -- test-keygen <output-folder>
//
// sign writes two files into the folder root:
//   signature.json  { formatVersion, id, version, kind, files: [ { path, sha256 } ] }: every file of the folder except the two
//                   signature files, paths with '/', sorted by ordinal comparison, UTF-8 without a byte order mark.
//   signature.sig   base64 ECDSA P-256 / SHA-256 signature over the exact bytes of signature.json.
// id, version and kind come from the folder's plugin.json. The server (PluginTrustVerifier) checks all of this every time a
// C# plugin loads. The private key is only read, never copied or printed; test-keygen makes a throwaway pair for tests.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string JsonName = "signature.json";
const string SigName = "signature.sig";

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: sign-package-contents.cs sign <folder> <private-key.pem> | verify <folder> <public-key.pem> | test-keygen <folder>");
    return 2;
}

switch (args[0])
{
    case "sign" when args.Length == 3: return Sign(args[1], args[2]);
    case "verify" when args.Length == 3: return Verify(args[1], args[2]);
    case "test-keygen": return KeyGen(args[1]);
    default:
        Console.Error.WriteLine("unknown or incomplete command");
        return 2;
}

static int Sign(string folder, string keyPath)
{
    var root = Path.GetFullPath(folder);
    using var manifestDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "plugin.json")));
    var manifest = manifestDoc.RootElement;
    string Field(string name) => manifest.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s
        ? s
        : throw new InvalidOperationException($"plugin.json has no \"{name}\"");

    var files = new List<(string Path, string Sha256)>();
    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
        if (relative == JsonName || relative == SigName) continue;
        files.Add((relative, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant()));
    }
    files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
    {
        writer.WriteStartObject();
        writer.WriteNumber("formatVersion", 1);
        writer.WriteString("id", Field("id"));
        writer.WriteString("version", Field("version"));
        writer.WriteString("kind", Field("kind"));
        writer.WriteStartArray("files");
        foreach (var (path, sha256) in files)
        {
            writer.WriteStartObject();
            writer.WriteString("path", path);
            writer.WriteString("sha256", sha256);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
    var bytes = stream.ToArray();

    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(File.ReadAllText(keyPath));
    var signature = ecdsa.SignData(bytes, HashAlgorithmName.SHA256);
    if (!ecdsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256))
    {
        Console.Error.WriteLine("signature self-check failed");
        return 1;
    }
    File.WriteAllBytes(Path.Combine(root, JsonName), bytes);
    File.WriteAllText(Path.Combine(root, SigName), Convert.ToBase64String(signature), new UTF8Encoding(false));
    Console.WriteLine($"files={files.Count}");
    return 0;
}

static int Verify(string folder, string publicKeyPath)
{
    var root = Path.GetFullPath(folder);
    var bytes = File.ReadAllBytes(Path.Combine(root, JsonName));
    var signature = Convert.FromBase64String(File.ReadAllText(Path.Combine(root, SigName)).Trim());
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(File.ReadAllText(publicKeyPath));
    if (!ecdsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256))
    {
        Console.Error.WriteLine("bad signature");
        return 1;
    }
    using var doc = JsonDocument.Parse(bytes);
    foreach (var entry in doc.RootElement.GetProperty("files").EnumerateArray())
    {
        var relative = entry.GetProperty("path").GetString()!;
        var path = Path.Combine(root, relative);
        if (!File.Exists(path) || !string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), entry.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"mismatch: {relative}");
            return 1;
        }
    }
    Console.WriteLine("ok");
    return 0;
}

static int KeyGen(string folder)
{
    Directory.CreateDirectory(folder);
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    File.WriteAllText(Path.Combine(folder, "test-private.pem"), ecdsa.ExportPkcs8PrivateKeyPem());
    File.WriteAllText(Path.Combine(folder, "test-public.pem"), ecdsa.ExportSubjectPublicKeyInfoPem());
    return 0;
}
