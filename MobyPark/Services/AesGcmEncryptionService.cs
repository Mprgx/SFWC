using System.Security.Cryptography;
using System.Text;

namespace MobyPark.Services
{
    public class AesGcmEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public AesGcmEncryptionService(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"];
            if (string.IsNullOrWhiteSpace(keyBase64))
                throw new InvalidOperationException("Missing Encryption:Key in configuration.");

            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key must be a 32-byte key in Base64 (256-bit).");
        }

        public string? Encrypt(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(_key);
            aes.Encrypt(nonce, plainBytes, cipher, tag);

            var combined = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, combined, nonce.Length + tag.Length, cipher.Length);

            return Convert.ToBase64String(combined);
        }

        public string? Decrypt(string? ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return ciphertext;

            var data = Convert.FromBase64String(ciphertext);
            if (data.Length < 12 + 16)
                throw new InvalidOperationException("Ciphertext too short.");

            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[data.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(data, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(data, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(data, nonce.Length + tag.Length, cipher, 0, cipher.Length);

            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_key);
            aes.Decrypt(nonce, cipher, tag, plain);

            return Encoding.UTF8.GetString(plain);
        }
    }
}
