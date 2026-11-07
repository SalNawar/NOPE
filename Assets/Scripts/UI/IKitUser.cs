/// <summary>
/// A view that draws kit pieces at run time (UiKitSO.Show, its inks): the
/// PC's skin (PcSkin) hands it the kit of the PC's look, so a card, a chosen
/// option or a search hit drawn later wears the same look as the built chrome.
/// </summary>
public interface IKitUser
{
    /// <summary>Draws with <paramref name="kit"/> from now on (the cel kit, or the PC theme's).</summary>
    void UseKit(UiKitSO kit);
}
