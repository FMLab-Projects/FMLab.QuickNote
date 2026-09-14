using System.Text;
using System.Text.RegularExpressions;

namespace FMLab.QuickNote.Core.Editor;

/// <summary>
/// Lógica pura (sem dependência de UI) do comportamento do editor de texto estruturado:
/// indentação, continuação de listas com marcador e alternância de checkbox.
/// Opera sobre a representação "de tela" do texto, que usa os glyphs ☐/☑ para checkbox
/// (ver <see cref="ToDisplayText"/>/<see cref="ToStorageText"/> para a conversão de/para o
/// formato persistido em disco, que usa a sintaxe "[ ]"/"[x]").
/// </summary>
public static class StructuredTextEditor
{
    public const char UncheckedGlyph = '☐';
    public const char CheckedGlyph = '☑';

    private const string BulletPrefix = "- ";

    private static readonly Regex CheckboxLineRegex =
        new(@"^(?<indent>\t*)- (?<mark>[☐☑]) (?<text>.*)$", RegexOptions.Compiled);

    private static readonly Regex BulletLineRegex =
        new(@"^(?<indent>\t*)- (?<text>.*)$", RegexOptions.Compiled);

    private static readonly Regex StorageCheckboxRegex =
        new(@"(?<pre>^\t*- )\[(?<mark> |x|X)\] ", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex DisplayCheckboxRegex =
        new(@"(?<pre>^\t*- )(?<mark>[☐☑]) ", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Converte do formato persistido ("[ ]"/"[x]") para o formato exibido no editor (☐/☑).</summary>
    public static string ToDisplayText(string storageText) =>
        StorageCheckboxRegex.Replace(storageText, m =>
            m.Groups["pre"].Value + (IsChecked(m.Groups["mark"].Value) ? CheckedGlyph : UncheckedGlyph) + " ");

    /// <summary>Converte do formato exibido no editor (☐/☑) para o formato persistido ("[ ]"/"[x]").</summary>
    public static string ToStorageText(string displayText) =>
        DisplayCheckboxRegex.Replace(displayText, m =>
            m.Groups["pre"].Value + (m.Groups["mark"].Value == CheckedGlyph.ToString() ? "[x]" : "[ ]") + " ");

    private static bool IsChecked(string mark) => mark is "x" or "X";

    /// <summary>Avança o nível de indentação da(s) linha(s) tocada(s) pela seleção (Tab).</summary>
    public static EditResult Indent(string text, int selectionStart, int selectionEnd) =>
        ApplyIndentDelta(text, selectionStart, selectionEnd, insert: true);

    /// <summary>Recua o nível de indentação da(s) linha(s) tocada(s) pela seleção (Shift+Tab).</summary>
    public static EditResult Outdent(string text, int selectionStart, int selectionEnd) =>
        ApplyIndentDelta(text, selectionStart, selectionEnd, insert: false);

    private static EditResult ApplyIndentDelta(string text, int selectionStart, int selectionEnd, bool insert)
    {
        if (selectionStart > selectionEnd)
        {
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);
        }

        var lineStarts = GetLineStarts(text);
        var firstLine = FindLineIndex(lineStarts, selectionStart);
        var lastLine = FindLineIndex(lineStarts, selectionEnd);

        // Seleção que termina exatamente no início de uma linha não deve indentar essa linha
        // (comportamento padrão de editores: só a linha onde o texto realmente começa).
        if (lastLine > firstLine && selectionEnd == lineStarts[lastLine] && selectionStart != selectionEnd)
        {
            lastLine--;
        }

        var builder = new StringBuilder(text);
        var shiftForStart = 0;
        var shiftForEnd = 0;

        for (var i = lastLine; i >= firstLine; i--)
        {
            var lineStart = lineStarts[i];
            if (insert)
            {
                builder.Insert(lineStart, '\t');
                if (lineStart <= selectionStart) shiftForStart++;
                if (lineStart <= selectionEnd) shiftForEnd++;
            }
            else if (lineStart < builder.Length && builder[lineStart] == '\t')
            {
                builder.Remove(lineStart, 1);
                if (lineStart < selectionStart) shiftForStart--;
                if (lineStart < selectionEnd) shiftForEnd--;
            }
        }

        var newStart = Math.Max(0, selectionStart + shiftForStart);
        var newEnd = Math.Max(0, selectionEnd + shiftForEnd);
        return new EditResult(builder.ToString(), newStart, newEnd);
    }

    /// <summary>
    /// Trata a tecla Enter: continua listas com marcador/checkbox automaticamente, sai da lista
    /// quando o item atual está vazio, e insere uma quebra de linha simples nos demais casos.
    /// </summary>
    public static EditResult HandleEnter(string text, int selectionStart, int selectionEnd)
    {
        if (selectionStart > selectionEnd)
        {
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);
        }

        var effectiveText = text.Remove(selectionStart, selectionEnd - selectionStart);
        var caret = selectionStart;

        var lineStarts = GetLineStarts(effectiveText);
        var lineIndex = FindLineIndex(lineStarts, caret);
        var lineStart = lineStarts[lineIndex];
        var lineEnd = GetLineEnd(effectiveText, lineStart);
        var lineContent = effectiveText[lineStart..lineEnd];

        var checkboxMatch = CheckboxLineRegex.Match(lineContent);
        if (checkboxMatch.Success)
        {
            var indent = checkboxMatch.Groups["indent"].Value;
            var itemText = checkboxMatch.Groups["text"].Value;
            return itemText.Trim().Length == 0
                ? ExitList(effectiveText, lineStart, lineEnd, indent)
                : ContinueList(effectiveText, caret, indent, $"{BulletPrefix}{UncheckedGlyph} ");
        }

        var bulletMatch = BulletLineRegex.Match(lineContent);
        if (bulletMatch.Success)
        {
            var indent = bulletMatch.Groups["indent"].Value;
            var itemText = bulletMatch.Groups["text"].Value;
            return itemText.Trim().Length == 0
                ? ExitList(effectiveText, lineStart, lineEnd, indent)
                : ContinueList(effectiveText, caret, indent, BulletPrefix);
        }

        var plain = effectiveText.Insert(caret, "\n");
        return EditResult.Caret(plain, caret + 1);
    }

