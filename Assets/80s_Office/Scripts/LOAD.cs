using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LOAD : MonoBehaviour
{
    // Start is called before the first frame update
    void Start () {
        SceneManager.LoadScene("Office_GroundFloor", LoadSceneMode.Single);
        SceneManager.LoadScene("Office_UpperFloor", LoadSceneMode.Additive);
		SceneManager.LoadScene("Office_Exterior", LoadSceneMode.Additive);
		
    }

    // Update is called once per frame
    void Update () {
//SceneManager.LoadScene("Office_GroundFloor", LoadSceneMode.Single);
    }
}
