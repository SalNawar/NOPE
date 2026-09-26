using System;
using NUnit.Framework;

/// <summary>The content JSON layer: key order and number literals kept, Python's indent-2 layout written back byte for byte.</summary>
public class ContentJsonTests
{
    private const string Doc =
        "{\n" +
        "  \"id\": \"ann\",\n" +
        "  \"year\": -1470,\n" +
        "  \"score\": 1.0,\n" +
        "  \"chance\": 0.05,\n" +
        "  \"on\": true,\n" +
        "  \"off\": false,\n" +
        "  \"text\": \"Say \\\"hi\\\"\\n\\tC:\\\\ \\u0001 é \U0001310F\",\n" +
        "  \"none\": [],\n" +
        "  \"empty\": {},\n" +
        "  \"list\": [\n" +
        "    1,\n" +
        "    \"b\",\n" +
        "    {\n" +
        "      \"z\": 1,\n" +
        "      \"a\": 2\n" +
        "    }\n" +
        "  ]\n" +
        "}\n";

    [Test]
    public void ParseThenWrite_IsByteIdentical()
    {
        Assert.AreEqual(Doc, ContentJson.Write(ContentJson.Parse(Doc)));
    }

    [Test]
    public void Parse_KeepsKeyOrderAndNumberLiterals()
    {
        ContentNode root = ContentJson.Parse(Doc);
        Assert.AreEqual("id", root.Members[0].Key);
        Assert.AreEqual("list", root.Members[root.Members.Count - 1].Key);
        Assert.AreEqual("1.0", root.Get("score").Text);
        Assert.IsFalse(root.Get("score").IsWholeNumber);
        Assert.AreEqual("-1470", root.Get("year").Text);
        Assert.IsTrue(root.Get("year").IsWholeNumber);
        ContentNode inner = root.Get("list").Items[2];
        Assert.AreEqual("z", inner.Members[0].Key);
        Assert.AreEqual("Say \"hi\"\n\tC:\\ \u0001 é \U0001310F", root.Get("text").Text);
    }

    [Test]
    public void Write_FollowsPythonIndentTwo()
    {
        ContentNode root = ContentNode.NewObject();
        root.Add("a", ContentNode.FromString("x"));
        ContentNode list = ContentNode.NewArray();
        list.Add(ContentNode.FromNumber("2"));
        list.Add(ContentNode.FromBool(false));
        root.Add("b", list);
        root.Add("c", ContentNode.NewObject());
        Assert.AreEqual("{\n  \"a\": \"x\",\n  \"b\": [\n    2,\n    false\n  ],\n  \"c\": {}\n}\n", ContentJson.Write(root));
    }

    [TestCase(1.0, "1.0")]
    [TestCase(0.05, "0.05")]
    [TestCase(-2.5, "-2.5")]
    [TestCase(3.1, "3.1")]
    [TestCase(0.0001, "0.0001")]
    [TestCase(0.00001, "1e-05")]
    [TestCase(1.5e-7, "1.5e-07")]
    [TestCase(123456789012345.0, "123456789012345.0")]
    [TestCase(1e16, "1e+16")]
    [TestCase(2.5e22, "2.5e+22")]
    [TestCase(0.0, "0.0")]
    [TestCase(100.0, "100.0")]
    public void FloatRepr_MatchesPython(double value, string expected)
    {
        Assert.AreEqual(expected, ContentJson.FloatRepr(value));
    }

    [TestCase("{\"a\": 1,}")]
    [TestCase("{\"a\": 1, \"a\": 2}")]
    [TestCase("[1 2]")]
    [TestCase("{\"a\": 01}")]
    [TestCase("{\"a\": \"x}")]
    [TestCase("{} extra")]
    public void Parse_RejectsMalformedJson(string text)
    {
        var e = Assert.Throws<FormatException>(() => ContentJson.Parse(text));
        StringAssert.Contains("line 1", e.Message);
    }

    [Test]
    public void Same_ComparesNumbersByValue()
    {
        Assert.IsTrue(ContentNode.Same(ContentNode.FromNumber("1"), ContentNode.FromNumber("1.0")));
        Assert.IsFalse(ContentNode.Same(ContentNode.FromNumber("1"), ContentNode.FromString("1")));
        Assert.IsTrue(ContentNode.Same(ContentJson.Parse("[1, \"a\"]"), ContentJson.Parse("[1.0, \"a\"]")));
    }
}