    private static EditResult ContinueList(string text, int caret, string indent, string prefix)
    {
        var insertion = "\n" + indent + prefix;
        var newText = text.Insert(caret, insertion);
        return EditResult.Caret(newText, caret + insertion.Length);
    }

    private static EditResult ExitList(string text, int lineStart, int lineEnd, string indent)
    {
        var newText = text[..lineStart] + indent + "\n" + text[lineEnd..];
        var newCaret = lineStart + indent.Length + 1;
        return EditResult.Caret(newText, newCaret);
    }

    /// <summary>Alterna o estado do checkbox da linha sob o cursor, se houver. Sem efeito caso contrário.</summary>
    public static EditResult ToggleCheckboxOnLine(string text, int caretIndex)
    {
        var lineStarts = GetLineStarts(text);
        var lineIndex = FindLineIndex(lineStarts, caretIndex);
        var lineStart = lineStarts[lineIndex];
        var lineEnd = GetLineEnd(text, lineStart);
        var lineContent = text[lineStart..lineEnd];

        var match = CheckboxLineRegex.Match(lineContent);
        if (!match.Success)
        {
            return EditResult.Caret(text, caretIndex);
        }

        var markGroup = match.Groups["mark"];
        var markIndex = lineStart + markGroup.Index;
        var newMark = markGroup.Value == CheckedGlyph.ToString() ? UncheckedGlyph : CheckedGlyph;

        var builder = new StringBuilder(text);
        builder[markIndex] = newMark;
        return EditResult.Caret(builder.ToString(), caretIndex);
    }

    /// <summary>
    /// Alterna o checkbox da linha em <paramref name="caretIndex"/> apenas se essa posição cair
    /// sobre (ou logo antes/depois d)o glyph — usado para o toggle por clique do mouse.
    /// </summary>
    public static EditResult ToggleCheckboxNearColumn(string text, int caretIndex)
    {
        var lineStarts = GetLineStarts(text);
        var lineIndex = FindLineIndex(lineStarts, caretIndex);
        var lineStart = lineStarts[lineIndex];
        var lineEnd = GetLineEnd(text, lineStart);
        var lineContent = text[lineStart..lineEnd];

        var match = CheckboxLineRegex.Match(lineContent);
        if (!match.Success)
        {
            return EditResult.Caret(text, caretIndex);
        }

        var markGroup = match.Groups["mark"];
        var column = caretIndex - lineStart;
        var withinGlyph = column >= markGroup.Index && column <= markGroup.Index + markGroup.Length;
        return withinGlyph ? ToggleCheckboxOnLine(text, caretIndex) : EditResult.Caret(text, caretIndex);
    }

    private static int[] GetLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }

    private static int GetLineEnd(string text, int lineStart)
    {
        var nextNewline = text.IndexOf('\n', lineStart);
        return nextNewline < 0 ? text.Length : nextNewline;
    }

    private static int FindLineIndex(int[] lineStarts, int position)
    {
        var index = Array.BinarySearch(lineStarts, position);
        if (index >= 0)
        {
            return index;
        }

        var insertionPoint = ~index;
        return Math.Max(0, insertionPoint - 1);
    }
}
