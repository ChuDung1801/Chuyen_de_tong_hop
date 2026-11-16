using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AIRecruitment.Infrastructure.Services;

// Lưu giấy tờ xác minh ngoài wwwroot, tên tệp ngẫu nhiên và chỉ tải qua API Admin.
public sealed class LocalRecruiterVerificationStorage(IConfiguration configuration) : IRecruiterVerificationStorage
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private readonly string _root = configuration["RECRUITER_VERIFICATION_STORAGE_PATH"]
        ?? Path.Combine(AppContext.BaseDirectory, "uploads", "recruiter-verification");

    public async Task<StoredRecruiterDocument> SaveAsync(string originalFileName, string contentType, Stream content, long sizeBytes, CancellationToken cancellationToken)
    {
        if (sizeBytes <= 0 || sizeBytes > MaxBytes)
            throw new ArgumentException("Tài liệu xác minh phải có dung lượng từ 1 byte đến 10 MB.");

        var safeOriginalName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalName).ToLowerInvariant();
        var expectedType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new ArgumentException("Chỉ chấp nhận giấy tờ PDF, JPG hoặc PNG.")
        };
        if (!string.Equals(contentType, expectedType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Định dạng nội dung tệp không khớp với phần mở rộng.");

        await using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        if (memory.Length != sizeBytes || memory.Length > MaxBytes)
            throw new ArgumentException("Dung lượng tài liệu không hợp lệ.");

        var bytes = memory.ToArray();
        var validSignature = extension switch
        {
            ".pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            _ => false
        };
        if (!validSignature) throw new ArgumentException("Tệp không có chữ ký định dạng hợp lệ hoặc không phải tài liệu được hỗ trợ.");

        Directory.CreateDirectory(_root);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_root, storedName);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return new StoredRecruiterDocument(storedName, expectedType, bytes.LongLength);
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsSafeStoredName(storedFileName)) return Task.FromResult<Stream?>(null);
        var path = Path.Combine(_root, storedFileName);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsSafeStoredName(storedFileName))
        {
            var path = Path.Combine(_root, storedFileName);
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static bool IsSafeStoredName(string name)
    {
        var file = Path.GetFileName(name);
        var extension = Path.GetExtension(file).ToLowerInvariant();
        return file == name && (extension is ".pdf" or ".jpg" or ".jpeg" or ".png")
            && Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out _);
    }
}
