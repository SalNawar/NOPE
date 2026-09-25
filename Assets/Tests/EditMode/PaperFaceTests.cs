using NUnit.Framework;

/// <summary>
/// A desk paper's face (piece 10 X16): a title band, rows of a label over a
/// value, and on a photo document the photo at the top right beside the first
/// rows, which end left of it. Laid out in the sheet's local space (the
/// paper's height is 1, its width the paper's aspect; centre origin, y up).
/// RowAt finds the row under a point, or -1 on the title, the photo, a margin
/// or a gap. The paper is the default 0.26 x 0.34 m.
/// </summary>
public class PaperFaceTests
{
    private const float Aspect = 0.26f / 0.34f;
    private const float Eps = 1e-5f;
    private static readonly PaperFaceTuning T = new PaperFaceTuning();

    private static bool Overlaps(FaceRect a, FaceRect b) =>
        a.XMin < b.XMax - Eps && b.XMin < a.XMax - Eps && a.YMin < b.YMax - Eps && b.YMin < a.YMax - Eps;

    [Test]
    public void Rows_DoNotOverlapTitleOrPhoto()
    {
        FaceLayout face = PaperFace.Layout(6, true, Aspect, T);
        Assert.IsTrue(face.HasPhoto);
        Assert.AreEqual(6, face.Rows.Count);
        foreach (FaceRow row in face.Rows)
        {
            Assert.IsFalse(Overlaps(row.Hit, face.Title), "a row under the title");
            Assert.IsFalse(Overlaps(row.Hit, face.Photo), "a row under the photo");
            Assert.Greater(row.Label.YMin, row.Value.YMax - Eps, "the label sits over the value");
            Assert.GreaterOrEqual(row.Hit.YMax, row.Label.YMax - Eps);
            Assert.LessOrEqual(row.Hit.YMin, row.Value.YMin + Eps);
        }
        Assert.IsFalse(Overlaps(face.Title, face.Photo));
        Assert.GreaterOrEqual(face.Photo.XMin, -Aspect / 2f);
        Assert.LessOrEqual(face.Photo.XMax, Aspect / 2f);
        Assert.AreEqual(T.photoHeight * LookCanvas.PhotoAspect, face.Photo.Width, Eps, "the photo keeps the portrait's aspect");
    }

    [Test]
    public void RowsBesidePhoto_EndLeftOfIt_LaterRowsFullWidth()
    {
        FaceLayout face = PaperFace.Layout(6, true, Aspect, T);
        float fullRight = Aspect / 2f - T.sideMargin * Aspect;
        Assert.AreEqual(face.Photo.XMin - T.photoGap * Aspect, face.Rows[0].Hit.XMax, Eps);
        Assert.AreEqual(face.Photo.XMin - T.photoGap * Aspect, face.Rows[1].Hit.XMax, Eps);
        for (int i = 2; i < face.Rows.Count; i++)
            Assert.AreEqual(fullRight, face.Rows[i].Hit.XMax, Eps, $"row {i} is below the photo and full width");
        Assert.AreEqual(-Aspect / 2f + T.sideMargin * Aspect, face.Rows[0].Hit.XMin, Eps);
    }

    [Test]
    public void RowAt_InsideEachRow_ReturnsIndex()
    {
        FaceLayout face = PaperFace.Layout(4, true, Aspect, T);
        for (int i = 0; i < face.Rows.Count; i++)
        {
            FaceRect hit = face.Rows[i].Hit;
            Assert.AreEqual(i, PaperFace.RowAt(face, hit.CentreX, hit.CentreY), $"row {i} centre");
            Assert.AreEqual(i, PaperFace.RowAt(face, hit.XMin + 0.001f, hit.YMax - 0.001f), $"row {i} top-left");
            Assert.AreEqual(i, PaperFace.RowAt(face, hit.XMax - 0.001f, hit.YMin + 0.001f), $"row {i} bottom-right");
        }
    }

    [Test]
    public void RowAt_TitlePhotoMarginsGaps_MinusOne()
    {
        FaceLayout face = PaperFace.Layout(4, true, Aspect, T);
        Assert.AreEqual(-1, PaperFace.RowAt(face, face.Title.CentreX, face.Title.CentreY), "the title");
        Assert.AreEqual(-1, PaperFace.RowAt(face, face.Photo.CentreX, face.Photo.CentreY), "the photo");
        Assert.AreEqual(-1, PaperFace.RowAt(face, -Aspect / 2f + 0.001f, face.Rows[2].Hit.CentreY), "the left margin");
        Assert.AreEqual(-1, PaperFace.RowAt(face, 0f, (face.Rows[1].Hit.YMin + face.Rows[2].Hit.YMax) / 2f), "the gap between rows");
        Assert.AreEqual(-1, PaperFace.RowAt(face, 0f, -0.5f + 0.01f), "the bottom margin");
        Assert.AreEqual(-1, PaperFace.RowAt(face, 5f, 5f), "off the paper");
        Assert.AreEqual(-1, PaperFace.RowAt(null, 0f, 0f), "no layout");
    }

    [Test]
    public void Capacity_Six_WithAndWithoutPhoto()
    {
        Assert.AreEqual(6, PaperFace.Capacity(true, T));
        Assert.AreEqual(6, PaperFace.Capacity(false, T));
        Assert.AreEqual(3, PaperFace.Capacity(false, new PaperFaceTuning { rowPitch = 0.25f }), "a bigger pitch holds fewer rows");
    }

    [Test]
    public void MoreRowsThanCapacity_LaysOutCapacity()
    {
        FaceLayout face = PaperFace.Layout(9, false, Aspect, T);
        Assert.AreEqual(6, face.Rows.Count);
        Assert.GreaterOrEqual(face.Rows[5].Hit.YMin, -0.5f + T.bottomMargin - Eps, "the last row ends above the bottom margin");
        Assert.AreEqual(0, PaperFace.Layout(0, false, Aspect, T).Rows.Count);
        Assert.AreEqual(0, PaperFace.Layout(-2, false, Aspect, T).Rows.Count);
    }

    [Test]
    public void NoPhoto_FullWidthRows()
    {
        FaceLayout face = PaperFace.Layout(2, false, Aspect, T);
        Assert.IsFalse(face.HasPhoto);
        float fullLeft = -Aspect / 2f + T.sideMargin * Aspect, fullRight = -fullLeft;
        foreach (FaceRow row in face.Rows)
        {
            Assert.AreEqual(fullLeft, row.Hit.XMin, Eps);
            Assert.AreEqual(fullRight, row.Hit.XMax, Eps);
            Assert.AreEqual(fullRight - fullLeft, row.Value.Width, Eps);
        }
        Assert.AreEqual(0.5f - T.titleTop, face.Title.YMax, Eps, "the title band from the top");
        Assert.AreEqual(0.5f - T.rowsTop, face.Rows[0].Hit.YMax, Eps, "the first row at rowsTop");
        Assert.AreEqual(T.rowPitch, face.Rows[0].Hit.YMax - face.Rows[1].Hit.YMax, Eps, "rows a pitch apart");
    }
}
