/// <summary>
/// Read-only view of today's shift for presentation code outside the gameplay
/// layer (the art office's crowd palette). The gameplay side owns it:
/// ShiftClockDriver implements it and publishes itself as
/// <see cref="ShiftClockDriver.Live"/>; a reader can never start, pause or stop
/// the clock. docs/SCENE_CONTRACT_GAMEPLAY.md lists it under the art side's hooks.
/// </summary>
public interface IShiftProgress
{
    /// <summary>0 at opening, 1 at closing (<see cref="ShiftClock.Progress01"/>); 0 until today's clock exists.</summary>
    float Progress01 { get; }

    /// <summary>The clock's minute of the day (<see cref="ShiftClock.CurrentMinute"/>; 540 is 09:00): the anime hall's day-night cycle reads its hour; the opening minute until today's clock exists.</summary>
    float MinuteOfDay { get; }
}
