using System;
using System.Collections.Generic;

/// <summary>
/// The r17 city's draw order (CITY-R17), from the page's own drawers (<c>city_r17_scene.json</c> "order", read from
/// App.drawers by the exporter): plane 0, then what the page draws in the slot after plane 0 (in drawer order),
/// plane 1, its slot, and so on. A system the port does not draw (the r12 laundry sway) is listed by
/// <see cref="Unported"/>, never silently dropped. Pure; tested in EditMode.
/// </summary>
public static class CityR17Order
{
    /// <summary>The systems the port draws.</summary>
    public static readonly string[] Ported = { "clouds", "traffic", "lights", "ads", "train", "smoke", "fire", "smokeMouth" };

    /// <summary>One step of the frame: a plane, or a system in the slot after a plane.</summary>
    public readonly struct Step
    {
        /// <summary>The system ("plane" for the plane itself).</summary>
        public readonly string System;

        /// <summary>The plane this step draws, or the plane whose slot it is in.</summary>
        public readonly int Plane;

        public Step(string system, int plane)
        {
            System = system;
            Plane = plane;
        }

        public override string ToString() => System == "plane" ? $"P{Plane:00}" : $"{System}@{Plane}";
    }

    /// <summary>The whole frame in order: each plane, then its slot's ported systems by the page's order (ties keep the file's order).</summary>
    public static List<Step> Steps(CityR17Scene s)
    {
        var steps = new List<Step>();
        for (int p = 0; p < s.Planes.Length; p++)
        {
            steps.Add(new Step("plane", p));
            var slot = new List<(double order, int idx, string sys)>();
            for (int i = 0; i < s.Order.Length; i++)
                if (s.Order[i].Plane == p && Array.IndexOf(Ported, s.Order[i].System) >= 0)
                    slot.Add((s.Order[i].Order, i, s.Order[i].System));
            slot.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.idx.CompareTo(b.idx));
            foreach ((double order, int idx, string sys) e in slot)
                steps.Add(new Step(e.sys, p));
        }
        return steps;
    }

    /// <summary>The page's drawers the port leaves out, as "system@plane".</summary>
    public static List<string> Unported(CityR17Scene s)
    {
        var list = new List<string>();
        foreach (CityR17OrderEntry e in s.Order)
            if (Array.IndexOf(Ported, e.System) < 0)
                list.Add(e.System + "@" + e.Plane);
        return list;
    }

    /// <summary>Whether a system draws in a plane's slot.</summary>
    public static bool Has(CityR17Scene s, string system, int plane)
    {
        foreach (CityR17OrderEntry e in s.Order)
            if (e.System == system && e.Plane == plane)
                return true;
        return false;
    }
}
