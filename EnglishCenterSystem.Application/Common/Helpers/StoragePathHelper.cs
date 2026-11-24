using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using EnglishCenterSystem.Application.Common.Enums;

namespace EnglishCenterSystem.Application.Common.Helpers;

public static class StoragePathHelper
{
    public static string GetModuleFolder(
        ImportStorageModule module)
    {
        return module switch
        {
            ImportStorageModule.Exercise =>
                "exercises",

            ImportStorageModule.Test =>
                "tests",

            ImportStorageModule.Competition =>
                "competitions",

            _ => throw new ArgumentOutOfRangeException(
                nameof(module),
                module,
                "Unsupported storage module")
        };
    }

    public static string BuildResourceFolder(
        string? title,
        string fallbackName)
    {
        var source =
            string.IsNullOrWhiteSpace(title)
                ? fallbackName
                : title.Trim();

        var normalized =
            source.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug =
            builder.ToString()
                .Normalize(
                    NormalizationForm.FormC)
                .ToLowerInvariant();

        slug = slug.Replace(
            'đ',
            'd');

        slug = Regex.Replace(
            slug,
            @"[^a-z0-9]+",
            "-");

        slug = slug.Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = fallbackName;
        }

        return slug;
    }

    public static string SanitizeFileName(
        string fileName)
    {
        var safeFileName =
            Path.GetFileName(fileName);

        var extension =
            Path.GetExtension(safeFileName);

        var nameWithoutExtension =
            Path.GetFileNameWithoutExtension(
                safeFileName);

        var safeName =
            BuildResourceFolder(
                nameWithoutExtension,
                "file");

        return $"{safeName}{extension.ToLowerInvariant()}";
    }

    public static string GetDocumentContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
