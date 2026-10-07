using System;

/// <summary>
/// The gate lever's travel (the desk machine spec §2: "a heavy
/// floor-mounted lever by the desk. You drag it down and it resists along its
/// travel. It ratchets with clicks every 15° and needs a full pull"), in
/// degrees down from rest: the arm lags the player's pull more the further it
/// goes (Resisted), a ratchet click sounds at each notch it crosses
/// (Crossed), and only the full travel is home. Pure; GateLever moves it.
/// </summary>
public static class LeverTravel
{
    /// <summary>The ratchet notch <paramref name="angle"/> has passed (0 at rest; a notch every <paramref name="step"/> degrees; 0 for no step).</summary>
    public static int Notch(float angle, float step) => step > 0f && angle > 0f ? (int)MathF.Floor(angle / step + 1e-4f) : 0;

    /// <summary>The ratchet clicks between <paramref name="from"/> and <paramref name="to"/>: notches crossed going down (positive) or up (negative).</summary>
    public static int Crossed(float from, float to, float step) => Notch(to, step) - Notch(from, step);

    /// <summary>True when the arm is home: at (or past) the full <paramref name="travel"/>.</summary>
    public static bool Home(float angle, float travel) => angle >= travel - 1e-3f;

    /// <summary>
    /// Where the arm goes for the player's <paramref name="pull"/> (degrees
    /// of pointer travel): pull / (1 + k·pull), the resistance k set so a
    /// pull of travel × (1 + <paramref name="resistance"/>) reaches home; 0
    /// resistance follows the pull; clamped between rest and home.
    /// </summary>
    public static float Resisted(float pull, float travel, float resistance)
    {
        if (pull <= 0f || travel <= 0f)
            return 0f;
        float r = MathF.Max(0f, resistance);
        float k = r / (travel * (1f + r));
        return MathF.Min(travel, pull / (1f + k * pull));
    }
}
