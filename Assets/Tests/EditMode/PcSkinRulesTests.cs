using System;
using NUnit.Framework;

/// <summary>
/// The cyberpunk PC's grammar (D2, the night of 2026-11-07): which drawn
/// piece replaces each cel kit sprite, in which tone and state; the art the
/// theme keeps; the taskbar's glyph-only rule (UI-14); the colour rules for
/// roles (the evidence keeps its colours, every chrome text is light); the
/// culture's accent; and the chrome painter's geometry (a clear pad, the lit
/// top edge, the bezel's hole).
/// </summary>
public class PcSkinRulesTests
{
    [TestCase("panel_bone", PcPiece.Panel, PcTone.Quiet, KitState.Rest)]
    [TestCase("dropdown", PcPiece.Panel, PcTone.Quiet, KitState.Rest)]
    [TestCase("tooltip", PcPiece.Panel, PcTone.Quiet, KitState.Rest)]
    [TestCase("titlebar_slate", PcPiece.TitleBar, PcTone.Quiet, KitState.Rest)]
    [TestCase("taskbar", PcPiece.Deck, PcTone.Quiet, KitState.Rest)]
    [TestCase("plate_ox_hover", PcPiece.Plate, PcTone.Accent, KitState.Hover)]
    [TestCase("miniplate_bone_rest", PcPiece.Plate, PcTone.Quiet, KitState.Rest)]
    [TestCase("plate_red_pressed", PcPiece.Plate, PcTone.Danger, KitState.Pressed)]
    [TestCase("plate_green_rest", PcPiece.Plate, PcTone.Primary, KitState.Rest)]
    [TestCase("plate_brass_locked", PcPiece.Plate, PcTone.Amber, KitState.Locked)]
    [TestCase("iconkey_red_hover", PcPiece.Plate, PcTone.Danger, KitState.Hover)]
    [TestCase("iconkey_rest", PcPiece.Plate, PcTone.Quiet, KitState.Rest)]
    [TestCase("segment_on", PcPiece.Plate, PcTone.Accent, KitState.Rest)]
    [TestCase("segment_off", PcPiece.Plate, PcTone.Quiet, KitState.Rest)]
    [TestCase("upgradecard_owned", PcPiece.Panel, PcTone.Primary, KitState.Rest)]
    [TestCase("upgradecard_buyable", PcPiece.Panel, PcTone.Accent, KitState.Rest)]
    [TestCase("upgradecard_dear", PcPiece.Panel, PcTone.Amber, KitState.Rest)]
    [TestCase("upgradecard_locked", PcPiece.Panel, PcTone.Muted, KitState.Rest)]
    [TestCase("listrow_rest", PcPiece.Row, PcTone.Primary, KitState.Rest)]
    [TestCase("listrow_hover", PcPiece.Row, PcTone.Primary, KitState.Hover)]
    [TestCase("listrow_selected", PcPiece.Row, PcTone.Accent, KitState.Hover)]
    [TestCase("field_focus", PcPiece.Field, PcTone.Primary, KitState.Rest)]
    [TestCase("field_error", PcPiece.Field, PcTone.Danger, KitState.Rest)]
    [TestCase("badge_red", PcPiece.Badge, PcTone.Accent, KitState.Rest)]
    [TestCase("badge_brass", PcPiece.Badge, PcTone.Amber, KitState.Rest)]
    [TestCase("scroll_thumb", PcPiece.Scroll, PcTone.Primary, KitState.Rest)]
    [TestCase("checkbox_on", PcPiece.Check, PcTone.Accent, KitState.Rest)]
    [TestCase("glyph_cross", PcPiece.Glyph, PcTone.Primary, KitState.Rest)]
    [TestCase("tile_mail_locked", PcPiece.Tile, PcTone.Primary, KitState.Locked)]
    public void EachCelSprite_TakesItsPiece_InItsToneAndState(string sprite, PcPiece piece, PcTone tone, KitState state) =>
        Assert.AreEqual(new PcPart(piece, tone, state), PcSkinRules.Of(sprite), sprite);

    [TestCase("lcd_glass")]
    [TestCase("logo_time_sorter")]
    [TestCase("slot_cabinet")]
    [TestCase("ribbon_green")]
    [TestCase("speech_bubble")]
    [TestCase("roundbadge_tick")]
    [TestCase("pulltab_left_rest")]
    [TestCase("wallpaper_skyline")]
    [TestCase("")]
    [TestCase(null)]
    public void ArtTheThemeDoesNotRedraw_Stays(string sprite) =>
        Assert.AreEqual(PcPart.Keep, PcSkinRules.Of(sprite));

    [Test]
    public void ATaskbarButton_ShowsItsTitle_OnlyWhileWideEnough()
    {
        Assert.IsTrue(PcSkinRules.TaskbarShowsLabel(220f, 170f));
        Assert.IsTrue(PcSkinRules.TaskbarShowsLabel(170f, 170f));
        Assert.IsFalse(PcSkinRules.TaskbarShowsLabel(120f, 170f), "a 120-unit button shows its glyph only: its title is never cut mid-letter (UI-14)");
    }

