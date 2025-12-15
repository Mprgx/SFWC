using MobyPark.Services;

namespace MobyPark.EncryptionHelper
{
    public static class LicensePlateProtector
    {
        public static string Normalize(string? plate)
            => (plate ?? string.Empty).Trim().ToUpperInvariant();

        public static string EncryptNormalized(IEncryptionService encryption, string normalizedPlate)
            => encryption.Encrypt(normalizedPlate) ?? string.Empty;

        public static string Encrypt(IEncryptionService encryption, string? plate)
            => EncryptNormalized(encryption, Normalize(plate));

        public static string DecryptNormalized(IEncryptionService encryption, string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return string.Empty;

            var trimmed = storedValue.Trim();

            if (trimmed.Length < 40)
                return trimmed.ToUpperInvariant();

            try
            {
                var plain = encryption.Decrypt(trimmed);
                return Normalize(plain);
            }
            catch (FormatException)
            {
                return trimmed.ToUpperInvariant();
            }
        }
    }
}
