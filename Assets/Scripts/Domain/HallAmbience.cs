using System;

/// <summary>How loud each ambience bed plays now (0 to 1, before the bank's own volumes).</summary>
public readonly struct AmbienceMix
{
    /// <summary>amb_hall_day, amb_hall_rush (layered over the day), amb_hall_night, amb_rain, amb_booth_room, amb_portal_hum.</summary>
    public readonly float Day, Rush, Night, Rain, Booth, Portal;

    /// <summary>A mix from its beds' levels.</summary>
    public AmbienceMix(float day, float rush, float night, float rain, float booth, float portal)
    {
        Day = day;
        Rush = rush;
        Night = night;
        Rain = rain;
        Booth = booth;
        Portal = portal;
    }

    /// <summary>The level of bed <paramref name="id"/> (0 for an id that is no bed).</summary>
    public float Of(string id)
    {
        switch (id)
        {
            case SoundCues.AmbHallDay:
                return Day;
            case SoundCues.AmbHallRush:
                return Rush;
            case SoundCues.AmbHallNight:
                return Night;
            case SoundCues.AmbRain:
                return Rain;
            case SoundCues.AmbBoothRoom:
                return Booth;
            case SoundCues.AmbPortalHum:
                return Portal;
            default:
                return 0f;
        }
    }
}

/// <summary>
/// The hall's ambience as the hall's state says (Saleh 2026-10-07: day, rush
/// and night from the shift hours, rain from the weather, the booth's room
/// tone always): at the clock's minute of the day the night bed plays from
/// <c>nightFrom</c> to <c>dayFrom</c> and the day bed the rest, each fading
/// into the other over <c>fade</c> minutes; the rush bed layers over the day
/// inside its windows (fading in and out the same way); the rain bed follows
/// the weather's rain; the booth's room tone and the portals' hum always play.
/// </summary>
public static class HallAmbience
{
    /// <summary>The mix at <paramref name="minute"/> of the day with <paramref name="rain"/> (0..1).</summary>
    public static AmbienceMix For(float minute, float rain, int nightFrom, int dayFrom, int fade, int[] rushWindows)
    {
        float night = Window(minute, nightFrom, dayFrom + 1440, fade);
        night = MathF.Max(night, Window(minute + 1440, nightFrom, dayFrom + 1440, fade));
        float rush = 0f;
        if (rushWindows != null)
            for (int i = 0; i + 1 < rushWindows.Length; i += 2)
                rush = MathF.Max(rush, Window(minute, rushWindows[i], rushWindows[i + 1], fade));
        float day = 1f - night;
        return new AmbienceMix(day, rush * day, night, Clamp01(rain), 1f, 1f);
    }

    /// <summary>1 inside [<paramref name="from"/>, <paramref name="until"/>) (minutes), ramping over <paramref name="fade"/> minutes at each edge (centred on it), 0 outside.</summary>
    private static float Window(float minute, float from, float until, float fade)
    {
        float half = MathF.Max(0.5f, fade / 2f);
        float rise = Clamp01((minute - (from - half)) / (2f * half));
        float fall = Clamp01(((until + half) - minute) / (2f * half));
        return MathF.Min(rise, fall);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}

/// <summary>The cue ids the code plays (Saleh's sound list, SOUND_LIST.md: each id is its file's name); the importer warns about any that the list lacks.</summary>
public static class SoundCues
{
    /// <summary>The stamps.</summary>
    public const string StampApprove = "stamp_approve", StampDeny = "stamp_deny", StampMiss = "stamp_miss", StampLift = "stamp_lift";

    /// <summary>The brass stamp drawer (Track BR, Saleh 2026-10-08): opening, a typewriter's carriage-return carry and the clunk at the stop; closing, a shorter reverse rasp and a deep thud.</summary>
    public const string DrawerOpen = "drawer_open", DrawerClose = "drawer_close";

    /// <summary>The papers.</summary>
    public const string PaperPickup = "paper_pickup", PaperDrop = "paper_drop", PaperSlide = "paper_slide";

    /// <summary>The citation and the scanner.</summary>
    public const string CitationPrint = "citation_print", CitationLand = "citation_land", ScannerStart = "scanner_start", ScannerDone = "scanner_done", ScannerFlag = "scanner_flag";

