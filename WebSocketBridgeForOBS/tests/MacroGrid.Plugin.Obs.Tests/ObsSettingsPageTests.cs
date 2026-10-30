using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs.Tests;

public sealed class ObsSettingsPageTests
{
    [Fact]
    public void The_independence_notice_carries_its_text_in_the_description()
    {
        // The editor draws a Notice field from Description; text in the label alone showed an empty warning box.
        var notice = new ObsSettingsPage(new FakePluginHost(Path.GetTempPath()), null!).Fields.Single(f => f.Kind == SettingFieldKind.Notice);

        Assert.Equal(ObsSettingsPage.Disclaimer, notice.Description);
    }
}
