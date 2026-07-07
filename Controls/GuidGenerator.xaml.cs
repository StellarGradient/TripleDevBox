using System;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace TripleDevBox.Controls;

public sealed partial class GuidGenerator : UserControl
{
    // Sample used for the live format preview; matches the example in the spec.
    private static readonly Guid SampleGuid = Guid.Parse("7076b156-8dfb-4092-b60a-8e6b1e7f900f");

    public GuidGenerator()
    {
        InitializeComponent();
        UpdatePreview();
    }

    private bool Uppercase => UppercaseToggle.IsChecked == true;
    private bool Braces => BracesToggle.IsChecked == true;
    private bool Hyphens => HyphensToggle.IsChecked == true;

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        // Wired to three checkboxes; PreviewText only exists after XAML load.
        if (PreviewText is not null) UpdatePreview();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        int count = 1;
        if (!double.IsNaN(CountBox.Value))
        {
            count = (int)Math.Clamp(CountBox.Value, 1, 100000);
        }

        var sb = new StringBuilder(count * 40);
        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(Format(Guid.NewGuid()));
        }

        OutputBox.Text = sb.ToString();
        StatusText.Text = $"Generated {count} GUID{(count == 1 ? string.Empty : "s")}.";
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(OutputBox.Text))
        {
            StatusText.Text = "Nothing to copy.";
            return;
        }

        var package = new DataPackage();
        package.SetText(OutputBox.Text);
        Clipboard.SetContent(package);
        StatusText.Text = "Copied to clipboard.";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = string.Empty;
        StatusText.Text = string.Empty;
    }

    private void UpdatePreview()
    {
        PreviewText.Text = Format(SampleGuid);
    }

    // Build the string for the current toggle combination. Start from the
    // hyphenated ("D") or bare ("N") form, then apply case and braces so every
    // combination is reachable (including no-hyphens-with-braces, which has no
    // built-in Guid format specifier).
    private string Format(Guid guid)
    {
        string text = guid.ToString(Hyphens ? "D" : "N", CultureInfo.InvariantCulture);
        if (Uppercase) text = text.ToUpperInvariant();
        if (Braces) text = "{" + text + "}";
        return text;
    }
}
