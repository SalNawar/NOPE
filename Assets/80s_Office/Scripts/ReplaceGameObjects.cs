// TimeDesk (office move): an editor-only wizard outside an Editor folder breaks
// player builds; wrapped so only the editor compiles it. Art-owned file.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
 
public class ReplaceGameObjects : ScriptableWizard
{
    public bool copyValues = true;
    public bool destroy = true;
    public GameObject NewType;
    public GameObject[] OldObjects;
    //public Vector3 RottationCorrection;
    public Vector3 RottationCorrection = new Vector3(90, 0, 0);
 
    [MenuItem("Custom/Replace GameObjects")]
  
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("Replace GameObjects", typeof(ReplaceGameObjects), "Replace");
    }
 
    void OnWizardCreate()
    {
        foreach (GameObject go in OldObjects)
        {
            GameObject newObject;
            newObject = (GameObject)EditorUtility.InstantiatePrefab(NewType);
            newObject.transform.parent = go.transform;
            newObject.transform.localScale = new Vector3(1,1,1);
            newObject.transform.localPosition = new Vector3(0,0,0);
            newObject.transform.localRotation = Quaternion.Euler(RottationCorrection);
            //newObject.transform.rotation = go.transform.rotation;
            //newObject.transform.position = go.transform.position;
            //newObject.transform.rotation = go.transform.rotation;
            //Vector3 OriginalRotation = go.transform.localRotation.eulerAngles;
            //newObject.transform.localRotation = go.transform.localRotation;
            //newObject.transform.Rotate(RottationCorrection.x, RottationCorrection.y, RottationCorrection.z, Space.Self);
            //newObject.transform.localScale = go.transform.localScale;
            newObject.transform.parent = go.transform.parent;
            
            if (destroy == true)
            DestroyImmediate(go);
 
        }
 
    }
}
#endif
