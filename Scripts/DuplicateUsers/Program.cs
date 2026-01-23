using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    [JsonConverter(typeof(FlexibleNullableIntConverter))]
    public int? birth_year { get; set; }

    public bool? active { get; set; }
}

public sealed class FlexibleNullableIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i)) return i;
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (int.TryParse(s, out var i)) return i;
            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}

public interface IEncryptionService
{
    string? Encrypt(string? plaintext);
    string? Decrypt(string? ciphertext);
}

public sealed class AesGcmEncryptionService : IEncryptionService
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
        return ciphertext;
    }
}

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var jsonlPath = args.Length > 0 ? args[0] : "";
        if (string.IsNullOrWhiteSpace(jsonlPath) || !File.Exists(jsonlPath))
        {
            Console.WriteLine("Usage: dotnet run -- <path-to-duplicates.jsonl>");
            return 1;
        }

        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        if (!File.Exists(appSettingsPath))
            appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            Console.WriteLine("Missing appsettings.json next to the tool.");
            return 1;
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(appSettingsPath)!)
            .AddJsonFile(appSettingsPath, optional: false)
            .AddEnvironmentVariables()
            .Build();

        var conn = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Console.WriteLine("Missing ConnectionStrings:DefaultConnection");
            return 1;
        }

        var enc = new AesGcmEncryptionService(config);

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(conn)
            .Options;

        await using var db = new UserDbContext(options);

        Console.WriteLine($"✅ Connected database: {db.Database.GetDbConnection().Database}");
        Console.WriteLine($"✅ Users in DB: {await db.Users.AsNoTracking().CountAsync()}");
        Console.WriteLine($"✅ DuplicateUsers in DB: {await db.DuplicateUsers.AsNoTracking().CountAsync()}");

        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var rejectPath = Path.ChangeExtension(jsonlPath, ".rejected.jsonl");
        await using var rejectWriter = new StreamWriter(rejectPath);

        var inserted = 0;
        var rejected = 0;

        var batch = new List<DuplicateUser>(1000);

        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonOpts.Converters.Add(new FlexibleNullableIntConverter());

        var fallbackReason = GetReasonFromFileName(jsonlPath);

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                if (!TryExtractUserJson(line, out var recordJson, out var reasonFromLine))
                {
                    rejected++;
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "Invalid split line format",
                        line
                    }));
                    continue;
                }

                var record = JsonSerializer.Deserialize<UserJson>(recordJson, jsonOpts);
                if (record is null)
                {
                    rejected++;
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "Record deserialized as null",
                        recordJson
                    }));
                    continue;
                }

                var username = (record.username ?? "").Trim().ToLowerInvariant();
                var name = (record.name ?? "").Trim();
                var email = (record.email ?? "").Trim();
                var phone = (record.phone ?? "").Trim();

                if (username.Length == 0 || name.Length == 0 || email.Length == 0 || phone.Length == 0)
                {
                    rejected++;
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "Missing required field(s)",
                        record
                    }));
                    continue;
                }

                if (username.Length > 40 || name.Length > 70)
                {
                    rejected++;
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "Field too long (Username>40 or Name>70)",
                        record
                    }));
                    continue;
                }

                var createdAt = ParseCreatedAt(record.created_at) ?? DateTimeOffset.UtcNow;
                var role = MapRole(record.role);

                var finalReason = !string.IsNullOrWhiteSpace(reasonFromLine)
                    ? reasonFromLine
                    : fallbackReason;

                if (finalReason == "Unknown")
                    finalReason = "DuplicateUsername+Email";

                var dupe = new DuplicateUser
                {
                    Id = Guid.NewGuid(),
                    LegacyId = record.id,

                    Username = username,
                    Name = name,

                    Email = enc.Encrypt(email) ?? "",
                    PhoneNumber = enc.Encrypt(phone) ?? "",

                    BirthYear = record.birth_year,
                    CreatedAt = createdAt,
                    Role = role,

                    LegacyPasswordHash = (record.password ?? "").Trim(),
                    LegacyPasswordAlgo = "MD5",

                    DuplicateReason = finalReason,
                    ImportedAt = DateTimeOffset.UtcNow,

                    OriginalJson = recordJson
                };

                batch.Add(dupe);

                if (batch.Count >= 1000)
                    await FlushBatch();
            }
            catch (Exception ex)
            {
                rejected++;
                await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    reason = "Exception while processing line",
                    error = ex.Message,
                    line
                }));
            }
        }

        if (batch.Count > 0)
            await FlushBatch();

        db.ChangeTracker.AutoDetectChangesEnabled = true;

        Console.WriteLine($"✅ Done. Inserted={inserted}, Rejected={rejected}");
        Console.WriteLine($"Rejected log: {rejectPath}");
        return 0;

        async Task FlushBatch()
        {
            try
            {
                db.DuplicateUsers.AddRange(batch);
                await db.SaveChangesAsync();
                inserted += batch.Count;
            }
            catch (DbUpdateException ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;

                rejected += batch.Count;
                foreach (var b in batch)
                {
                    await rejectWriter.WriteLineAsync(JsonSerializer.Serialize(new
                    {
                        reason = "DbUpdateException",
                        error = details,
                        record = new { b.Username, b.Name, b.DuplicateReason }
                    }));
                }
            }
            finally
            {
                batch.Clear();
                db.ChangeTracker.Clear();
            }
        }
    }

    private static bool TryExtractUserJson(string line, out string recordJson, out string? reason)
    {
        recordJson = "";
        reason = null;

        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        if (root.TryGetProperty("duplicateFields", out var df))
        {
            bool u = df.TryGetProperty("username", out var uEl) && uEl.ValueKind == JsonValueKind.True;
            bool e = df.TryGetProperty("email", out var eEl) && eEl.ValueKind == JsonValueKind.True;
            bool p = df.TryGetProperty("phone", out var pEl) && pEl.ValueKind == JsonValueKind.True;

            reason = BuildReason(u, e, p);
        }

        if (root.TryGetProperty("original", out var original))
        {
            if (original.TryGetProperty("record", out var record))
            {
                recordJson = record.GetRawText();
                return true;
            }

            if (original.TryGetProperty("username", out _))
            {
                recordJson = original.GetRawText();
                return true;
            }
        }

        if (root.TryGetProperty("record", out var record2))
        {
            recordJson = record2.GetRawText();
            return true;
        }

        if (root.TryGetProperty("username", out _))
        {
            recordJson = root.GetRawText();
            return true;
        }

        return false;
    }

    private static string BuildReason(bool username, bool email, bool phone)
    {
        var parts = new List<string>();
        if (username) parts.Add("Username");
        if (email) parts.Add("Email");
        if (phone) parts.Add("Phone");

        return parts.Count == 0 ? "Unknown" : "Duplicate" + string.Join("+", parts);
    }

    private static string GetReasonFromFileName(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();

        if (name.Contains("by_username")) return "DuplicateUsername";
        if (name.Contains("by_email")) return "DuplicateEmail";
        if (name.Contains("by_phone")) return "DuplicatePhone";
        if (name.Contains("unknown")) return "DuplicateUsername+Email";

        return "Unknown";
    }

    private static DateTimeOffset? ParseCreatedAt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTime.TryParseExact(
                s.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dt))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
        }

        if (DateTimeOffset.TryParse(s.Trim(), out var dto))
            return dto;

        return null;
    }

    private static UserRole MapRole(string? role)
        => string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Admin
            : UserRole.Customer;
}
