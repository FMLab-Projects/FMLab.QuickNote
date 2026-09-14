using FMLab.QuickNote.Core.Editor;

namespace FMLab.QuickNote.Tests.Editor;

public sealed class StructuredTextEditorTests
{
    [Fact]
    public void Indent_inserts_tab_at_start_of_current_line()
    {
        var result = StructuredTextEditor.Indent("abc", 1, 1);

        Assert.Equal("\tabc", result.Text);
        Assert.Equal(2, result.CaretIndex);
    }

    [Fact]
    public void Indent_applies_to_every_line_touched_by_selection()
    {
        var text = "one\ntwo\nthree";
        var result = StructuredTextEditor.Indent(text, 1, 6); // seleciona parte de "one" e "two"

        Assert.Equal("\tone\n\ttwo\nthree", result.Text);
    }

    [Fact]
    public void Outdent_removes_one_leading_tab()
    {
        var result = StructuredTextEditor.Outdent("\t\tabc", 3, 3);

        Assert.Equal("\tabc", result.Text);
        Assert.Equal(2, result.CaretIndex);
    }

    [Fact]
    public void Outdent_is_noop_when_line_has_no_leading_tab()
    {
        var result = StructuredTextEditor.Outdent("abc", 1, 1);

        Assert.Equal("abc", result.Text);
        Assert.Equal(1, result.CaretIndex);
    }

    [Fact]
    public void Enter_after_bullet_line_continues_the_list()
    {
        var text = "- primeiro";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.Equal("- primeiro\n- ", result.Text);
        Assert.Equal(result.Text.Length, result.CaretIndex);
    }

    [Fact]
    public void Enter_preserves_indentation_when_continuing_bullet_list()
    {
        var text = "\t- item";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.Equal("\t- item\n\t- ", result.Text);
    }

    [Fact]
    public void Enter_on_empty_bullet_item_exits_the_list()
    {
        var text = "- primeiro\n- ";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.Equal("- primeiro\n\n", result.Text);
        Assert.Equal(result.Text.Length, result.CaretIndex);
    }

    [Fact]
    public void Enter_on_plain_line_just_breaks_the_line()
    {
        var result = StructuredTextEditor.HandleEnter("abc", 3, 3);

        Assert.Equal("abc\n", result.Text);
        Assert.Equal(4, result.CaretIndex);
    }

    [Fact]
    public void Enter_after_checkbox_line_continues_with_unchecked_item()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} primeiro";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.Equal($"- {StructuredTextEditor.UncheckedGlyph} primeiro\n- {StructuredTextEditor.UncheckedGlyph} ", result.Text);
    }

    [Fact]
    public void Enter_after_checked_checkbox_line_continues_as_unchecked()
    {
        var text = $"- {StructuredTextEditor.CheckedGlyph} feito";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.EndsWith($"- {StructuredTextEditor.UncheckedGlyph} ", result.Text);
    }

    [Fact]
    public void Enter_on_empty_checkbox_item_exits_the_list()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} feito\n- {StructuredTextEditor.UncheckedGlyph} ";
        var caret = text.Length;

        var result = StructuredTextEditor.HandleEnter(text, caret, caret);

        Assert.Equal($"- {StructuredTextEditor.UncheckedGlyph} feito\n\n", result.Text);
    }

    [Fact]
    public void ToggleCheckboxOnLine_toggles_unchecked_to_checked()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} tarefa";

        var result = StructuredTextEditor.ToggleCheckboxOnLine(text, 4);

        Assert.Equal($"- {StructuredTextEditor.CheckedGlyph} tarefa", result.Text);
    }

    [Fact]
    public void ToggleCheckboxOnLine_toggles_checked_to_unchecked()
    {
        var text = $"- {StructuredTextEditor.CheckedGlyph} tarefa";

        var result = StructuredTextEditor.ToggleCheckboxOnLine(text, 4);

        Assert.Equal($"- {StructuredTextEditor.UncheckedGlyph} tarefa", result.Text);
    }

    [Fact]
    public void ToggleCheckboxOnLine_is_noop_on_non_checkbox_line()
    {
        var text = "linha comum";

        var result = StructuredTextEditor.ToggleCheckboxOnLine(text, 3);

        Assert.Equal(text, result.Text);
    }

    [Fact]
    public void ToggleCheckboxOnLine_uses_caret_line_when_multiple_lines_present()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} um\n- {StructuredTextEditor.UncheckedGlyph} dois";
        var secondLineCaret = text.IndexOf("dois", StringComparison.Ordinal);

        var result = StructuredTextEditor.ToggleCheckboxOnLine(text, secondLineCaret);

        Assert.Equal($"- {StructuredTextEditor.UncheckedGlyph} um\n- {StructuredTextEditor.CheckedGlyph} dois", result.Text);
    }

    [Fact]
    public void ToggleCheckboxNearColumn_toggles_when_clicking_on_the_glyph()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} tarefa";
        var glyphIndex = text.IndexOf(StructuredTextEditor.UncheckedGlyph);

        var result = StructuredTextEditor.ToggleCheckboxNearColumn(text, glyphIndex);

        Assert.Equal($"- {StructuredTextEditor.CheckedGlyph} tarefa", result.Text);
    }

    [Fact]
    public void ToggleCheckboxNearColumn_does_not_toggle_when_clicking_far_into_the_text()
    {
        var text = $"- {StructuredTextEditor.UncheckedGlyph} tarefa longa";

        var result = StructuredTextEditor.ToggleCheckboxNearColumn(text, text.Length - 2);

        Assert.Equal(text, result.Text);
    }

    [Fact]
    public void ToDisplayText_converts_bracket_syntax_to_glyphs()
    {
        var storage = "- [ ] um\n- [x] dois\n- [X] tres";

        var display = StructuredTextEditor.ToDisplayText(storage);

        Assert.Equal(
            $"- {StructuredTextEditor.UncheckedGlyph} um\n- {StructuredTextEditor.CheckedGlyph} dois\n- {StructuredTextEditor.CheckedGlyph} tres",
            display);
    }

    [Fact]
    public void ToStorageText_converts_glyphs_back_to_bracket_syntax()
    {
        var display = $"- {StructuredTextEditor.UncheckedGlyph} um\n- {StructuredTextEditor.CheckedGlyph} dois";

        var storage = StructuredTextEditor.ToStorageText(display);

        Assert.Equal("- [ ] um\n- [x] dois", storage);
    }

    [Fact]
    public void ToDisplayText_and_ToStorageText_roundtrip()
    {
        var storage = "titulo\n- [ ] tarefa\n\tsub item\n- [x] feita";

        var roundtripped = StructuredTextEditor.ToStorageText(StructuredTextEditor.ToDisplayText(storage));

        Assert.Equal(storage, roundtripped);
    }

    [Fact]
    public void ToDisplayText_does_not_touch_plain_bullet_lines()
    {
        var storage = "- item sem checkbox";

        Assert.Equal(storage, StructuredTextEditor.ToDisplayText(storage));
    }
}
