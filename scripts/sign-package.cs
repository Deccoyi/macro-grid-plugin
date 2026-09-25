// Hashes and signs one plugin zip. Run with: dotnet run scripts/sign-package.cs -- <zip> <private-key.pem>
//
// Writes <zip>.sha256 (lowercase hex) and <zip>.sig (base64) next to the zip. The signature is ECDSA P-256 over the
// zip's own bytes with SHA-256, the exact scheme the host verifies (see website/reference/source-index.md). The private
// key is only read, never copied or printed.
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: sign-package.cs <zip> <private-key.pem>");
    return 2;
}

var zipPath = args[0];
var bytes = File.ReadAllBytes(zipPath);
var sha256Hex = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
File.WriteAllText(zipPath + ".sha256", sha256Hex);

using var ecdsa = ECDsa.Create();
ecdsa.ImportFromPem(File.ReadAllText(args[1]));
var signature = ecdsa.SignData(bytes, HashAlgorithmName.SHA256);
if (!ecdsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256))
{
    Console.Error.WriteLine("signature self-check failed");
    return 1;
}
var signatureBase64 = Convert.ToBase64String(signature);
File.WriteAllText(zipPath + ".sig", signatureBase64);

// Machine-readable result for the calling script: hash, size and signature (none of these are secret).
Console.WriteLine($"sha256={sha256Hex}");
Console.WriteLine($"size={bytes.Length}");
Console.WriteLine($"signature={signatureBase64}");
return 0;
