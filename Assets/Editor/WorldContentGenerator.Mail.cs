using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generate World's authored mail (redesign phase 25; the PC spec's ML1):
/// world_source.json "pc.mail" (each message's id, first and last day, flag,
/// sender, subject and body) is checked (Mailbox.AuthoredProblems, the
/// validator's rule) and written into the content library, where the Mail app
/// reads it beside the day's generated messages. The list is read on its
/// own from the source file, so the PC block's other tables stay the
/// Internet's (WorldContentGenerator.Pc).
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The source file's "pc.mail" list, read on its own.</summary>
    [Serializable] private sealed class MailSourceData { public PcMailData pc; }

    /// <summary>The PC block's mail list.</summary>
    [Serializable] private sealed class PcMailData { public AuthoredMail[] mail; }

    /// <summary>The authored mail as written (none when the section is missing), its problems added to <paramref name="errors"/>.</summary>
    private static AuthoredMail[] CheckMail(List<string> errors)
    {
        string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SourcePath);
        AuthoredMail[] mail;
        try
        {
            mail = JsonUtility.FromJson<MailSourceData>(File.ReadAllText(full))?.pc?.mail ?? Array.Empty<AuthoredMail>();
        }
        catch (Exception e)
        {
            errors.Add($"'{SourcePath}' \"pc.mail\" cannot be read: {e.Message}");
            return Array.Empty<AuthoredMail>();
        }
        errors.AddRange(Mailbox.AuthoredProblems(mail));
        return mail;
    }

    /// <summary>Writes the authored mail into the content library.</summary>
    private static void WireMail(ContentLibrarySO lib, AuthoredMail[] mail)
    {
        var so = new SerializedObject(lib);
        SerializedProperty list = so.FindProperty("mail");
        list.arraySize = mail.Length;
        for (int i = 0; i < mail.Length; i++)
            list.GetArrayElementAtIndex(i).boxedValue = mail[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
