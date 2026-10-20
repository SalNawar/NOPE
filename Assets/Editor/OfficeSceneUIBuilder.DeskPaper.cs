using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The office builder's desk paper: the paper template (its face, texts and
/// depth parts), the documents' meshes and cel, edge and shadow materials,
/// the rulebook folder, and the floating notes with their backing card.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: DOC-LOOK.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The paper's photo frame (shown only on a photo document).</summary>
    private static readonly Color PhotoGrey = new Color(0.55f, 0.56f, 0.58f, 1f);

    /// <summary>The photo's height as a share of its frame's.</summary>
    private const float PhotoFill = 0.92f;

    /// <summary>The desk notes' ink.</summary>
    private static readonly Color NoteInk = new Color(0.96f, 0.95f, 0.88f, 1f);

    /// <summary>The day-1 notes' backing (the readability fix): a dark, mostly opaque plate behind the light text, so it reads over the pale morning crowds and the evening palette.</summary>
    private static readonly Color NoteBackingColour = new Color(0.07f, 0.08f, 0.1f, 0.85f);

    /// <summary>The margin of a note's backing around its text (metres).</summary>
    private static readonly Vector2 NoteBackingMargin = new Vector2(0.04f, 0.02f);

    /// <summary>
    /// The inactive paper every handed-over document clones: a root (the
    /// DeskDocument, its DeskDraggable and Clickable) and its lying Sheet (the
    /// click box on the Interactable layer, the lit paper quad in the form
    /// style's paper tone that the hover outlines, the hidden photo frame with
    /// the traveller's photo, and the printing parts: BuildPaperPrint); the
    /// unlit reading material the paper wears on the desk, full size. The paper prints its
    /// document's form when it binds (redesign phase 4). Rebuilt each run.
    /// </summary>
    private static DeskDocument BuildPaperTemplate(Transform desk, DeskConfigSO config)
    {
        DestroyChildIfPresent(desk, "PaperTemplate");
        Transform root = EnsureChild(desk, "PaperTemplate");
        Transform sheet = EnsureChild(root, "Sheet");
        sheet.localRotation = Quaternion.Euler(90f, 0f, 0f);
        sheet.gameObject.layer = OfficeLayers.InteractableLayer;
        Vector2 size = config.paperSize;
        BoxCollider box = sheet.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, PaperBoxThickness);

        Sprite paperSprite = EnsureOfficeSprite("paper", EnsureFormStyle().paper, 150, 200);
        Material paperMaterial = EnsureMaterial("Paper", "Universal Render Pipeline/Lit", m =>
        {
            m.SetTexture("_BaseMap", paperSprite.texture);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
        });
        PrimitivePart(sheet, "Paper", PrimitiveType.Quad, Vector3.zero, new Vector3(size.x, size.y, 1f), paperMaterial);
        MeshRenderer paper = sheet.Find("Paper").GetComponent<MeshRenderer>();
        paper.shadowCastingMode = ShadowCastingMode.Off;
        Material examineMaterial = CelMaterial("Paper_ExamineCel", paperSprite.texture, 0f);

        // The photo frame: PhotoAspect by 1 (the paper scales it to its form's photo cell) with the crop sprites one unit tall inside.
        Transform frame = EnsureChild(sheet, "PhotoSlot");
        frame.localPosition = new Vector3(0f, 0f, -0.0005f);
        PrimitivePart(frame, "Frame", PrimitiveType.Quad, Vector3.zero, new Vector3(LookCanvas.PhotoAspect, 1f, 1f), LitMaterial("Paper_PhotoFrame", PhotoGrey, 0.1f));
        frame.Find("Frame").GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        Transform portrait = EnsureChild(frame, "Photo");
        portrait.localPosition = new Vector3(0f, 0f, -PaperLayers.PhotoInset); // the stacking rule: every part under PaperLayers.PartsDepth
        portrait.localRotation = Quaternion.identity;
        portrait.localScale = Vector3.one * PhotoFill;
        LookSpriteStack stack = portrait.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, portrait, 1, true);
        frame.gameObject.SetActive(false);

        PaperPrint print = BuildPaperPrint(sheet);

        Clickable click = root.gameObject.AddComponent<Clickable>();
        click.SetOutline(new Renderer[] { paper });
        DeskDraggable drag = root.gameObject.AddComponent<DeskDraggable>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", box);
        soDrag.ApplyModifiedProperties();
        DeskPaperDepth paperDepth = DepthParts(paper.transform, drag, null, true);

        DeskDocument doc = root.gameObject.AddComponent<DeskDocument>();
        var so = new SerializedObject(doc);
        SetRef(so, "sheet", sheet);
        SetRef(so, "photoSlot", frame.gameObject);
        SetRef(so, "photo", stack);
        SetRef(so, "textTemplate", print.Text);
        SetRef(so, "fills", print.Fills);
        SetRef(so, "lines", print.Lines);
        SetRef(so, "seal", print.Seal);
        SetRef(so, "highlightTemplate", print.Slot);
        SetRef(so, "style", EnsureFormStyle());
        SetRef(so, "paperQuad", paper);
        SetRef(so, "depth", paperDepth);
        SetRef(so, "examineMaterial", examineMaterial);
        SetRef(so, "click", click);
        SetRef(so, "drag", drag);
        so.ApplyModifiedProperties();

        var soClick = new SerializedObject(click);
        SerializedArrays.Set(soClick, "outline", new Object[] { paper });
        soClick.ApplyModifiedProperties();

        root.gameObject.SetActive(false);
        return doc;
    }

    /// <summary>A text lying on the paper sheet (in the sheet's plane, just above it), auto-sized into a box of metres.</summary>
    private static TextMeshPro PaperText(Transform sheet, string name, string content, Vector2 centre, Vector2 box, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(sheet, false);
        go.transform.localPosition = new Vector3(centre.x, centre.y, -0.0006f);
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        ((RectTransform)go.transform).sizeDelta = box;
        tmp.text = content;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = 0.5f;
        tmp.fontSizeMin = 0.05f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Ink;
        if (bold)
            tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    /// <summary>
    /// A note floating in the office (the binder places it and turns it to the
    /// camera): light text with a dark outline, auto-sized, inactive until
    /// shown; with <paramref name="backing"/> (the day-1 hints) a plate
    /// behind it fitted to the text by NoteBacking: the UI kit's card with the
    /// line in ink (run 7), else a dark Backing quad in FloatingNote_Backing.
    /// Idempotent.
    /// </summary>
    private static TextMeshPro FloatingNote(Transform parent, string name, bool backing = false)
    {
        DestroyChildIfPresent(parent, name);
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(1.1f, 0.09f);
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        tmp.text = string.Empty;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = 0.6f;
        tmp.fontSizeMin = 0.1f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = NoteInk;
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSharedMaterial = NoteMaterial(tmp.font);
        tmp.sortingLayerID = GameplaySortingLayerId();
        if (backing && _kit != null)
        {
            // The UI kit's card behind the note (run 7; sheet 05's hint plate), its line in the kit's ink and bold body face.
            tmp.font = _kit.bodyBoldFont;
            tmp.fontSharedMaterial = _kit.bodyBoldFont.material;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = _kit.inkOnLight;
            var card = new GameObject("Backing", typeof(SpriteRenderer));
            card.transform.SetParent(go.transform, false);
            SpriteRenderer cardSprite = card.GetComponent<SpriteRenderer>();
            cardSprite.sprite = _kit.Get("tooltip");
            cardSprite.drawMode = SpriteDrawMode.Sliced;
            cardSprite.sortingLayerID = tmp.sortingLayerID;
            cardSprite.sortingOrder = -1;
            cardSprite.shadowCastingMode = ShadowCastingMode.Off;
            NoteBacking fit = go.AddComponent<NoteBacking>();
            var so = new SerializedObject(fit);
            SetRef(so, "text", tmp);
            SetRef(so, "backing", card.transform);
            so.FindProperty("margin").vector2Value = NoteBackingMargin + Vector2.one * (_kit.spritePad / 100f * NoteCardScale);
            so.FindProperty("sliceScale").floatValue = NoteCardScale;
            so.ApplyModifiedProperties();
        }
        else if (backing)
        {
            PrimitivePart(go.transform, "Backing", PrimitiveType.Quad, new Vector3(0f, 0f, 0.002f), Vector3.one, NoteBackingMaterial());
            MeshRenderer plate = go.transform.Find("Backing").GetComponent<MeshRenderer>();
            plate.shadowCastingMode = ShadowCastingMode.Off;
            plate.receiveShadows = false;
            plate.sortingLayerID = tmp.sortingLayerID;
            NoteBacking fit = go.AddComponent<NoteBacking>();
            var so = new SerializedObject(fit);
            SetRef(so, "text", tmp);
            SetRef(so, "backing", plate.transform);
            so.FindProperty("margin").vector2Value = NoteBackingMargin;
            so.ApplyModifiedProperties();
        }
        go.SetActive(false);
        return tmp;
    }

    /// <summary>The metres one sprite unit of the kit's card behind a floating note is drawn at (its corners about a centimetre round).</summary>
    private const float NoteCardScale = 0.015f;

    /// <summary>The day-1 notes' backing material: unlit, transparent, dark, drawn before the notes' text. Created once; a designer's edits are kept.</summary>
    private static Material NoteBackingMaterial() =>
        EnsureMaterial("FloatingNote_Backing", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", NoteBackingColour);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", -10f);
            BaseShaderGUI.SetMaterialKeywords(m);
        });

    /// <summary>
    /// The floating notes' shared font material: the font's own with a dark
    /// outline (setting a text's outline in edit mode would instance its
    /// material into the scene). Created once; a designer's edits are kept.
    /// </summary>
    private static Material NoteMaterial(TMP_FontAsset font)
    {
        string path = $"{GameplayArtFolder}/Materials/FloatingNote_Outline.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null || font == null)
            return material;

        PlaceholderPng.EnsureFolderTree($"{GameplayArtFolder}/Materials");
        material = new Material(font.material) { name = "FloatingNote_Outline" };
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(20, 22, 26, 255));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>The folder cover's printed words (English, their doc.* strings translate them), each its place on the art (shares x0, y0, x1, y1), its ink and its largest size: painted out of the clean face (ArtSlots.RulebookFolderClean, track LANG's art step).</summary>
    private static readonly (string english, Vector4 place, Color ink, float size)[] RulebookCoverWords =
    {
        ("TEMPORAL CUSTOMS BUREAU", new Vector4(0.1699f, 0.4922f, 0.3945f, 0.6944f), new Color(0.184f, 0.208f, 0.251f), 0.3f),
        ("CLERK'S RULEBOOK · DESK 3", new Vector4(0.1597f, 0.7318f, 0.3999f, 0.77f), new Color(0.486f, 0.224f, 0.18f), 0.12f),
    };

    /// <inheritdoc cref="RulebookSheet"/>
    private static readonly Vector4[] RulebookRowPlaces =
    {
        new Vector4(0.57167f, 0.25926f, 0.9f, 0.34222f), new Vector4(0.57167f, 0.35407f, 0.9f, 0.43704f), new Vector4(0.57167f, 0.44889f, 0.9f, 0.53185f),
        new Vector4(0.57167f, 0.5437f, 0.9f, 0.62667f), new Vector4(0.57167f, 0.63852f, 0.9f, 0.72148f), new Vector4(0.57167f, 0.73407f, 0.9f, 0.81704f)
    };

    /// <summary>The folder's four tabs on its top edge, RULES, PAPERS, GUIDE, SEALS (their art: ArtSlots.RulebookTab, the art's words painted out), and the place of each tab's word, as shares of the folder art from its top-left.</summary>
    private static readonly Vector4[] RulebookTabPlaces =
    {
        new Vector4(0.16583f, 0.01037f, 0.36333f, 0.07259f), new Vector4(0.36167f, 0.01037f, 0.55583f, 0.07259f),
        new Vector4(0.55917f, 0.01037f, 0.74417f, 0.07259f), new Vector4(0.74833f, 0.01037f, 0.92167f, 0.07259f)
    }, RulebookTabWords =
    {
        new Vector4(0.19f, 0.012f, 0.34f, 0.07f), new Vector4(0.385f, 0.012f, 0.535f, 0.07f),
        new Vector4(0.585f, 0.012f, 0.72f, 0.07f), new Vector4(0.77f, 0.012f, 0.9f, 0.07f)
    };

    /// <summary>How far a tab's plate lies over the folder's plane (metres): a raised card, its model's edge reaching down to the plane, its word over it.</summary>
    private const float RulebookTabPlateLift = 0.0006f;

    /// <summary>The tabs' art names in tab order (ArtSlots.RulebookTab).</summary>
    private static readonly string[] RulebookTabArt = { "rules", "papers", "guide", "seals" };

    /// <summary>The RULES page's rows (the art's ruled lines), the PAPERS page's rows and their pitch (the papers not handed over, to flag missing).</summary>
    private const int RulebookRows = 6;

    private const int RulebookPaperRows = 4;

    private const float RulebookPaperPitch = 0.036f;

    /// <summary>The rulebook's SEALS page: its rows (one per office) and their pitch (metres).</summary>
    private const int RulebookSealRows = 8;

    private const float RulebookSealPitch = 0.0185f;

    private static readonly Vector2 RulebookGuideButton = new Vector2(0.07f, 0.024f);

    /// <summary>The red of the rulebook's NEW marks.</summary>
    private static readonly Color RulebookNewInk = new Color(0.72f, 0.1f, 0.08f);

    /// <summary>A paper's and a rulebook part's click box thickness (metres, before a paper's zone scale): thin, so the papers' stack (DeskConfigSO.paperStackStep a place) alone decides which of two overlapping papers, or a paper and the rulebook, a click or a stamp meets (Saleh 2026-10-06: documents clipping).</summary>
    private const float PaperBoxThickness = 0.0004f;

    /// <summary>The contact shadows' render queue (TimeDesk/PaperDepth): after everything printed (the paper art at 3005), so a shadow lies over the paper under it, print and all, and only depth hides it.</summary>
    private const int ShadowQueue = 3010;

    /// <summary>
    /// The mesh <paramref name="mesh"/> of the document model <paramref name="model"/>
    /// (Documents/&lt;model&gt;.fbx: each model's Top, its readable face in the
    /// face quad's unit space, and its Body), the model imported without
    /// materials or animation (the builder gives the materials).
    /// </summary>
    private static Mesh DocumentMesh(string model, string mesh)
    {
        string path = $"{DocumentsFolder}/{model}.fbx";
        if (AssetImporter.GetAtPath(path) is ModelImporter importer
            && (importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.importAnimation || importer.importCameras || importer.importLights))
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }
        Mesh found = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m => m.name == mesh);
        if (found == null)
            Debug.LogError($"[TimeDesk] {path} has no mesh '{mesh}': run tools/props/documents/docs_models.py.");
        return found;
    }

    /// <summary>A cel material (TimeDesk/DocumentCel) showing <paramref name="art"/> (a face's art, or the edge atlas), cut out at <paramref name="cutoff"/> (0: none); made once.</summary>
    private static Material CelMaterial(string name, Texture2D art, float cutoff) =>
        EnsureMaterial(name, "TimeDesk/DocumentCel", m =>
        {
            m.SetTexture("_BaseMap", art);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Cutoff", cutoff);
        });

    /// <summary>The models' sides, page blocks, stitches and rims (the edge atlas, Documents/docs_edges.png, in the cel shader).</summary>
    private static Material EdgesMaterial() => CelMaterial("Docs_Edges", AssetDatabase.LoadAssetAtPath<Texture2D>($"{DocumentsFolder}/docs_edges.png"), 0f);

    /// <summary>The contact shadows' material (TimeDesk/PaperDepth: blended, writing no depth; Paper_Shadow), its queue kept.</summary>
    private static Material ShadowMaterial()
    {
        Material m = EnsureMaterial("Paper_Shadow", "TimeDesk/PaperDepth", mat =>
        {
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
        });
        if (m != null && m.renderQueue != ShadowQueue)
        {
            m.renderQueue = ShadowQueue;
            EditorUtility.SetDirty(m);
        }
        return m;
    }

    /// <summary>
    /// The modelled body of a thing on the desk under <paramref name="face"/>
    /// (a paper's quad, the folder's card or a tab's plate; in its unit space;
    /// Saleh 2026-10-17: "use canva and blender to make actual 3d documents"):
    /// a Depth child with its DeskPaperDepth (lifting its shadow while
    /// <paramref name="drag"/> holds it, if any), its Body (the model's sides,
    /// <paramref name="body"/>; a paper's is chosen when it binds) and its
    /// Shadow (runtime-built), casting and taking no shadow and taking no
    /// click. A paper's (<paramref name="paper"/>) also carries the papers'
    /// models, and its face takes its kind's Top when it binds.
    /// </summary>
    private static DeskPaperDepth DepthParts(Transform face, DeskDraggable drag, Mesh body, bool paper)
    {
        var host = new GameObject("Depth");
        host.transform.SetParent(face, false);
        host.layer = face.gameObject.layer;
        DeskPaperDepth depth = host.AddComponent<DeskPaperDepth>();
        MeshFilter Part(string name, Material material, Mesh mesh)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(host.transform, false);
            go.layer = face.gameObject.layer;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            MeshFilter filter = go.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            return filter;
        }
        var so = new SerializedObject(depth);
        SetRef(so, "body", Part("Body", EdgesMaterial(), body));
        SetRef(so, "shadow", Part("Shadow", ShadowMaterial(), null));
        SetRef(so, "drag", drag);
        SetRef(so, "face", face.GetComponent<MeshFilter>()); // its Top: a paper's takes its kind's when it binds; every model sags through it while it moves
        if (paper)
        {
            SetRef(so, "sheetTop", DocumentMesh("Sheet", "SheetTop"));
            SetRef(so, "sheetBody", DocumentMesh("Sheet", "SheetBody"));
            SetRef(so, "bookletTop", DocumentMesh("Booklet", "BookletTop"));
            SetRef(so, "bookletBody", DocumentMesh("Booklet", "BookletBody"));
            SetRef(so, "cardTop", DocumentMesh("Card", "CardTop"));
            SetRef(so, "cardBody", DocumentMesh("Card", "CardBody"));
            SetRef(so, "bookletShade", AssetDatabase.LoadAssetAtPath<Texture2D>($"{DocumentsFolder}/booklet_shade.png"));
        }
        so.ApplyModifiedProperties();
        return depth;
    }

    /// <summary>
    /// The rulebook on the desk, Papers, Please's booklet (Saleh 2026-10-06),
    /// as Saleh's Canva folder (run 7: "folder too"), rebuilt each run:
    /// Office/Rulebook (the DeskRulebook and its DeskDraggable; the office
    /// binder lays it beside the mat on the desk plane) and its Booklet (lifted
    /// by the papers' stack: DeskRulebook.SetLift): the open manila folder lying
    /// face up (its art, cut out at its outline; the left cover is the art's
    /// own, static), its click box (CardClick: its outline and the drag's
    /// proxy), the four tabs on its top edge at the art's places (RULES, PAPERS,
    /// GUIDE, SEALS: the tab's art, its word printed live and a click box each;
    /// GUIDE's NEW badge above it; SEALS shows only once introduced), and the
    /// pages printed on its right sheet (Page): the RULES page (its title
    /// between the sheet's two printed rules, a row on each of its six ruled
    /// lines, each a click box on the Interactable layer over its text, two
    /// lines at most, and the line for a day with no directive), the PAPERS
    /// page (its heading, four rows, click boxes over their texts: the papers
    /// not handed over, to flag missing, and the line when none is left), the
    /// GUIDE page (the help guide: a sheet's title, its NEW mark, its text,
    /// PREV and NEXT click boxes and its number) and the SEALS page. Returns it.
    /// </summary>
    private static DeskRulebook BuildRulebook(Transform office, DeskSurface surface, DeskCounter counter)
    {
        DestroyChildIfPresent(office, "Rulebook");
        Transform book = EnsureChild(office, "Rulebook");
        Transform booklet = EnsureChild(book, "Booklet"); // lifted by the papers' stack (DeskController), the root on the desk plane
        // The folder is its model (Saleh 2026-10-17: "use canva and blender to make actual 3d documents"): its art on its top (the rules print on its top sheet, on this plane), its boards, spine, sheets, flaps and clip under it.
        GameObject card = PrimitivePart(booklet, "Card", PrimitiveType.Quad, Vector3.zero, new Vector3(RulebookFolderSize.x, RulebookFolderSize.y, 1f),
                                        CelMaterial("Rulebook_FolderModel", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtSlots.AssetRoot + ArtSlots.RulebookFolder + ".png"), 0f));
        card.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        card.GetComponent<MeshFilter>().sharedMesh = DocumentMesh("Folder", "FolderTop");
        Material folderMaterial = card.GetComponent<MeshRenderer>().sharedMaterial;
        Texture2D folderShade = AssetDatabase.LoadAssetAtPath<Texture2D>($"{DocumentsFolder}/folder_shade.png");
        if (folderMaterial != null && folderMaterial.GetTexture("_ShadeMap") != folderShade)
        {
            folderMaterial.SetTexture("_ShadeMap", folderShade); // its sheets', flaps' and clip's shadows on it, baked with the model
            EditorUtility.SetDirty(folderMaterial);
        }
        Color ink = new Color(0.13f, 0.12f, 0.15f);

        // The folder cover's words, printed over its clean face only while a culture's language is read (DeskRulebook.ShowCover).
        var cover = new List<TMP_Text>();
        for (int i = 0; i < RulebookCoverWords.Length; i++)
        {
            (string english, Vector4 place, Color coverInk, float size) = RulebookCoverWords[i];
            TextMeshPro word = FlatText(booklet, "Cover" + (i + 1), FolderPoint(place) + new Vector3(0f, 0.0003f, 0f), FolderSize(place), size, coverInk, FontStyles.Bold);
            word.text = english;
            word.textWrappingMode = TextWrappingModes.Normal;
            word.gameObject.SetActive(false);
            cover.Add(word);
        }

        // The tabs on the folder's top edge, at the art's places, each its own art (shown only while its page is there: DeskRulebook).
        var tabs = new List<Clickable>();
        var plates = new List<Renderer>();
        var tabDepths = new List<DeskPaperDepth>();
        string[] tabKeys = { "desk.rulebook.tabRules", "desk.rulebook.tabPapers", "desk.rulebook.tabGuide", "desk.rulebook.tabSeals" };
        for (int i = 0; i < tabKeys.Length; i++)
        {
            Vector3 at = FolderPoint(RulebookTabPlaces[i]) + new Vector3(0f, 0.0002f, 0f);
            Vector2 size = FolderSize(RulebookTabPlaces[i]);
            // The plate is a raised card, its model (its art on top, its edge down to the folder's plane; its shadow: DeskRulebook.ShowDepth), under its word.
            GameObject plate = PrimitivePart(booklet, "TabPlate" + (i + 1), PrimitiveType.Quad, FolderPoint(RulebookTabPlaces[i]) + new Vector3(0f, RulebookTabPlateLift, 0f), new Vector3(size.x, size.y, 1f),
                                             CelMaterial("Rulebook_TabModel_" + RulebookTabArt[i], AssetDatabase.LoadAssetAtPath<Texture2D>(ArtSlots.AssetRoot + ArtSlots.RulebookTab(RulebookTabArt[i]) + ".png"), 0.5f));
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plate.GetComponent<MeshFilter>().sharedMesh = DocumentMesh("FolderTab", "FolderTabTop");
            plates.Add(plate.GetComponent<Renderer>());
            tabDepths.Add(DepthParts(plate.transform, null, DocumentMesh("FolderTab", "FolderTabBody"), false));
            Clickable tab = EnsureClickBox(booklet, "Tab" + (i + 1));
            tab.transform.localPosition = at + new Vector3(0f, 0.0004f, 0f);
            var box = tab.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(size.x, PaperBoxThickness, size.y);
            tab.SetOutline(new[] { plate.GetComponent<Renderer>() });
            TextMeshPro word = FlatText(tab.transform, "Text", FolderPoint(RulebookTabWords[i]) - at + new Vector3(0f, 0.0004f, 0f), FolderSize(RulebookTabWords[i]), 0.2f, ink, FontStyles.Bold);
            word.text = UiText.Get(tabKeys[i]);
            Keyed(word, tabKeys[i]);
            tabs.Add(tab);
        }

        // The pages print on the folder's right sheet.
        Transform page = EnsureChild(booklet, "Page");
        page.localPosition = FolderPoint(RulebookSheet);

        // The RULES page: the title between the sheet's printed rules, a row on each ruled line.
        Transform rulesPage = EnsureChild(page, "RulesPage");
        TextMeshPro title = FlatText(rulesPage, "Title", FolderPoint(RulebookHeading) - page.localPosition + new Vector3(0f, 0.0006f, 0f), FolderSize(RulebookHeading), 0.2f, ink, FontStyles.Bold);
        var rows = new List<Clickable>();
        for (int i = 0; i < RulebookRows; i++)
        {
            Vector2 size = FolderSize(RulebookRowPlaces[i]);
            Clickable row = EnsureClickBox(rulesPage, "Row" + (i + 1));
            row.transform.localPosition = FolderPoint(RulebookRowPlaces[i]) - page.localPosition + new Vector3(0f, 0.0006f, 0f);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(size.x, PaperBoxThickness, size.y);
            TextMeshPro text = FlatText(row.transform, "Text", Vector3.zero, size - new Vector2(0.004f, 0.002f), 0.16f, ink, FontStyles.Normal);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.BottomLeft;
            rows.Add(row);
        }
        TextMeshPro none = FlatText(rulesPage, "None", FolderPoint(RulebookRowPlaces[0]) - page.localPosition + new Vector3(0f, 0.0006f, 0f), FolderSize(RulebookRowPlaces[0]), 0.13f, ink, FontStyles.Italic);

        // The PAPERS page: its heading and a row per paper not handed over (a click flags it missing).
        Transform papersPage = EnsureChild(page, "PapersPage");
        TextMeshPro papersTitle = FlatText(papersPage, "PapersTitle", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.16f, ink, FontStyles.Bold);
        var paperRows = new List<Clickable>();
        for (int i = 0; i < RulebookPaperRows; i++)
        {
            Clickable row = EnsureClickBox(papersPage, "Paper" + (i + 1));
            row.transform.localPosition = new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f - i * RulebookPaperPitch);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookSize.x - 0.016f, PaperBoxThickness, RulebookPaperPitch - 0.004f);
            TextMeshPro text = FlatText(row.transform, "Text", Vector3.zero, new Vector2(RulebookSize.x - 0.024f, RulebookPaperPitch - 0.006f), 0.12f, ink, FontStyles.Normal);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            paperRows.Add(row);
        }
        TextMeshPro papersNone = FlatText(papersPage, "None", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f), new Vector2(RulebookSize.x - 0.024f, 0.03f), 0.13f, ink, FontStyles.Italic);
        papersPage.gameObject.SetActive(false);

        // The GUIDE page: the help guide's sheet (DeskRulebook.SetGuide), and the tab's NEW badge.
        Transform guidePage = EnsureChild(page, "GuidePage");
        TextMeshPro guideTitle = FlatText(guidePage, "Title", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.15f, ink, FontStyles.Bold);
        TextMeshPro guideNew = FlatText(guidePage, "New", new Vector3(RulebookSize.x / 2f - 0.035f, 0.0006f, RulebookSize.y / 2f - 0.046f), new Vector2(0.05f, 0.018f), 0.09f, RulebookNewInk, FontStyles.Bold);
        guideNew.text = UiText.Get("desk.guide.new");
        Keyed(guideNew, "desk.guide.new");
        TextMeshPro guideBody = FlatText(guidePage, "Body", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.048f - RulebookGuideBody.y / 2f), RulebookGuideBody, 0.15f, ink, FontStyles.Normal);
        guideBody.textWrappingMode = TextWrappingModes.Normal;
        guideBody.alignment = TextAlignmentOptions.TopLeft;
        guideBody.richText = true;
        float footer = -RulebookSize.y / 2f + 0.018f;
        Clickable GuideButton(string name, float x, string key)
        {
            Clickable button = EnsureClickBox(guidePage, name);
            button.transform.localPosition = new Vector3(x, 0.0006f, footer);
            var box = button.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookGuideButton.x, PaperBoxThickness, RulebookGuideButton.y);
            TextMeshPro word = FlatText(button.transform, "Text", Vector3.zero, RulebookGuideButton, 0.1f, ink, FontStyles.Bold);
            word.text = UiText.Get(key);
            Keyed(word, key);
            return button;
        }
        Clickable guidePrev = GuideButton("Prev", -RulebookSize.x / 2f + 0.012f + RulebookGuideButton.x / 2f, "desk.guide.prev");
        Clickable guideNext = GuideButton("Next", RulebookSize.x / 2f - 0.012f - RulebookGuideButton.x / 2f, "desk.guide.next");
        TextMeshPro guideNumber = FlatText(guidePage, "Number", new Vector3(0f, 0.0006f, footer), new Vector2(0.07f, 0.02f), 0.09f, ink, FontStyles.Normal);
        guidePage.gameObject.SetActive(false);
        Transform guideTab = tabs[DeskRulebook.GuidePageIndex].transform;
        TextMeshPro guideBadge = FlatText(booklet, "GuideBadge", guideTab.localPosition + new Vector3(0f, 0f, FolderSize(RulebookTabPlaces[DeskRulebook.GuidePageIndex]).y / 2f + 0.011f),
                                          new Vector2(0.05f, 0.018f), 0.09f, RulebookNewInk, FontStyles.Bold);
        guideBadge.text = UiText.Get("desk.guide.new");
        Keyed(guideBadge, "desk.guide.new");
        guideBadge.gameObject.SetActive(false);

        // The SEALS page: the Seal Register at the desk (DeskRulebook.ShowSeals), a row per office: its seal's mark and legend, its name.
        Transform sealsPage = EnsureChild(page, "SealsPage");
        TextMeshPro sealsTitle = FlatText(sealsPage, "Title", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.15f, ink, FontStyles.Bold);
        sealsTitle.text = UiText.Get("desk.rulebook.seals");
        Keyed(sealsTitle, "desk.rulebook.seals");
        Material sealMaterial = FormSealMaterial();
        var sealRows = new List<Clickable>();
        var sealMarks = new List<Renderer>();
        var sealLegends = new List<TextMeshPro>();
        var sealNames = new List<TextMeshPro>();
        float markSize = RulebookSealPitch - 0.005f;
        for (int i = 0; i < RulebookSealRows; i++)
        {
            Clickable row = EnsureClickBox(sealsPage, "Seal" + (i + 1));
            row.transform.localPosition = new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.052f - i * RulebookSealPitch);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookSize.x - 0.016f, PaperBoxThickness, RulebookSealPitch - 0.003f);
            float markX = -RulebookSize.x / 2f + 0.016f + markSize / 2f;
            GameObject mark = PrimitivePart(row.transform, "Mark", PrimitiveType.Quad, new Vector3(markX, 0.0002f, 0f), new Vector3(markSize, markSize, 1f), sealMaterial);
            mark.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mark.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            TextMeshPro legend = FlatText(row.transform, "Legend", new Vector3(markX, 0.0004f, 0f), new Vector2(markSize * 0.62f, markSize * 0.4f), 0.06f, ink, FontStyles.Bold);
            float nameX = markX + markSize / 2f + 0.008f;
            float nameWidth = RulebookSize.x / 2f - 0.012f - nameX;
            TextMeshPro name = FlatText(row.transform, "Text", new Vector3(nameX + nameWidth / 2f, 0f, 0f), new Vector2(nameWidth, RulebookSealPitch - 0.006f), 0.12f, ink, FontStyles.Normal);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            sealRows.Add(row);
            sealMarks.Add(mark.GetComponent<Renderer>());
            sealLegends.Add(legend);
            sealNames.Add(name);
        }
        sealsPage.gameObject.SetActive(false);

        Clickable cardClick = EnsureClickBox(booklet, "CardClick");
        var cardBox = cardClick.GetComponent<BoxCollider>();
        cardBox.center = new Vector3(0f, -0.0002f, 0f);
        cardBox.size = new Vector3(RulebookFolderSize.x, 0.0004f, RulebookFolderSize.y);
        cardClick.SetOutline(new[] { card.GetComponent<Renderer>() });

        DeskDraggable drag = GetOrAdd<DeskDraggable>(book.gameObject);
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", cardBox);
        soDrag.ApplyModifiedProperties();
        DeskPaperDepth folderDepth = DepthParts(card.transform, drag, DocumentMesh("Folder", "FolderBody"), false);

        DeskRulebook rulebook = GetOrAdd<DeskRulebook>(book.gameObject);
        var so = new SerializedObject(rulebook);
        SetRef(so, "booklet", booklet);
        SetRef(so, "depth", folderDepth);
        SerializedArrays.Set(so, "tabDepths", tabDepths);
        SetRef(so, "card", cardClick);
        SetRef(so, "folderFace", card.GetComponent<Renderer>());
        SerializedArrays.Set(so, "coverWords", cover);
        SetRef(so, "title", title);
        SerializedArrays.Set(so, "rows", rows);
        so.FindProperty("sheetSize").vector2Value = RulebookSize;
        SetRef(so, "none", none);
        SetRef(so, "papersTitle", papersTitle);
        SerializedArrays.Set(so, "paperRows", paperRows);
        SetRef(so, "papersNone", papersNone);
        SetRef(so, "rulesPage", rulesPage.gameObject);
        SetRef(so, "papersPage", papersPage.gameObject);
        SetRef(so, "guidePage", guidePage.gameObject);
        SetRef(so, "guideTitle", guideTitle);
        SetRef(so, "guideBody", guideBody);
        SetRef(so, "guideNumber", guideNumber);
        SetRef(so, "guideNew", guideNew.gameObject);
        SetRef(so, "guidePrev", guidePrev);
        SetRef(so, "guideNext", guideNext);
        SetRef(so, "guideBadge", guideBadge.gameObject);
        SetRef(so, "sealsPage", sealsPage.gameObject);
        SerializedArrays.Set(so, "sealRows", sealRows);
        SerializedArrays.Set(so, "sealMarks", sealMarks);
        SerializedArrays.Set(so, "sealLegends", sealLegends);
        SerializedArrays.Set(so, "sealNames", sealNames);
        SerializedArrays.Set(so, "tabs", tabs);
        SerializedArrays.Set(so, "tabPlates", plates);
        SetRef(so, "drag", drag);
        SetRef(so, "surface", surface);
        SetRef(so, "counter", counter);
        so.ApplyModifiedProperties();
        return rulebook;
    }

    /// <summary>The centre of a place on the folder art (shares from its top-left: x0, y0, x1, y1) in the booklet's space (metres; the art's top at the folder's far edge).</summary>
    private static Vector3 FolderPoint(Vector4 place) =>
        new Vector3(((place.x + place.z) / 2f - 0.5f) * RulebookFolderSize.x, 0f, (0.5f - (place.y + place.w) / 2f) * RulebookFolderSize.y);

    /// <summary>The size of a place on the folder art in metres (width by depth).</summary>
    private static Vector2 FolderSize(Vector4 place) => new Vector2((place.z - place.x) * RulebookFolderSize.x, (place.w - place.y) * RulebookFolderSize.y);
}
