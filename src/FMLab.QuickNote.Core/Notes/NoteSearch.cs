using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FMLab.QuickNote.Core.Notes;

/// <summary>
/// Busca por aproximação (não exata) sobre título + conteúdo: normaliza texto (minúsculas + sem
/// acentos) e casa por token, tolerando pequenos typos via distância de edição — sem depender de
/// lib externa. Uma nota só entra no resultado se <b>todos</b> os tokens da query tiverem algum
/// token correspondente (exato/substring ou próximo o bastante) no texto da nota.
/// </summary>
public static class NoteSearch
{
    private static readonly Regex TokenPattern = new(@"[\p{L}\p{Nd}]+", RegexOptions.Compiled);

    /// <summary>
    /// Retorna as notas que casam com <paramref name="query"/>, ordenadas por relevância
    /// aproximada (match mais forte primeiro). Query vazia/só espaços retorna
    /// <paramref name="notes"/> sem reordenar (mantém a ordenação recebida, ex.: <c>UpdatedAt desc</c>).
    /// </summary>
    public static IReadOnlyList<Note> Search(IReadOnlyList<Note> notes, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return notes;
        }

        var queryTokens = Tokenize(Normalize(query));
        if (queryTokens.Count == 0)
        {
            return notes;
        }

        var scored = new List<(Note Note, double Score)>();
        foreach (var note in notes)
        {
            var haystackTokens = Tokenize(Normalize($"{note.Title} {note.Content}"));
            if (haystackTokens.Count == 0)
            {
                continue;
            }

            var totalScore = 0.0;
            var matchedEveryToken = true;
            foreach (var queryToken in queryTokens)
            {
                var bestScore = BestTokenScore(queryToken, haystackTokens);
                if (bestScore is null)
                {
                    matchedEveryToken = false;
                    break;
                }

                totalScore += bestScore.Value;
            }

            if (matchedEveryToken)
            {
                scored.Add((note, totalScore / queryTokens.Count));
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .Select(s => s.Note)
            .ToList();
    }

    private static double? BestTokenScore(string queryToken, IReadOnlyList<string> haystackTokens)
    {
        double? best = null;
        foreach (var token in haystackTokens)
        {
            var score = TokenScore(queryToken, token);
            if (score is { } value && (best is null || value > best))
            {
                best = value;
            }
        }

        return best;
    }

    private static double? TokenScore(string queryToken, string haystackToken)
    {
        if (haystackToken.Contains(queryToken, StringComparison.Ordinal))
        {
            return 1.0;
        }

        var distance = LevenshteinDistance(queryToken, haystackToken);
        var maxAllowedDistance = queryToken.Length <= 4 ? 1 : 2;

        return distance <= maxAllowedDistance
            ? 1.0 - (double)distance / (queryToken.Length + 1)
            : null;
    }

    private static List<string> Tokenize(string normalizedText) =>
        TokenPattern.Matches(normalizedText).Select(m => m.Value).ToList();

    private static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var distances = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            distances[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[a.Length, b.Length];
    }
}
