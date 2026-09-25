using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScenes : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("Office_UpperFloor", LoadSceneMode.Additive);
        SceneManager.LoadScene("Office_GroundFloor", LoadSceneMode.Additive);
        SceneManager.LoadScene("Office_Exterior", LoadSceneMode.Additive);     

        LightProbes.Tetrahedralize();
    }
}
