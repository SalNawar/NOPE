using UnityEngine;

/// <summary>
/// Retired (the office move): the push-in camera rig. Kept as an empty
/// component only because the art office (OfficeScene.unity, the art side's)
/// still carries one on its leftover OfficeRoot, and a deleted script would
/// log a missing-script warning on every office load. It does nothing; the
/// art side deletes OfficeRoot (docs/SCENE_CONTRACT_GAMEPLAY.md), then this
/// file goes too.
/// </summary>
[AddComponentMenu("")]
public sealed class CinemachineCameraRig : MonoBehaviour
{
}
