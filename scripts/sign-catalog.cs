// Signs and checks the official catalog files (index.signed.json and revoked.signed.json).
//
//   dotnet run scripts/sign-catalog.cs -- sign <index|revoked> <source.json> <out.json> <private-key.pem>
//   dotnet run scripts/sign-catalog.cs -- verify <file.json> <index|revoked> [public-key.pem]
//   dotnet run scripts/sign-catalog.cs -- test-keygen <folder>
//
// A signed file is { "payload": base64, "signature": base64 }. The payload is the source JSON with a header put first:
// kind, sequence (one more than the sequence already in <out.json>, or 1 for a first file) and issuedAt (UTC). The signature is
// ECDSA P-256 over the decoded payload bytes with SHA-256, the scheme the host checks before it reads a byte (see
// website/reference/source-index.md). The private key is only read, never copied or printed. verify without a public key
// only checks the structure; with one it also checks the signature.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const long MaxIndexBytes = 1_500_000;
const long MaxRevokedBytes = 256 * 1024;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: sign-catalog.cs sign <index|revoked> <source.json> <out.json> <private-key.pem> | verify <file.json> <index|revoked> [public-key.pem] | test-keygen <folder>");
    return 2;
}

try
{
    switch (args[0])
    {
        case "sign" when args.Length == 5: return Sign(args[1], args[2], args[3], args[4]);
        case "verify" when args.Length is 3 or 4: return Verify(args[1], args[2], args.Length == 4 ? args[3] : null);
        case "test-keygen": return KeyGen(args[1]);
        default:
            Console.Error.WriteLine("unknown or incomplete command");
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or CryptographicException or FormatException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static long MaxBytes(string kind) => kind == "index" ? MaxIndexBytes : MaxRevokedBytes;

static (JsonObject Header, byte[] Payload, byte[] Signature) ReadEnvelope(string path)
{
    var envelope = JsonNode.Parse(File.ReadAllBytes(path)) as JsonObject ?? throw new InvalidOperationException($"{path} is not a JSON object");
    var payload = Convert.FromBase64String(envelope["payload"]?.GetValue<string>() ?? throw new InvalidOperationException("no payload"));
    var signature = Convert.FromBase64String(envelope["signature"]?.GetValue<string>() ?? throw new InvalidOperationException("no signature"));
    var header = JsonNode.Parse(payload) as JsonObject ?? throw new InvalidOperationException("the payload is not a JSON object");
    return (header, payload, signature);
}

static int Sign(string kind, string sourcePath, string outPath, string keyPath)
{
    if (kind is not ("index" or "revoked")) throw new InvalidOperationException("kind must be index or revoked");
    var source = JsonNode.Parse(File.ReadAllBytes(sourcePath)) as JsonObject ?? throw new InvalidOperationException("the source is not a JSON object");

    long sequence = 1;
    if (File.Exists(outPath))
    {
        var (previous, _, _) = ReadEnvelope(outPath);
        if (previous["kind"]?.GetValue<string>() != kind) throw new InvalidOperationException($"{outPath} holds a different kind of file");
        sequence = previous["sequence"]!.GetValue<long>() + 1;
    }

    var payloadObject = new JsonObject
    {
        ["kind"] = kind,
        ["sequence"] = sequence,
        ["issuedAt"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    };
    foreach (var (name, value) in source)
        if (name is not ("kind" or "sequence" or "issuedAt")) payloadObject[name] = value?.DeepClone();

    var payload = Encoding.UTF8.GetBytes(payloadObject.ToJsonString());
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(File.ReadAllText(keyPath));
    var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);
    if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256)) throw new InvalidOperationException("signature self-check failed");

    var envelope = new JsonObject { ["payload"] = Convert.ToBase64String(payload), ["signature"] = Convert.ToBase64String(signature) };
    var text = envelope.ToJsonString();
    if (text.Length > MaxBytes(kind)) throw new InvalidOperationException($"the signed file would be {text.Length} bytes, over the limit of {MaxBytes(kind)}");
    File.WriteAllText(outPath, text, new UTF8Encoding(false));
    Console.WriteLine($"{kind} sequence={sequence}");
    return 0;
}

static int Verify(string path, string kind, string? publicKeyPath)
{
    var size = new FileInfo(path).Length;
    if (size > MaxBytes(kind)) throw new InvalidOperationException($"{path} is {size} bytes, over the limit of {MaxBytes(kind)}");
    var (header, payload, signature) = ReadEnvelope(path);
    if (header["kind"]?.GetValue<string>() != kind) throw new InvalidOperationException($"{path} is not a {kind} file");
    if (header["sequence"]?.GetValue<long>() is not >= 1) throw new InvalidOperationException("the sequence must be a whole number from 1");
    if (header["formatVersion"]?.GetValue<int>() is not (1 or 2)) throw new InvalidOperationException("unsupported formatVersion");
    if (publicKeyPath is not null)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(File.ReadAllText(publicKeyPath));
        if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256)) throw new InvalidOperationException("the signature does not verify");
    }
    Console.WriteLine($"{kind} sequence={header["sequence"]} ok{(publicKeyPath is null ? " (structure only)" : "")}");
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