    [Test]
    public void TheEvidence_KeepsItsColours_AndEveryChromeTextIsLight()
    {
        foreach (ThemeRoleId role in Enum.GetValues(typeof(ThemeRoleId)))
        {
            if (ThemeRoles.IsDiegetic(role))
            {
                Assert.AreEqual(PcInk.Keep, PcPalette.Of(role, true), role + " text");
                Assert.AreEqual(PcInk.Keep, PcPalette.Of(role, false), role + " image");
                continue;
            }
            PcInk text = PcPalette.Of(role, true);
            Assert.AreNotEqual(PcInk.Glass, text, role + ": a text never takes a glass colour");
            Assert.AreNotEqual(PcInk.GlassRaised, text, role.ToString());
            Assert.AreNotEqual(PcInk.GlassDeep, text, role.ToString());
        }
        Assert.AreEqual(PcInk.Glass, PcPalette.Of(ThemeRoleId.WindowBody, false));
        Assert.AreEqual(PcInk.Ink, PcPalette.Of(ThemeRoleId.WindowBody, true));
        Assert.AreEqual(PcInk.InkMuted, PcPalette.Of(ThemeRoleId.SurfaceMuted, true));
        Assert.AreEqual(PcInk.Phosphor, PcPalette.Of(ThemeRoleId.Tray, true));
        Assert.AreEqual(PcInk.Keep, PcPalette.Of(ThemeRoleId.Desktop, false), "the wallpaper is the skin's own");
    }

    [Test]
    public void TheCultureAccent_BlendsByItsShare_AsANeon()
    {
        var magenta = new Rgba(1f, 0.243f, 0.659f);
        var dullGreen = new Rgba(0.2f, 0.4f, 0.25f);
        Rgba none = PcPalette.CultureAccent(magenta, dullGreen, 0f);
        Assert.AreEqual(magenta.R, none.R, 1e-5f);
        Assert.AreEqual(magenta.G, none.G, 1e-5f);
        Rgba all = PcPalette.CultureAccent(magenta, dullGreen, 1f);
        Assert.GreaterOrEqual(Math.Max(all.R, Math.Max(all.G, all.B)), 0.95f - 1e-5f, "lifted to a neon's value");
        Assert.Greater(all.G, all.R, "the culture's hue is kept (green)");
        Assert.Greater(all.G, all.B);
        Rgba half = PcPalette.CultureAccent(magenta, dullGreen, 0.5f);
        Assert.AreEqual((magenta.R + all.R) / 2f, half.R, 1e-4f);
    }

    [Test]
    public void APiece_IsClearInItsPad_FilledInside_AndLitAlongItsTop()
    {
        var fill = new Rgba(0.05f, 0.06f, 0.1f);
        var edge = new Rgba(0.1f, 0.2f, 0.3f);
        var lit = new Rgba(0.2f, 0.9f, 1f);
        var paint = new PcPaint(fill, fill, edge, lit, new Rgba(0f, 0f, 0f, 0f), 28f, 10f, 2f, 0f);
        const int w = 120, h = 100;
        Assert.AreEqual(0f, PcChromeArt.Pixel(paint, 2, 2, w, h).A, 1e-5f, "the pad holds no paint");
        Rgba inside = PcChromeArt.Pixel(paint, w / 2, h / 2, w, h);
        Assert.AreEqual(fill.R, inside.R, 1e-4f);
        Assert.AreEqual(1f, inside.A, 1e-4f);
        Rgba top = PcChromeArt.Pixel(paint, w / 2, h - 28 - 1, w, h);
        Assert.AreEqual(lit.G, top.G, 1e-4f, "the top edge is lit");
        Rgba bottom = PcChromeArt.Pixel(paint, w / 2, 28, w, h);
        Assert.AreEqual(edge.G, bottom.G, 1e-4f, "the other sides take the quiet edge");
    }

    [Test]
    public void AHoveredPlate_GlowsInItsPad()
    {
        var c = new Rgba(0.1f, 0.1f, 0.2f);
        var glow = new Rgba(0.2f, 0.9f, 1f, 0.5f);
        var paint = new PcPaint(c, c, c, c, glow, 28f, 10f, 2f, 18f);
        Assert.Greater(PcChromeArt.Pixel(paint, 60, 22, 120, 100).A, 0.1f, "just outside the plate: the glow");
        Assert.AreEqual(0f, PcChromeArt.Pixel(paint, 60, 2, 120, 100).A, 1e-5f, "past the glow's reach: clear");
    }

    [Test]
    public void TheBezel_IsClearInItsGlass_AndLitAlongItsEdge()
    {
        var body = new Rgba(0.07f, 0.09f, 0.12f);
        var lit = new Rgba(0.2f, 0.9f, 1f, 0.85f);
        var gasket = new Rgba(0f, 0f, 0.02f);
        Rgba Px(int x, int y) => PcChromeArt.Bezel(x, y, 620, 530, 22f, 30f, 80f, 590f, 500f, body, body, lit, gasket, 9f);
        Assert.AreEqual(0f, Px(300, 300).A, 1e-5f, "the glass shows the picture");
        Assert.AreEqual(1f, Px(300, 40).A, 1e-4f, "the chin is solid");
        Assert.Greater(Px(300, 529).G, body.G + 0.3f, "the outer edge is lit");
        Assert.AreEqual(0f, Px(0, 0).A, 1e-5f, "the rounded corner is clear");
    }

    [Test]
    public void TheScanRows_DarkenOneRowInEachPitch()
    {
        Assert.AreEqual(0.12f, PcChromeArt.ScanRow(0, 3, 0.12f), 1e-6f);
        Assert.AreEqual(0f, PcChromeArt.ScanRow(1, 3, 0.12f));
        Assert.AreEqual(0.12f, PcChromeArt.ScanRow(-3, 3, 0.12f), 1e-6f);
        Assert.AreEqual(0f, PcChromeArt.Vignette(0.5f, 0.5f, 0.35f), 1e-6f, "the middle of the glass is clear");
        Assert.Greater(PcChromeArt.Vignette(0.02f, 0.02f, 0.35f), 0.2f, "the corners darken");
        Assert.Greater(PcChromeArt.Bloom(0.01f, 0.5f, 0.16f), PcChromeArt.Bloom(0.5f, 0.5f, 0.16f), "the bloom rises to the edges");
    }
}
