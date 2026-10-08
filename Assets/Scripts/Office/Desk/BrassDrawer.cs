using UnityEngine;

/// <summary>
/// The brass stamp drawer's mechanism (Track BR; the art's prop contract,
/// tools/props/brass_drawer.py, built by Build Office UI when the model is
/// there): per lane (DENIED, APPROVED) the cradle the dater stands in (it
/// turns about the rack's x on its pivot, the dater's back-foot edge: flat on
/// its back at 0, upright at 1), the pinion on the cradle's shaft (it turns
/// with it), the rack riding on the pinion (it slides along z by the arc the
/// pinion's top travels) and the lever whose pin rides in the rack's slotted
/// block (it swings about y so its pin stays in the slot). The model is
/// modelled upright; Pose moves every part from that rest by the same
/// kinematics, so the motion reads as machinery, not a plain rotation. The
/// stamp tray (DeskStampTray) poses it from its DrawerSequence each frame.
/// </summary>
public sealed class BrassDrawer : MonoBehaviour
{
    /// <summary>The cradles, DENIED then APPROVED (DrawerSequence's lanes): each dater stands in its own.</summary>
    [SerializeField] private Transform[] cradles = new Transform[2];

    /// <summary>The pinions on the cradles' shafts, DENIED then APPROVED.</summary>
    [SerializeField] private Transform[] pinions = new Transform[2];

    /// <summary>The racks riding on the pinions, DENIED then APPROVED.</summary>
    [SerializeField] private Transform[] racks = new Transform[2];

    /// <summary>The levers driving the racks, DENIED then APPROVED (each modelled pointing along x toward the middle).</summary>
    [SerializeField] private Transform[] levers = new Transform[2];

    /// <summary>The pinions' pitch radius (metres): the rack travels this times the cradle's turn (radians).</summary>
    [SerializeField] private float pinionRadius = 0.011f;

    /// <summary>The levers' length from their pivot to their pin (metres).</summary>
    [SerializeField] private float leverLength = 0.021f;

    /// <summary>How far past the rack's middle along z its pin rides (metres): the lever's pin sits there.</summary>
    [SerializeField] private float pinReach = 0.027f;

    private readonly Quaternion[] _cradleRest = new Quaternion[2], _pinionRest = new Quaternion[2], _leverRest = new Quaternion[2];
    private readonly Vector3[] _rackRest = new Vector3[2];
    private bool _rested;

    /// <summary>The cradle of lane <paramref name="lane"/> (DrawerSequence.Denied or Approved): its dater's parent.</summary>
    public Transform Cradle(int lane) => cradles != null && lane < cradles.Length ? cradles[lane] : null;

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        for (int i = 0; i < 2; i++)
        {
            _cradleRest[i] = Part(cradles, i) != null ? cradles[i].localRotation : Quaternion.identity;
            _pinionRest[i] = Part(pinions, i) != null ? pinions[i].localRotation : Quaternion.identity;
            _leverRest[i] = Part(levers, i) != null ? levers[i].localRotation : Quaternion.identity;
            _rackRest[i] = Part(racks, i) != null ? racks[i].localPosition : Vector3.zero;
        }
    }

    private void Awake() => Rest();

    /// <summary>Poses both lanes: <paramref name="denied"/> and <paramref name="approved"/> are how far each cradle stands (0 flat on its back, 1 upright; DrawerSequence.Raise).</summary>
    public void Pose(float denied, float approved)
    {
        Rest();
        PoseLane(0, denied);
        PoseLane(1, approved);
    }

    private void PoseLane(int lane, float raise)
    {
        // Lying is a quarter turn about x that tips the top away from the chair (+z); upright is the model's rest.
        float turn = 90f * (1f - raise);
        Quaternion about = Quaternion.AngleAxis(turn, Vector3.right);
        if (Part(cradles, lane) != null)
            cradles[lane].localRotation = about * _cradleRest[lane];
        if (Part(pinions, lane) != null)
            pinions[lane].localRotation = about * _pinionRest[lane];
        // The pinion's top moves +z by its arc as the cradle lies down: the rack rides along.
        float slide = pinionRadius * turn * Mathf.Deg2Rad;
        if (Part(racks, lane) != null)
            racks[lane].localPosition = _rackRest[lane] + Vector3.forward * slide;
        if (Part(levers, lane) == null || Part(racks, lane) == null || leverLength <= 0f)
            return;
        // The lever's pivot sits at the pin's mid travel; its pin follows the slotted block along z (it slides along x in the slot).
        float travel = pinionRadius * Mathf.PI / 2f;
        float pinZ = _rackRest[lane].z + pinReach + slide;
        float pivotZ = _rackRest[lane].z + pinReach + travel / 2f;
        float swing = Mathf.Asin(Mathf.Clamp((pinZ - pivotZ) / leverLength, -1f, 1f)) * Mathf.Rad2Deg;
        // Pointing toward the middle (-x on the right, +x on the left), a turn of a about y moves the tip's z by -side·L·sin(a).
        float side = levers[lane].localPosition.x >= 0f ? -1f : 1f;
        levers[lane].localRotation = Quaternion.AngleAxis(-side * swing, Vector3.up) * _leverRest[lane];
    }

    private static Transform Part(Transform[] parts, int i) => parts != null && i < parts.Length ? parts[i] : null;
}
