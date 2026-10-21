using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The office builder's text and button makers (Text, MakeButton).
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.cs by P0-A (pure move). Owner: TEXT-LANG.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>
    /// A text, created once (existing-wins); with a role it gets that theme
    /// tag. A new keyed text is baked with its English label; <paramref name="style"/>
    /// is its built style (new texts get it; the tag records it). A new text is
    /// set in the kit's body face (bold: its bold face), never TMP's default
    /// LiberationSans (BUG-38a; the kit pass gives each role its own face, and
    /// re-fonts any text an older build left in the default, KitRefont).
    /// </summary>
    private static TMP_Text Text(Transform parent, string name, string content, int size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color color,
                                 ThemeRoleId? role = null, string labelKey = null, FontStyles style = FontStyles.Normal, ThemeTextKind kind = ThemeTextKind.Body, bool fit = false)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var et = existing.GetComponent<TMP_Text>();
            if (et != null)
            {
                if (role.HasValue)
                    SceneUiKit.Tag(et, role.Value, ThemePart.Ink, labelKey, style, kind, fit);
                return et;
            }
            // Same-named non-text leftover from an older build: replace it.
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        SceneUiKit.SetBodyFace(t);
        if ((style & FontStyles.Bold) != 0 && SceneUiKit.BodyBoldFace != null)
        {
            t.font = SceneUiKit.BodyBoldFace;
            t.fontSharedMaterial = SceneUiKit.BodyBoldFace.material;
        }
        t.text = labelKey != null ? UiText.Get(labelKey) : content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.fontStyle = style;
        if (role.HasValue)
            SceneUiKit.Tag(t, role.Value, ThemePart.Ink, labelKey, style, kind, fit);
        return t;
    }

    /// <summary>
    /// A button (created once, existing-wins) with its label; with a role, its
    /// image and label get that theme tag, and a keyed label shrinks to fit
    /// (the label is baked in English from <paramref name="labelKey"/>, else <paramref name="label"/>).
    /// </summary>
    private static Button MakeButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Color? color = null,
                                     ThemeRoleId? role = null, string labelKey = null)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var eb = existing.GetComponent<Button>();
            if (eb != null)
            {
                if (role.HasValue)
                {
                    SceneUiKit.Tag(eb.GetComponent<Image>(), role.Value, ThemePart.Fill);
                    Transform existingLabel = eb.transform.Find("Label");
                    if (existingLabel != null)
                        SceneUiKit.Tag(existingLabel.GetComponent<TMP_Text>(), role.Value, ThemePart.Ink, labelKey, FontStyles.Normal, ThemeTextKind.Button, labelKey != null);
                }
                return eb;
            }
            // Same-named non-button leftover from an older build: replace it
            // instead of silently creating a duplicate sibling.
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color ?? XpFace;
        if (role.HasValue)
            SceneUiKit.Tag(img, role.Value, ThemePart.Fill);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        Color labelColor = color.HasValue ? Color.white : Color.black;
        Text(go.transform, "Label", label, 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, labelColor,
             role, labelKey, FontStyles.Normal, ThemeTextKind.Button, labelKey != null);
        return btn;
    }
}
