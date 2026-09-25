using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The facade UI code reads its strings through (piece 6 U7, R2): the culture
/// theme service's lookup when it runs, else a reading-only lookup built once
/// from RunConfig's content library (edit-mode tools get English), else the
/// key itself. A key no table has is logged once. Numbers are formatted by the
/// template's specifiers in the invariant culture; canonical values (names,
/// places, facts, dates) go in as string arguments, verbatim.
/// </summary>
public static class UiText
{
    /// <summary>The three forms the wallet's word takes (R13).</summary>
    public enum WalletForm
    {
        /// <summary>The tray's label ("Credits: 0").</summary>
        Label,

        /// <summary>Inside a sentence ("+5 credits").</summary>
        Inline,

        /// <summary>Home's shop prices ("120 cr").</summary>
        Short
    }

    /// <summary>The reading-only lookup used when no theme service runs.</summary>
    private static UiStrings _readingOnly;

    /// <summary>The table entries <see cref="_readingOnly"/> was built from (rebuilt when Generate World replaces them).</summary>
    private static List<UiStringEntry> _readingEntries;

    /// <summary>The library RunConfig names (for the fallback lookup and FitLabel).</summary>
    private static ContentLibrarySO _configLibrary;

    /// <summary>Keys already reported missing.</summary>
    private static readonly HashSet<string> WarnedKeys = new HashSet<string>();

    /// <summary>The string for a key.</summary>
    public static string Get(string key) => Format(key);

    /// <summary>The string for a key with its placeholders filled (UiStrings.Format).</summary>
    public static string Format(string key, params object[] args)
    {
        UiStrings strings = Strings();
        if (strings == null)
            return key;

        string text = strings.Format(key, args);
        if (text == key && !string.IsNullOrEmpty(key) && System.Linq.Enumerable.Contains(strings.MissingKeys, key) && WarnedKeys.Add(key))
            Debug.LogWarning($"[UiText] No UI string '{key}'; add it to world_source.json ui.strings and run Tools > TimeDesk > Generate World.");
        return text;
    }

    /// <summary>The wallet's word in a form: the present culture's Future currency, else the neutral word (CultureChoice.Wallet).</summary>
    public static string Currency(WalletForm form)
    {
        CultureThemeService service = CultureThemeService.Instance;
        string fallback = Get(form == WalletForm.Label ? "wallet.credits" : form == WalletForm.Inline ? "wallet.creditsInline" : "wallet.creditsShort");
        return service != null ? CultureChoice.Wallet(service.ActiveCultureId, service.FutureCurrency, fallback) : fallback;
    }

    /// <summary>A category's report word (ClueLabels.Key).</summary>
    public static string Category(ClueCategory category) => Get(ClueLabels.Key(category));

    /// <summary>A garment slot's word (Looks.SlotKey).</summary>
    public static string Slot(LookSlot slot) => Get(Looks.SlotKey(slot));

    /// <summary>A deviation's report line: the template Domain chose, with the category word, the stated value and ReportOther.</summary>
    public static string Deviation(Discrepancy d) =>
        d == null ? string.Empty : Format(d.ReportKey, Category(d.category), d.documentValue, d.ReportOther);

    /// <summary>
    /// Lets a text built outside the office builder (Home), or a text showing
    /// wide foreign glyphs (piece 9), shrink instead of spilling: auto-size
    /// from CultureUiSettings.labelMinScale of its size to its size (R21), with
    /// no wrapping unless <paramref name="keepWrapping"/>. Without a content
    /// library it is left as it is.
    /// </summary>
    public static void FitLabel(TMP_Text text, bool keepWrapping = false)
    {
        ContentLibrarySO library = Library();
        if (text == null || library == null)
            return;

        float size = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
        if (!keepWrapping)
            text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = size * library.CultureUi.labelMinScale;
    }

    /// <summary>The lookup in use (null when no content library is reachable).</summary>
    private static UiStrings Strings()
    {
        CultureThemeService service = CultureThemeService.Instance;
        if (service != null && service.Strings != null)
            return service.Strings;

        ContentLibrarySO library = Library();
        UiStringTableSO table = library != null ? library.GetStringTable(library.CultureUi.readingLanguage) : null;
        if (table != null && (_readingOnly == null || !ReferenceEquals(table.entries, _readingEntries)))
        {
            _readingEntries = table.entries;
            _readingOnly = new UiStrings(table.entries, null, false, library.CultureUi.glossPercent);
        }
        return _readingOnly;
    }

    /// <summary>The theme service's library, else RunConfig's.</summary>
    private static ContentLibrarySO Library()
    {
        CultureThemeService service = CultureThemeService.Instance;
        if (service != null && service.Library != null)
            return service.Library;
        if (_configLibrary == null)
        {
            var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
            _configLibrary = config != null ? config.contentLibrary : null;
        }
        return _configLibrary;
    }
}
