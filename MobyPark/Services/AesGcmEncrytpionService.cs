using System.Security.Cryptography;
using System.Text;

namespace MobyPark.Services
{
    public class AesGcmEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public AesGcmEncryptionService(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("Missing Encryption:Key in configuration.");

            _key = Convert.FromBase64String(keyBase64);

            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key must be a 32-byte key (Base64 of 32 bytes).");
        }

        public string? Encrypt(string? plaintext)
        {
            if (plaintext is null) return null;
            if (plaintext.Length == 0) return string.Empty;

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(_key);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            byte[] combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(combined);
        }

        public string? Decrypt(string? ciphertext)
        {
            if (ciphertext is null) return null;
            if (ciphertext.Length == 0) return string.Empty;

            byte[] combined;
            try
            {
                combined = Convert.FromBase64String(ciphertext);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Invalid ciphertext format.", ex);
            }

            if (combined.Length < 12 + 16)
                throw new CryptographicException("Ciphertext is too short.");

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] cipherBytes = new byte[combined.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(combined, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(combined, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

            byte[] plaintextBytes = new byte[cipherBytes.Length];

            using var aes = new AesGcm(_key);
            aes.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
