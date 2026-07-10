using System.Net;
using System.Text;

namespace OnlyNines.Web.Services;

/// <summary>
/// Tiny KQL span-tokenizer for the read-only query blocks (design spec, Assess page).
/// Token colors via CSS classes: c-kw, c-str, c-pipe, c-comment; default inherits.
/// </summary>
public static class KqlHighlighter
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "resources", "where", "project", "type", "in~", "summarize", "extend",
        "tolower", "tostring", "hash", "hash_sha256",
    };

    public static string Highlight(string code)
    {
        var sb = new StringBuilder(code.Length * 2);
        var i = 0;
        while (i < code.Length)
        {
            var c = code[i];
            if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
            {
                var j = code.IndexOf('\n', i);
                if (j < 0) j = code.Length;
                Append(sb, "c-comment", code[i..j]);
                i = j;
            }
            else if (c == '\'')
            {
                var j = code.IndexOf('\'', i + 1);
                j = j < 0 ? code.Length - 1 : j;
                Append(sb, "c-str", code[i..(j + 1)]);
                i = j + 1;
            }
            else if (c == '|')
            {
                Append(sb, "c-pipe", "|");
                i++;
            }
            else if (char.IsLetter(c))
            {
                var j = i;
                while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] is '~' or '_')) j++;
                var word = code[i..j];
                if (Keywords.Contains(word)) Append(sb, "c-kw", word);
                else sb.Append(WebUtility.HtmlEncode(word));
                i = j;
            }
            else
            {
                sb.Append(WebUtility.HtmlEncode(c.ToString()));
                i++;
            }
        }
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string cls, string text) =>
        sb.Append("<span class=\"").Append(cls).Append("\">")
          .Append(WebUtility.HtmlEncode(text)).Append("</span>");
}
