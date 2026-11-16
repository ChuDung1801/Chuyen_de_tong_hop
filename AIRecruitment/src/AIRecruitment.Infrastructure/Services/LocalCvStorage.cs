using Microsoft.Extensions.Configuration;
using AIRecruitment.Application.Interfaces;

namespace AIRecruitment.Infrastructure.Services;

// ============== 
// TV2 — Lưu CV bằng tên ngẫu nhiên, tránh dùng tên người dùng làm đường dẫn.
// ==============
public sealed class LocalCvStorage(IConfiguration configuration) : ICvStorage
{
    public async Task<(string storedName, string fullPath)> SaveAsync(string originalFileName, Stream content, CancellationToken cancellationToken)
    {
        var root = configuration["CV_STORAGE_PATH"] ?? Path.Combine(AppContext.BaseDirectory, "uploads", "cvs");
        Directory.CreateDirectory(root);
        var extension = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(root, storedName);
        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
        return (storedName, fullPath);
    }
}
