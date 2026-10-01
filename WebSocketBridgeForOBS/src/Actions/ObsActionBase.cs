using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

/// <summary>Common shape of every OBS action: the shared category, the connection, and the small
/// setting readers the actions repeat. Concrete actions supply their type id, labels, icon and, where they
/// have any, settings fields.</summary>
public abstract class ObsActionBase(ObsConnection obs) : IActionHandler, IActionDescriptor, IActionOutcomeHandler
{
    protected ObsConnection Obs { get; } = obs;

    public abstract string Type { get; }
    public abstract string DisplayName { get; }
    public string Category => "OBS";
    public virtual string? Description => null;
    public abstract string? Icon { get; }
    public virtual IReadOnlyList<SettingField> Fields => [];

    public abstract Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken);

    /// <summary>Runs the action and says what happened: the exceptions the plugin throws on purpose become coded failures; anything else
    /// propagates and the host reports it as a provider error.</summary>
    public async Task<ActionOutcome> ExecuteWithOutcomeAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(context, settings, cancellationToken);
            return ActionOutcome.Success;
        }
        catch (ObsNotConnectedException ex) { return ActionOutcome.Failed(ActionFailureCode.NotConnected, ex.Message); }
        catch (IOException) { return ActionOutcome.Failed(ActionFailureCode.NotConnected, "Not connected to OBS."); }
        catch (ObsNotConfiguredException ex) { return ActionOutcome.Failed(ActionFailureCode.NotConfigured, ex.Message); }
        catch (ObsTargetMissingException ex) { return ActionOutcome.Failed(ActionFailureCode.NotFound, ex.Message); }
        catch (TimeoutException ex) { return ActionOutcome.Failed(ActionFailureCode.Timeout, ex.Message); }
        catch (ObsRequestException ex) { return ActionOutcome.Failed(ActionFailureCode.ProviderRejected, ex.Comment); }
    }

    protected static string? GetString(JsonObject settings, string key) => settings[key]?.GetValue<string>();

    /// <summary>Reads the "inputName" setting. Throws when it is empty or when the input no longer exists in OBS.</summary>
    protected bool TryGetAudioInput(JsonObject settings, out string inputName)
    {
        inputName = GetString(settings, "inputName") ?? "";
        if (inputName.Length == 0) throw new ObsNotConfiguredException("No audio source is chosen.");
        ObsTargetCheck.RequireAudioInput(Obs, inputName);
        return true;
    }
}

/// <summary>An action with dropdown fields: answers <see cref="IOptionsSource"/> from the connection's cache.</summary>
public abstract class ObsOptionsAction(ObsConnection obs) : ObsActionBase(obs), IOptionsSource
{
    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(Obs, sourceId, currentValues);
}

/// <summary>An action that sends one fixed request without data (start/stop/toggle stream or recording, and so on).</summary>
public abstract class ObsRequestAction(ObsConnection obs, string requestType) : ObsActionBase(obs)
{
    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) =>
        Obs.RequestAsync(requestType, null, cancellationToken);
}

/// <summary>An action with a "mode" setting that picks one of two explicit requests or a toggle request.
/// Settings: { "mode": first | second | "toggle" }, the default is "toggle".</summary>
public abstract class ObsModeAction(
    ObsConnection obs,
    (string Mode, string Label, string Request) first,
    (string Mode, string Label, string Request) second,
    string toggleRequest) : ObsActionBase(obs)
{
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("mode", "Mode", SettingFieldKind.Segmented)
        {
            Options = [new(first.Mode, first.Label), new(second.Mode, second.Label), new("toggle", "Toggle")],
            Default = "toggle",
        },
    ];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var mode = GetString(settings, "mode") ?? "toggle";
        var requestType = mode == first.Mode ? first.Request : mode == second.Mode ? second.Request : toggleRequest;
        return Obs.RequestAsync(requestType, null, cancellationToken);
    }

    public static JsonObject Settings(string mode) => new() { ["mode"] = mode };
}
