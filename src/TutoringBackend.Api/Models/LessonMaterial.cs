namespace TutoringBackend.Api.Models;

/// Materiał (link lub plik) dopięty do konkretnej lekcji.
/// Dla linków: Url musi być poprawnym, bezwzględnym adresem http/https (walidacja w DTO).
/// Dla plików: przechowujemy tylko wygenerowaną nazwę (StoredFileName) - nigdy oryginalnej
/// nazwy w ścieżce na dysku (ochrona przed path traversal), oryginalna nazwa tylko do wyświetlenia.
public class LessonMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public MaterialType Type { get; set; }

    public string? Url { get; set; }

    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public long? FileSizeBytes { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public Guid AddedByUserId { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}
