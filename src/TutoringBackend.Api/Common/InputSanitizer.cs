using System.Net;

namespace TutoringBackend.Api.Common;

/// Podstawowa ochrona przed SQL Injection i XSS
public static class InputSanitizer
{
    public static string? EncodeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return WebUtility.HtmlEncode(input.Trim());
    }

    /// Waliduje, że URL jest bezwzględny i używa wyłącznie http/https (blokuje np. javascript:).
    public static bool IsSafeHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".png", ".jpg", ".jpeg", ".gif", ".mp3", ".mp4", ".txt", ".zip"
    };

    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static bool IsAllowedFileExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedFileExtensions.Contains(ext);
    }

    /// Generuje bezpieczną, losową nazwę pliku do zapisu na dysku (bez ścieżek, bez oryginalnej nazwy)
    public static string GenerateSafeStoredFileName(string originalFileName)
    {
        var ext = Path.GetExtension(originalFileName);
        return $"{Guid.NewGuid():N}{ext}";
    }
}
