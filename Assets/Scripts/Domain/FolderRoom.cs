using System;

/// <summary>
/// Where a desk paper goes to make room for the rulebook folder (D66, rule 1;
/// the director's call of 2026-11-12): when the folder comes out of its
/// pocket onto the mat or opens, a paper whose footprint overlaps the
/// folder's footprint slides along the desk's right or left, the shorter way
/// that clears the folder and keeps the paper on the mat. With no room to
/// clear it, the paper goes as far as the mat lets it. Rectangles are centre
/// and size in the desk's frame (metres: x right, y away from the chair).
/// </summary>
public static class FolderRoom
{
    /// <summary>
    /// The paper's new centre: unchanged when <paramref name="paper"/> does not
    /// overlap <paramref name="folder"/>; else moved along x by the shorter of
    /// the two moves that clear the folder and keep the paper inside
    /// <paramref name="mat"/>'s width; when neither fits, by the shorter move,
    /// stopped at the mat's edge (never moved back past where it lies).
    /// </summary>
    public static (float x, float y) Aside((float x, float y, float w, float h) paper, (float x, float y, float w, float h) folder, (float x, float y, float w, float h) mat)
    {
        float p0 = paper.x - paper.w / 2f, p1 = paper.x + paper.w / 2f;
        float f0 = folder.x - folder.w / 2f, f1 = folder.x + folder.w / 2f;
        bool overlaps = p0 < f1 && p1 > f0 && Math.Abs(paper.y - folder.y) < (paper.h + folder.h) / 2f;
        if (!overlaps)
            return (paper.x, paper.y);
        float right = f1 - p0, left = p1 - f0;
        float roomRight = Math.Max(0f, mat.x + mat.w / 2f - p1), roomLeft = Math.Max(0f, p0 - (mat.x - mat.w / 2f));
        bool rightFits = right <= roomRight, leftFits = left <= roomLeft;
        bool goRight = rightFits && leftFits ? right <= left : rightFits || (!leftFits && right <= left);
        return goRight ? (paper.x + Math.Min(right, roomRight), paper.y) : (paper.x - Math.Min(left, roomLeft), paper.y);
    }
}
