namespace FMLab.QuickNote.Core.Editor;

/// <summary>
/// Resultado puro de uma operação de edição: o novo texto e a nova seleção/posição do cursor.
/// Quando não há seleção, <see cref="SelectionStart"/> e <see cref="SelectionEnd"/> são iguais.
/// </summary>
public readonly record struct EditResult(string Text, int SelectionStart, int SelectionEnd)
{
    public int CaretIndex => SelectionEnd;

    public static EditResult Caret(string text, int caretIndex) => new(text, caretIndex, caretIndex);
}
