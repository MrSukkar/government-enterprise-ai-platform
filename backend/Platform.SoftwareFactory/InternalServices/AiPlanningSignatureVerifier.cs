using System.Security.Cryptography;
using Platform.Evidence.Chain;

namespace Platform.SoftwareFactory.InternalService;

internal static class AiPlanningSignatureVerifier
{
    public static bool Verify(string algorithm, IReadOnlyDictionary<string, string> trustedKeys,
        SignatureEnvelope signature, byte[] sha256Digest)
    {
        signature.Validate();
        if (!StringComparer.Ordinal.Equals(signature.Algorithm, algorithm) ||
            !trustedKeys.TryGetValue(signature.KeyId, out var pem)) return false;
        try
        {
            var bytes = Convert.FromBase64String(signature.SignatureBase64);
            if (algorithm == "RS256")
            {
                using var rsa = RSA.Create(); rsa.ImportFromPem(pem);
                return rsa.VerifyHash(sha256Digest, bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            using var ecdsa = ECDsa.Create(); ecdsa.ImportFromPem(pem);
            return ecdsa.VerifyHash(sha256Digest, bytes);
        }
        catch (CryptographicException) { return false; }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
