using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MobyPark.Data;
using MobyPark.Entities;

public sealed class UserJson
{
    public string? id { get; set; }
    public string? username { get; set; }
    public string? password { get; set; }
    public string? name { get; set; }
    public string? email { get; set; }
    public string? phone { get; set; }
    public string? role { get; set; }
    public string? created_at { get; set; }
    public int? birth_year { get; set; }
    public bool? active { get; set; }
}

public interface IEncryptionService
{
    string? Encrypt(string? plaintext);
    string? Decrypt(string? ciphertext);
}

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

        if (!IsBase64(ciphertext))
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

    private static bool IsBase64(string s)
    {
        s = s.Trim();
        if (s.Length % 4 != 0) return false;

        Span<byte> buffer = stackalloc byte[s.Length];
        return Convert.TryFromBase64String(s, buffer, out _);
    }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        var jsonPath = args.Length > 0 ? args[0] : "users.json";
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"File not found: {jsonPath}");
            return 1;
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var conn = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Console.WriteLine("Missing ConnectionStrings:DefaultConnection in appsettings.json");
            return 1;
        }

        IEncryptionService enc = new AesGcmEncryptionService(config);

        var defaultBcryptHash = BCrypt.Net.BCrypt.HashPassword("Test123!");

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(conn)
            .Options;

        await using var db = new UserDbContext(options);

        Console.WriteLine($"✅ Connected database: {db.Database.GetDbConnection().Database}");
        Console.WriteLine($"✅ DataSource (server): {db.Database.GetDbConnection().DataSource}");
        Console.WriteLine($"✅ Users in DB: {await db.Users.AsNoTracking().CountAsync()}");

        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingPhoneNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (await db.Users.AsNoTracking().AnyAsync())
        {
            var existingUsers = await db.Users.AsNoTracking()
                .Select(u => new { u.Email, u.Username, u.PhoneNumber })
                .ToListAsync();

            existingEmails = existingUsers
                .Select(x => Normalize(enc.Decrypt(x.Email) ?? ""))
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            existingUsernames = existingUsers
                .Select(x => Normalize(x.Username))
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            existingPhoneNumbers = existingUsers
                .Select(x => Normalize(enc.Decrypt(x.PhoneNumber) ?? ""))
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenPhoneNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rejectPath = Path.ChangeExtension(jsonPath, ".rejected.jsonl");
        var dupPath = Path.ChangeExtension(jsonPath, ".duplicates.jsonl");
        await using var rejectWriter = new StreamWriter(rejectPath);
        await using var dupWriter = new StreamWriter(dupPath);

        var batch = new List<User>(1000);
        var inserted = 0;
        var rejected = 0;
        var duplicates = 0;

        var firstNonWs = PeekFirstNonWhitespaceChar(jsonPath);

        if (firstNonWs == '[')
        {
            await using var fs = File.OpenRead(jsonPath);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            await foreach (var u in JsonSerializer.DeserializeAsyncEnumerable<UserJson>(fs, opts))
            {
                if (u is null) continue;
                await HandleOne(u);
            }
        }
        else
        {
            foreach (var line in File.ReadLines(jsonPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var u = JsonSerializer.Deserialize<UserJson>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (u is null) continue;
                    await HandleOne(u);
                }
                catch
                {
                    rejected++;
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new { reason = "Invalid JSON line", line }));
                }
            }
        }

        if (batch.Count > 0)
            await FlushBatch();

        db.ChangeTracker.AutoDetectChangesEnabled = true;

        Console.WriteLine($"Done. Inserted={inserted}, Duplicates={duplicates}, Rejected={rejected}");
        Console.WriteLine($"Rejected log: {rejectPath}");
        Console.WriteLine($"Duplicates log: {dupPath}");
        return 0;

        async Task HandleOne(UserJson j)
        {
            var username = (j.username ?? "").Trim().ToLowerInvariant();
            var name = (j.name ?? "").Trim();
            var email = (j.email ?? "").Trim();
            var phone = (j.phone ?? "").Trim();
            var legacyPasswordHash = (j.password ?? "").Trim();

            if (username.Length == 0 || name.Length == 0 || email.Length == 0 || phone.Length == 0 || legacyPasswordHash.Length == 0 || j.birth_year is null)
            {
                rejected++;
                await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new { reason = "Missing required field(s)", record = j }));
                return;
            }

            if (username.Length > 40 || name.Length > 70)
            {
                rejected++;
                await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new { reason = "Field too long (Username>20 or Name>70)", record = j }));
                return;
            }

            var emailKey = Normalize(email);
            var userKey = Normalize(username);
            var phoneKey = Normalize(phone);

            if (existingEmails.Contains(emailKey) || seenEmails.Contains(emailKey) ||
                existingUsernames.Contains(userKey) || seenUsernames.Contains(userKey) ||
                existingPhoneNumbers.Contains(phoneKey) || seenPhoneNumbers.Contains(phoneKey))
            {
                duplicates++;
                await dupWriter.WriteLineAsync(JsonSerializer.Serialize(new { reason = "Duplicate email/username/phone", record = j }));
                return;
            }

            var createdAt = ParseCreatedAt(j.created_at) ?? DateTimeOffset.UtcNow;
            var role = MapRole(j.role);

            var user = new User
            {
                Id = Guid.NewGuid(),
                LegacyId = j.id,

                Username = username,
                Name = name,

                Email = enc.Encrypt(email) ?? "",
                PhoneNumber = enc.Encrypt(phone) ?? "",

                PasswordHash = defaultBcryptHash,

                LegacyPasswordHash = legacyPasswordHash,
                LegacyPasswordAlgo = "MD5",

                BirthYear = j.birth_year.Value,
                CreatedAt = createdAt,
                Role = role
            };

            batch.Add(user);

            seenEmails.Add(emailKey);
            seenUsernames.Add(userKey);
            seenPhoneNumbers.Add(phoneKey);

            if (batch.Count >= 1000)
                await FlushBatch();
        }

        async Task FlushBatch()
        {
            try
            {
                db.Users.AddRange(batch);
                await db.SaveChangesAsync();

                inserted += batch.Count;

                foreach (var u in batch)
                {
                    var plainEmail = Normalize(enc.Decrypt(u.Email) ?? "");
                    var plainPhone = Normalize(enc.Decrypt(u.PhoneNumber) ?? "");

                    existingEmails.Add(plainEmail);
                    existingUsernames.Add(Normalize(u.Username));
                    existingPhoneNumbers.Add(plainPhone);
                }
            }
            catch (DbUpdateException ex)
            {
                rejected += batch.Count;

                var details = ex.InnerException?.Message ?? ex.Message;

                foreach (var u in batch)
                {
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "DbUpdateException",
                        error = details,
                        record = new { u.Username, u.Name }
                    }));
                }
            }

            finally
            {
                batch.Clear();
                db.ChangeTracker.Clear();
            }
        }

        static string Normalize(string s) => s.Trim().ToLowerInvariant();

        static DateTimeOffset? ParseCreatedAt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var dt))
            {
                return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
            }

            if (DateTimeOffset.TryParse(s.Trim(), out var dto))
                return dto;

            return null;
        }

        static UserRole MapRole(string? role)
            => string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
                ? UserRole.Admin
                : UserRole.Customer;

        static char PeekFirstNonWhitespaceChar(string path)
        {
            using var fs = File.OpenRead(path);
            int b;
            do { b = fs.ReadByte(); } while (b != -1 && char.IsWhiteSpace((char)b));
            return b == -1 ? '\0' : (char)b;
        }
    }
}
