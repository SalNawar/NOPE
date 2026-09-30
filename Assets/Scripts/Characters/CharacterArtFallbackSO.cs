using UnityEngine;

/// <summary>
/// Where a character layer with no ChatGPT art yet borrows its picture from
/// (LookArtFallback): each layer's steps and each nation's neighbours. Lives
/// at Assets/Resources/CharacterArtFallback.asset, loaded by CharacterArt; a
/// knob Saleh edits in the inspector (Generate World never writes it). The
/// order is documented in docs/CHARACTER_ART_CONTRACT.md section 9.
/// </summary>
[CreateAssetMenu(fileName = "CharacterArtFallback", menuName = "TimeDesk/Character Art Fallback", order = 20)]
public sealed class CharacterArtFallbackSO : ScriptableObject
{
    /// <summary>The Resources path CharacterArt loads the table from.</summary>
    public const string ResourcePath = "CharacterArtFallback";

    /// <summary>The steps and the neighbours.</summary>
    public LookArtFallbackTable table = new();
}
