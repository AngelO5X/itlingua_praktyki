namespace TutoringBackend.Api.Common;

/// Nazwy ról używane w całej aplikacji (Identity role names).
/// Trzymane w jednym miejscu, żeby uniknąć literówek
public static class Roles
{
    public const string Admin = "Admin";
    public const string Teacher = "Nauczyciel";
    public const string Student = "Uczen";

    public static readonly string[] All = { Admin, Teacher, Student };
}
