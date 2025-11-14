namespace MobyPark.Services
{
    public interface IEncryptionService
    {
        string? Encrypt(string? plaintext);
        string? Decrypt(string? ciphertext);
    }
}
