using System.Globalization;
using System.Text;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.StreamTexts;

/// <summary>Renders a text file's template against the variable store. Same token syntax as a widget's text in Macro Grid:
/// <c>{name}</c>, <c>{name|format}</c>, <c>{name|format|placeholder}</c> (the placeholder is written while the variable has no
/// value) and <c>{{</c>/<c>}}</c> for a literal brace. One addition: a literal <c>\n</c> in the template becomes a line break, since
/// the settings field is a single line. Numbers default to <c>0.##</c>; everything is formatted with the invariant culture so a
/// file reads the same on every PC.</summary>
public static class TextTemplate
{
    private const int MaxPlaceholderLength = 64;

    public static string Render(string template, IVariableStore store)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if (c == '{' && i + 1 < template.Length && template[i + 1] == '{') { sb.Append('{'); i += 2; continue; }
            if (c == '}' && i + 1 < template.Length && template[i + 1] == '}') { sb.Append('}'); i += 2; continue; }
            if (c == '\\' && i + 1 < template.Length && template[i + 1] == 'n') { sb.Append('\n'); i += 2; continue; }

            if (c == '{')
            {
                var end = template.IndexOf('}', i + 1);
                if (end < 0) { sb.Append(template, i, template.Length - i); break; } // unmatched '{' -> literal rest

                var inner = template[(i + 1)..end];
                var pipe = inner.IndexOf('|');
                var name = (pipe < 0 ? inner : inner[..pipe]).Trim();
                if (name.Length == 0) { sb.Append(template, i, end - i + 1); i = end + 1; continue; } // "{}" -> literal

                string? format = null, placeholder = null;
                if (pipe >= 0)
                {
                    var rest = inner[(pipe + 1)..];
                    var second = rest.IndexOf('|');
                    format = second < 0 ? rest : rest[..second];
                    if (second >= 0)
                    {
                        placeholder = rest[(second + 1)..];
                        if (placeholder.Length > MaxPlaceholderLength) placeholder = placeholder[..MaxPlaceholderLength];
                    }
                }

                var value = store.Get(name);
                sb.Append(value is null && placeholder is not null ? placeholder : Format(value, format));
                i = end + 1;
                continue;
            }

            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static string Format(object? value, string? format)
    {
        try
        {
            return value switch
            {
                null => "",
                double or float or int or long => Convert.ToDouble(value, CultureInfo.InvariantCulture)
                    .ToString(string.IsNullOrEmpty(format) ? "0.##" : format, CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString(string.IsNullOrEmpty(format) ? "HH:mm" : format, CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString(string.IsNullOrEmpty(format) ? "HH:mm" : format, CultureInfo.InvariantCulture),
                TimeSpan ts => FormatTimeSpan(ts, format),
                bool b => FormatBool(b, format),
                _ => value.ToString() ?? "",
            };
        }
        catch (FormatException)
        {
            // A typo in a user-typed format must not stop the file from updating: fall back to the plain value.
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }
    }

    private static string FormatTimeSpan(TimeSpan ts, string? format)
    {
        if (!string.IsNullOrEmpty(format)) return ts.ToString(format, CultureInfo.InvariantCulture);
        return ts.ToString(ts.Days != 0 ? @"d\.hh\:mm\:ss" : @"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatBool(bool value, string? format)
    {
        if (!string.IsNullOrEmpty(format) && format.Contains('/'))
        {
            var words = format.Split('/', 2);
            return value ? words[0] : words[1];
        }
        return value ? "On" : "Off";
    }
}
