using UnityEngine;
using UnityEngine.SceneManagement;

public class GUIHanlder : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnARMapBtnClick()
    {
        SceneManager.LoadScene("MapAR", LoadSceneMode.Single);
    }

    public void OnNextLocationBtnClick()
    {
        SceneManager.LoadScene("NextLocationAR", LoadSceneMode.Single);
    }
}
