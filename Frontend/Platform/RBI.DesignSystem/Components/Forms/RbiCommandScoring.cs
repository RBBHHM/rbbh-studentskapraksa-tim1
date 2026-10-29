namespace RBI.DesignSystem.Components.Forms;

// Native adaptation of cmdk 1.1.1's standalone command-score function.
// Copyright (c) 2022 Paco Coursey; MIT notice is in THIRD-PARTY-NOTICES.txt.
internal static class RbiCommandScoring
{
    private const string Boundaries = "\\/_+.#\"@[({&";

    internal static double Score(string value, string query, IReadOnlyList<string> aliases)
    {
        if (aliases.Count > 0) value += " " + string.Join(' ', aliases.Select(Trim));
        var lowerValue = Normalize(value);
        var lowerQuery = Normalize(query);
        var memo = new Dictionary<(int Value, int Query), double>();
        return Match(0, 0);

        double Match(int valueIndex, int queryIndex)
        {
            if (queryIndex == query.Length) return valueIndex == value.Length ? 1 : .99;
            if (queryIndex > query.Length) return 0;
            if (memo.TryGetValue((valueIndex, queryIndex), out var cached)) return cached;
            var best = 0d;
            for (var position = lowerValue.IndexOf(lowerQuery[queryIndex], valueIndex); position >= 0; position = lowerValue.IndexOf(lowerQuery[queryIndex], position + 1))
            {
                var score = Match(position + 1, queryIndex + 1);
                if (score > best)
                {
                    if (position == valueIndex) { }
                    else if (Boundary(value[position - 1]))
                    {
                        score *= .8;
                        if (valueIndex > 0) score *= Math.Pow(.999, Count(valueIndex, position - 1, Boundary));
                    }
                    else if (WordBoundary(value[position - 1]))
                    {
                        score *= .9;
                        if (valueIndex > 0) score *= Math.Pow(.999, Count(valueIndex, position - 1, WordBoundary));
                    }
                    else
                    {
                        score *= .17;
                        if (valueIndex > 0) score *= Math.Pow(.999, position - valueIndex);
                    }
                    if (value[position] != query[queryIndex]) score *= .9999;
                }
                var next = queryIndex + 1 < lowerQuery.Length ? lowerQuery[queryIndex + 1] : '\0';
                var previous = position > 0 ? lowerValue[position - 1] : '\0';
                if (score < .1 && previous == next || next == lowerQuery[queryIndex] && previous != lowerQuery[queryIndex])
                    score = Math.Max(score, Match(position + 1, queryIndex + 2) * .1);
                best = Math.Max(best, score);
            }
            memo[(valueIndex, queryIndex)] = best;
            return best;
        }

        int Count(int from, int to, Func<char, bool> predicate)
        {
            var count = 0;
            for (var index = from; index < to; index++) if (predicate(value[index])) count++;
            return count;
        }
    }

    private static bool Boundary(char character) => Boundaries.Contains(character);
    // Match ECMAScript \s, not .NET whitespace (NEL is excluded; BOM is included).
    private static bool WordBoundary(char character) => character is '-' or '\t' or '\n' or '\v' or '\f' or '\r' or ' ' or '\u00a0' or '\u1680' or >= '\u2000' and <= '\u200a' or '\u2028' or '\u2029' or '\u202f' or '\u205f' or '\u3000' or '\ufeff';
    internal static string Trim(string value)
    {
        static bool Space(char character) => character != '-' && WordBoundary(character);
        var start = 0;
        var end = value.Length;
        while (start < end && Space(value[start])) start++;
        while (end > start && Space(value[end - 1])) end--;
        return value[start..end];
    }
    private static string Normalize(string value) => string.Concat(value.ToLowerInvariant().Select(character => WordBoundary(character) ? ' ' : character));
}
