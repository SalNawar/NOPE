using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Object-reference array writes through SerializedObject, shared by Generate
/// World and Build Office UI (one implementation for both tools).
/// </summary>
internal static class SerializedArrays
{
    /// <summary>Sets an object-reference array property to exactly <paramref name="values"/>; logs an error when the property does not exist.</summary>
    public static void Set(SerializedObject so, string prop, IReadOnlyList<Object> values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[SerializedArrays] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }

        p.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>Removes null (deleted) references from an object-reference array property; logs an error when the property does not exist.</summary>
    public static void DropMissing(SerializedObject so, string prop)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[SerializedArrays] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }

        var kept = new List<Object>();
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null)
                kept.Add(o);
        }

        Set(so, prop, kept);
    }
}