    /// <summary>The cream scanner (Track BR, Saleh 2026-10-08: "makes a xerox sound as it quickly scans"): its lid swinging open, the lid's clack shut, the xerox scan.</summary>
    public const string ScannerLidOpen = "scanner_lid_open", ScannerLidClose = "scanner_lid_close", ScannerScan = "scanner_scan";

    /// <summary>The scanner's PC app (the scanner app spec's "Feel"): a glow's soft tick (a cell that differs, a paper flagged) and a finding pinned to the board (evidence_pin: added to the list by Track SA).</summary>
    public const string InspectLink = "inspect_link", EvidencePin = "evidence_pin";

    /// <summary>The desk machine (the desk machine spec, Track DM): the date wheels' ratchet click, the re-ink squish, the DETAIN button's cover snapping open or shut, DETAIN's chirp.</summary>
    public const string DaterWheelClick = "dater_wheel_click", DaterReink = "dater_reink", DetainCover = "detain_cover", Detain = "detain";

    /// <summary>The booth.</summary>
    public const string CallNext = "call_next", PortalThrough = "portal_through", DialogueBlip = "dialogue_blip";

    /// <summary>The PC.</summary>
    public const string PcOn = "pc_on", PcOff = "pc_off", KeyTap = "key_tap", MouseClick = "mouse_click", WindowOpen = "window_open", WindowClose = "window_close", PcError = "pc_error", PcNotify = "pc_notify";

    /// <summary>The interface.</summary>
    public const string UiHover = "ui_hover", UiPress = "ui_press", UiRelease = "ui_release", UiToggle = "ui_toggle", UiTab = "ui_tab", UiError = "ui_error", UiPopup = "ui_popup", WheelOpen = "wheel_open";

    /// <summary>The Night Slots machine at Home: a ratchet notch of the lever, the reels spinning up, a reel landing, a win's bell, a loss's womp, the payout's coins.</summary>
    public const string SlotLever = "slot_lever", SlotSpin = "slot_spin", SlotStop = "slot_stop", SlotWin = "slot_win", SlotLose = "slot_lose", Coins = "coins";

    /// <summary>The shift.</summary>
    public const string LastHourAlarm = "last_hour_alarm", ShiftEndBell = "shift_end_bell", PaChime = "pa_chime", BoardFlip = "board_flip", DayStart = "day_start";

    /// <summary>The river and the penalties.</summary>
    public const string CitationHit = "citation_hit", HelixPulse = "helix_pulse", HelixBreach = "helix_breach", FamousPass = "famous_pass";

    /// <summary>The ambience beds.</summary>
    public const string AmbHallDay = "amb_hall_day", AmbHallRush = "amb_hall_rush", AmbHallNight = "amb_hall_night", AmbPortalHum = "amb_portal_hum",
                        AmbRain = "amb_rain", AmbBoothRoom = "amb_booth_room", AmbHome = "amb_home", MusTitle = "mus_title";

    /// <summary>The beds the hall's ambience mixes (HallAmbience).</summary>
    public static readonly string[] HallBeds = { AmbHallDay, AmbHallRush, AmbHallNight, AmbRain, AmbBoothRoom, AmbPortalHum };
}

/// <summary>
/// The shift's bells (Saleh 2026-10-07: "an alarm ding or something" when the
/// last hour of the shift starts; the bell when it ends): when the clock,
/// moving from <c>previous</c> to <c>minute</c> (minutes of the day), crosses
/// into the shift's last <c>lastHour</c> minutes, read from the day's own
/// hours (a night shift's 24:00 close rings at 23:00). A shift no longer than
/// its last hour has none.
/// </summary>
public static class ShiftBells
{
    /// <summary>True when the clock crossed the start of the last <paramref name="lastHour"/> minutes of <paramref name="hours"/> between <paramref name="previous"/> and <paramref name="minute"/>.</summary>
    public static bool LastHourBegan(float previous, float minute, ShiftHours hours, int lastHour)
    {
        if (lastHour <= 0 || hours.LengthMinutes <= lastHour)
            return false;
        float mark = hours.EndMinute - lastHour;
        return previous < mark && minute >= mark;
    }
}
