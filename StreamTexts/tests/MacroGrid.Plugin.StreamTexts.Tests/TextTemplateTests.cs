namespace MacroGrid.Plugin.StreamTexts.Tests;

public class TextTemplateTests
{
    private static string Render(string template, params (string Name, object? Value)[] vars)
    {
        var store = new FakeVariableStore();
        foreach (var (name, value) in vars) store.Set(name, value);
        return TextTemplate.Render(template, store);
    }

    [Fact]
    public void Plain_text_is_unchanged() => Assert.Equal("hello", Render("hello"));

    [Fact]
    public void Number_uses_the_format_and_invariant_culture() =>
        Assert.Equal("37.5%", Render("{cpu|0.0}%", ("cpu", 37.54)));

    [Fact]
    public void Number_without_format_trims_trailing_zeros() =>
        Assert.Equal("12.5", Render("{x}", ("x", 12.5)));

    [Fact]
    public void Missing_variable_is_empty_or_uses_the_placeholder()
    {
        Assert.Equal("[]", Render("[{nope}]"));
        Assert.Equal("[n/a]", Render("[{nope||n/a}]"));
    }

    [Fact]
    public void Bool_format_picks_a_word() =>
        Assert.Equal("LIVE", Render("{live|LIVE/OFF AIR}", ("live", true)));

    [Fact]
    public void Doubled_braces_are_literal_and_backslash_n_is_a_new_line()
    {
        Assert.Equal("{a}", Render("{{a}}"));
        Assert.Equal("one\ntwo", Render("one\\ntwo"));
    }

    [Fact]
    public void Bad_format_does_not_throw() =>
        Assert.Equal("00:00:05", Render("{t|q}", ("t", TimeSpan.FromSeconds(5))));

    [Fact]
    public void Unmatched_brace_and_empty_token_are_kept() =>
        Assert.Equal("a {} b {c", Render("a {} b {c"));
}
