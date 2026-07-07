using System;
using System.Globalization;
using Microsoft.UI.Xaml.Controls;

namespace TripleDevBox.Controls;

public sealed partial class TimestampConverter : UserControl
{
    // Editable, parse-friendly format for the human-readable box.
    private const string HumanFormat = "yyyy-MM-dd HH:mm:ss";

    // Any epoch whose magnitude reaches this is treated as milliseconds in
    // auto-detect mode. 1e11 seconds is year ~5138, so real second-timestamps
    // stay below it while modern millisecond-timestamps (~1.7e12) sit above.
    private const long MillisecondThreshold = 100_000_000_000L;

    // Guards against the two text fields updating each other in a loop.
    private bool _suppress;

    // False until construction finishes. The pre-selected ComboBox items raise
    // SelectionChanged during InitializeComponent, before the result TextBlocks
    // further down the XAML exist; the handlers must not run until then.
    private bool _ready;

    // The canonical instant currently represented, if the fields parse.
    private DateTimeOffset? _current;

    public TimestampConverter()
    {
        InitializeComponent();
        _ready = true;
    }

    private enum EpochUnit { Auto, Seconds, Milliseconds }

    private EpochUnit SelectedUnit => UnitCombo?.SelectedIndex switch
    {
        1 => EpochUnit.Seconds,
        2 => EpochUnit.Milliseconds,
        _ => EpochUnit.Auto,
    };

    // ZoneCombo: index 0 = Local, index 1 = UTC.
    private bool UseUtc => ZoneCombo?.SelectedIndex == 1;

    // ---- Event handlers -------------------------------------------------

    private void EpochBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready || _suppress) return;
        UpdateFromEpoch();
    }

    private void HumanBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready || _suppress) return;
        UpdateFromHuman();
    }

    private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Re-interpret whatever is currently in the epoch box under the new unit.
        if (!_ready || _suppress) return;
        UpdateFromEpoch();
    }

    private void ZoneCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The instant did not change; just re-render the human field for the new zone.
        if (!_ready || _suppress || _current is null) return;
        WriteHuman(_current.Value);
    }

    private void NowButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SetInstant(DateTimeOffset.Now, writeEpoch: true, writeHuman: true);
    }

    // ---- Conversion core ------------------------------------------------

    private void UpdateFromEpoch()
    {
        string raw = EpochBox.Text?.Trim() ?? string.Empty;

        if (raw.Length == 0)
        {
            _current = null;
            ClearResults();
            DetectedText.Text = string.Empty;
            return;
        }

        if (!long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
        {
            ShowError("Epoch must be an integer.");
            return;
        }

        EpochUnit unit = SelectedUnit;
        bool asMilliseconds = unit switch
        {
            EpochUnit.Seconds => false,
            EpochUnit.Milliseconds => true,
            _ => Math.Abs(value) >= MillisecondThreshold,
        };

        DateTimeOffset instant;
        try
        {
            instant = asMilliseconds
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            ShowError("Epoch value is out of the representable date range.");
            return;
        }

        DetectedText.Text = unit == EpochUnit.Auto
            ? $"Detected: {(asMilliseconds ? "milliseconds" : "seconds")}"
            : $"Interpreting as {(asMilliseconds ? "milliseconds" : "seconds")}";

        _current = instant;
        WriteHuman(instant);
        ShowResults(instant);
    }

    private void UpdateFromHuman()
    {
        string raw = HumanBox.Text?.Trim() ?? string.Empty;

        if (raw.Length == 0)
        {
            _current = null;
            ClearResults();
            return;
        }

        if (!TryParseHuman(raw, out DateTime parsed))
        {
            ShowError("Could not parse the datetime. Try e.g. 2025-07-06 00:00:00.");
            return;
        }

        DateTimeOffset instant;
        if (UseUtc)
        {
            instant = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
        }
        else
        {
            DateTime unspecified = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(unspecified);
            instant = new DateTimeOffset(unspecified, offset);
        }

        _current = instant;
        WriteEpoch(instant);
        ShowResults(instant);
    }

    // Set everything from a known instant (used by "Now").
    private void SetInstant(DateTimeOffset instant, bool writeEpoch, bool writeHuman)
    {
        _current = instant;
        if (writeEpoch) WriteEpoch(instant);
        if (writeHuman) WriteHuman(instant);
        ShowResults(instant);
    }

    // ---- Field writers (suppress reentrancy) ----------------------------

    private void WriteEpoch(DateTimeOffset instant)
    {
        long epoch = SelectedUnit == EpochUnit.Milliseconds
            ? instant.ToUnixTimeMilliseconds()
            : instant.ToUnixTimeSeconds();

        _suppress = true;
        EpochBox.Text = epoch.ToString(CultureInfo.InvariantCulture);
        DetectedText.Text = SelectedUnit == EpochUnit.Milliseconds
            ? "Interpreting as milliseconds"
            : "Interpreting as seconds";
        _suppress = false;
    }

    private void WriteHuman(DateTimeOffset instant)
    {
        DateTime shown = UseUtc ? instant.UtcDateTime : instant.LocalDateTime;

        _suppress = true;
        HumanBox.Text = shown.ToString(HumanFormat, CultureInfo.InvariantCulture);
        _suppress = false;
    }

    // ---- Results panel --------------------------------------------------

    private void ShowResults(DateTimeOffset instant)
    {
        ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        UtcText.Text = instant.UtcDateTime.ToString("ddd, dd MMM yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        DateTimeOffset local = instant.ToLocalTime();
        string offset = local.Offset < TimeSpan.Zero
            ? $"-{local.Offset:hh\\:mm}"
            : $"+{local.Offset:hh\\:mm}";
        LocalText.Text = local.LocalDateTime.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                         + $" (UTC{offset})";

        IsoText.Text = instant.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

        RelativeText.Text = Humanize(instant - DateTimeOffset.UtcNow);
    }

    private void ClearResults()
    {
        ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        UtcText.Text = "—";
        LocalText.Text = "—";
        IsoText.Text = "—";
        RelativeText.Text = "—";
    }

    private void ShowError(string message)
    {
        _current = null;
        ErrorText.Text = message;
        ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        UtcText.Text = "—";
        LocalText.Text = "—";
        IsoText.Text = "—";
        RelativeText.Text = "—";
    }

    // ---- Helpers --------------------------------------------------------

    private static bool TryParseHuman(string s, out DateTime dt)
    {
        const DateTimeStyles styles = DateTimeStyles.None;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, styles, out dt)
               || DateTime.TryParse(s, CultureInfo.CurrentCulture, styles, out dt);
    }

    private static string Humanize(TimeSpan delta)
    {
        bool future = delta > TimeSpan.Zero;
        TimeSpan abs = delta.Duration();

        string unit;
        double value;
        if (abs.TotalSeconds < 60) { value = abs.TotalSeconds; unit = "second"; }
        else if (abs.TotalMinutes < 60) { value = abs.TotalMinutes; unit = "minute"; }
        else if (abs.TotalHours < 24) { value = abs.TotalHours; unit = "hour"; }
        else if (abs.TotalDays < 30) { value = abs.TotalDays; unit = "day"; }
        else if (abs.TotalDays < 365) { value = abs.TotalDays / 30; unit = "month"; }
        else { value = abs.TotalDays / 365; unit = "year"; }

        long rounded = (long)Math.Round(value);
        if (rounded <= 0) return "just now";
        string plural = rounded == 1 ? unit : unit + "s";
        return future ? $"in {rounded} {plural}" : $"{rounded} {plural} ago";
    }
}
