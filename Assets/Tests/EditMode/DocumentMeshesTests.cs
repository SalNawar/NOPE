using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// D63 (Saleh, 2026-11-09: "they clip and they are stiff just make 3d
/// objects"): every document kind is a model with depth, never a flat quad.
/// Each kind's Blender export (Assets/Art/Office/Gameplay/Documents: the
/// sheet for the forms and tickets, the booklet, the card, the folder) holds
/// vertices whose box is deeper than 0 on every axis. The test reads the
/// binary FBX's "Vertices" arrays as bytes (no engine); a checkout that holds
/// Git LFS pointers skips it.
/// </summary>
public class DocumentMeshesTests
{
    private const string Folder = "Assets/Art/Office/Gameplay/Documents/";

    private static string Root([CallerFilePath] string here = "") => File.Exists(Folder + "Card.fbx") ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");

    [TestCase("Sheet.fbx")]
    [TestCase("Booklet.fbx")]
    [TestCase("Card.fbx")]
    [TestCase("Folder.fbx")]
    public void EveryDocumentKind_IsAModel_WithThicknessOverZero(string file)
    {
        string path = Path.Combine(Root(), Folder + file);
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length > 0 && bytes[0] == (byte)'v')
            Assert.Ignore($"{path} is a Git LFS pointer in this checkout (git lfs pull fetches the model).");
        double[] min = { double.MaxValue, double.MaxValue, double.MaxValue }, max = { double.MinValue, double.MinValue, double.MinValue };
        int arrays = 0;
        byte[] key = System.Text.Encoding.ASCII.GetBytes("Vertices");
        for (int at = IndexOf(bytes, key, 0); at >= 0; at = IndexOf(bytes, key, at + key.Length))
        {
            int p = at + key.Length;
            if (p + 13 > bytes.Length || bytes[p] != (byte)'d')
                continue; // the word in a name, not a property
            int count = BitConverter.ToInt32(bytes, p + 1), encoding = BitConverter.ToInt32(bytes, p + 5), length = BitConverter.ToInt32(bytes, p + 9);
            byte[] data = new byte[count * 8];
            if (encoding == 1)
            {
                // zlib: two header bytes, then deflate
                using var deflate = new DeflateStream(new MemoryStream(bytes, p + 15, length - 2), CompressionMode.Decompress);
                int read = 0;
                while (read < data.Length)
                {
                    int n = deflate.Read(data, read, data.Length - read);
                    if (n <= 0)
                        break;
                    read += n;
                }
            }
            else
                Array.Copy(bytes, p + 13, data, 0, data.Length);
            for (int i = 0; i + 2 < count; i += 3)
                for (int axis = 0; axis < 3; axis++)
                {
                    double v = BitConverter.ToDouble(data, (i + axis) * 8);
                    min[axis] = Math.Min(min[axis], v);
                    max[axis] = Math.Max(max[axis], v);
                }
            arrays++;
        }
        Assert.Greater(arrays, 0, $"{file} holds no vertex array");
        for (int axis = 0; axis < 3; axis++)
            Assert.Greater(max[axis] - min[axis], 0.0, $"{file} is flat along axis {axis}");
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int from)
    {
        for (int i = from; i <= haystack.Length - needle.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && haystack[i + j] == needle[j])
                j++;
            if (j == needle.Length)
                return i;
        }
        return -1;
    }
}
