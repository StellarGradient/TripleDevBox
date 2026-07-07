using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace TripleDevBox.Controls;

public sealed partial class JsonEscaper : UserControl
{
    public JsonEscaper()
    {
        InitializeComponent();
    }

    private void Escape_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = Escape(InputBox.Text ?? string.Empty);
        StatusText.Text = "Escaped.";
    }

    private void Unescape_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = Unescape(InputBox.Text ?? string.Empty);
        StatusText.Text = "Unescaped.";
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(OutputBox.Text))
        {
            StatusText.Text = "Nothing to copy.";
            return;
        }

        var package = new DataPackage();
        package.SetText(OutputBox.Text);
        Clipboard.SetContent(package);
        StatusText.Text = "Output copied.";
    }

    private void Swap_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = OutputBox.Text;
        OutputBox.Text = string.Empty;
        StatusText.Text = "Moved output to input.";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = string.Empty;
        OutputBox.Text = string.Empty;
        StatusText.Text = string.Empty;
    }

    // Escape raw text into JSON string content (no surrounding quotes), matching
    // the freeformatter.com JSON-escape behavior: backslash, double-quote, the
    // named control escapes (\b \f \n \r \t), and \uXXXX for other control
    // characters. Non-ASCII characters and forward slashes are left as-is.
    private static string Escape(string input)
    {
        var sb = new StringBuilder(input.Length + 16);
        foreach (char c in input)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // Reverse of Escape. Tolerant of input with or without surrounding quotes.
    // Unknown backslash sequences are preserved verbatim rather than dropped.
    private static string Unescape(string input)
    {
        string s = input;

        // Strip a single pair of enclosing double quotes if present.
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            s = s[1..^1];

        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c != '\\' || i + 1 >= s.Length)
            {
                sb.Append(c);
                continue;
            }

            char next = s[++i];
            switch (next)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (i + 4 < s.Length &&
                        ushort.TryParse(s.Substring(i + 1, 4), NumberStyles.HexNumber,
                                        CultureInfo.InvariantCulture, out ushort code))
                    {
                        sb.Append((char)code);
                        i += 4;
                    }
                    else
                    {
                        sb.Append("\\u");
                    }
                    break;
                default:
                    // Not a recognized escape: keep the backslash and the char.
                    sb.Append('\\').Append(next);
                    break;
            }
        }
        return sb.ToString();
    }
}
