using System.Globalization;
using System.Text;

namespace VocabularyService.Helpers;

/// <summary>Opaque cursor для keyset-пагинации списка терминов: (UserTermStatus.UpdatedAt DESC, ProjectTerm.Id ASC).</summary>
public static class TermListCursorCodec
{
    public static string Encode(DateTime updatedAtUtc, Guid termId)
    {
        var payload = $"{updatedAtUtc.ToString("o", CultureInfo.InvariantCulture)}|{termId:D}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
    }

    public static (DateTime UpdatedAtUtc, Guid TermId) Decode(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            throw new ArgumentException("cursor empty", nameof(cursor));

        var bytes = Base64UrlDecode(cursor.Trim());
        var payload = Encoding.UTF8.GetString(bytes);
        var sep = payload.IndexOf('|', StringComparison.Ordinal);
        if (sep <= 0 || sep >= payload.Length - 1)
            throw new FormatException("invalid cursor payload");

        var timePart = payload[..sep];
        var idPart = payload[(sep + 1)..];
        if (!DateTime.TryParse(timePart, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t))
            throw new FormatException("invalid cursor time");
        if (!Guid.TryParse(idPart, out var id))
            throw new FormatException("invalid cursor id");

        return (DateTime.SpecifyKind(t, DateTimeKind.Utc), id);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        var s = Convert.ToBase64String(data);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        return Convert.FromBase64String(s);
    }
}
