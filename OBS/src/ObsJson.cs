using System.Text.Json;
using System.Text.Json.Nodes;

namespace MacroGrid.Plugin.Obs;

/// <summary>Safe field readers for obs-websocket JSON payloads — a message with an unexpected shape (a
/// string where a number was expected, a missing field) returns the fallback instead of throwing, so one
/// malformed event never kills the receive loop.</summary>
public static class ObsJson
{
    public static int TryGetInt(this JsonObject? obj, string key, int fallback = 0)
    {
        if (obj?[key] is not JsonValue v) return fallback;
        return v.TryGetValue(out int i) ? i : (v.TryGetValue(out double d) ? (int)d : fallback);
    }

    public static double TryGetDouble(this JsonObject? obj, string key, double fallback = 0)
    {
        if (obj?[key] is not JsonValue v) return fallback;
        return v.TryGetValue(out double d) ? d : fallback;
    }

    public static bool TryGetBool(this JsonObject? obj, string key, bool fallback = false)
    {
        if (obj?[key] is not JsonValue v) return fallback;
        return v.TryGetValue(out bool b) ? b : fallback;
    }

    public static string? TryGetString(this JsonObject? obj, string key, string? fallback = null)
    {
        if (obj?[key] is not JsonValue v) return fallback;
        return v.TryGetValue(out string? s) ? s : fallback;
    }

    public static JsonArray? TryGetArray(this JsonObject? obj, string key) => obj?[key] as JsonArray;

    public static JsonObject? TryGetObject(this JsonObject? obj, string key) => obj?[key] as JsonObject;
}
