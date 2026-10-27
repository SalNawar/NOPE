using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The concept-vs-game review's desk items (desk v3, round 12;
/// E:\unity\NOPE-tools\reports\desk_v3_unity\CONCEPT_VS_GAME.md): the homes
/// the painting shows empty stay empty (5) and the scanner's front has no
/// extra slot plate (8), their upgrades still usable; no painted object
/// casts the hall daylight's wrong-way shadow, the "ghost square" (6, 11);
/// the slot behind the deal tray is closed (7); the mat and the desk keep more
/// of their line work (10). Data and source text (Unity runs the look: R12).
/// </summary>
public class PaintedDeskConceptTests
{
    private static string Read(string relative, [CallerFilePath] string here = "")
    {
        string path = File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [Test]
    public void NoStandIn_IsDrawn_ButEachKeepsItsSpec()
    {
        foreach (StandIn s in DeskPaintedMap.StandIns)
        {
            Assert.IsFalse(DeskPaintedMap.StandInDrawn(s.id), $"{s.id}: the painting shows its home empty");
            Assert.IsNotNull(DeskPaintedMap.StandInFor(s.id), $"{s.id}: still built, for its click box");
        }
        Assert.IsFalse(DeskPaintedMap.StandInDrawn(DeskPaintedMap.AutoFeed), "no white slot plate on the scanner's front");
    }

    [Test]
    public void AStandInNotDrawn_KeepsItsClick_DrawsNothing_AndDoesNotSlide()
    {
        string binder = Read("Assets/Scripts/Office/OfficeSceneBinder.cs");
        StringAssert.Contains("if (!DeskPaintedMap.StandInDrawn(spec.id))", binder);
        int fit = binder.IndexOf("FitStandInBox(box, standIn, spec);", StringComparison.Ordinal);
        int hide = binder.IndexOf("if (!DeskPaintedMap.StandInDrawn(spec.id))", StringComparison.Ordinal);
        Assert.That(hide, Is.GreaterThan(fit), "the click box is fitted on the built stand-in before its renderers go off");
        StringAssert.Contains("if (!_painted || DeskPaintedMap.StandInDrawn(id))\n                MachineSlideIn.Attach(id, DeskLayout.ObjectOf(id));", binder,
                              "an invisible lift would rise and sound");
        StringAssert.Contains("c.SetOutline(scannerLook);", binder, "the add-ons' clicks outline the painted scanner they sit on");
    }

    [Test]
    public void NoPaintedObject_CastsARealTimeShadow()
    {
        string painted = Read("Assets/Scripts/Office/Desk/DeskPainted.cs");
        StringAssert.Contains("CastNoShadows();", painted, "at install (Align)");
        Assert.IsFalse(painted.Contains("ShadowCastingMode.On"), "the stand-ins and the crate's boxes cast none either");
        string importer = Read("Assets/Editor/DeskPainted/DeskPaintedImporter.cs");
        Assert.IsFalse(importer.Contains("ShadowCastingMode.On"), "the next import marks none to cast");
    }

    [Test]
    public void TheDealTraysSlot_IsClosed()
    {
        string painted = Read("Assets/Scripts/Office/Desk/DeskPainted.cs");
        StringAssert.Contains("CloseDealTraySlot();", painted);
        StringAssert.Contains("SlotBackCentre = new Vector3(0f, 1.12f, 1.21f)", painted, "the slot's back, measured by a ray probe of the bake (rows 693 to 696 met no mesh)");
    }

    [Test]
    public void TheMatAndTheDesk_KeepTheirLineWork_KaiserMipsAndANegativeBias()
    {
        foreach (string name in new[] { "Desk_Mat_Paint", "DeskShell_Paint" })
        {
            string meta = Read($"Assets/Art/Office/DeskPainted/Textures/{name}.png.meta");
            StringAssert.Contains("mipMapMode: 1", meta, name);
            StringAssert.Contains("mipBias: -0.5", meta, name);
        }
        StringAssert.Contains("importer.mipMapBias = sharp ? -0.5f : 0f;", Read("Assets/Editor/DeskPainted/DeskPaintedImporter.cs"), "the importer keeps them on the next import");
    }
}
