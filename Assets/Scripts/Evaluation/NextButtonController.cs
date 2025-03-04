using UnityEngine;
using UnityEngine.SceneManagement;

public class NextButtonController : MonoBehaviour
{
    private void Start()
    {
        SceneController.SetupScenes();
        Debug.Log("Scene loaded " + SceneManager.GetActiveScene().buildIndex);
    }

    public void NextScene()
    {
        SceneController.NextScene(0);
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("run", PlayerPrefs.GetInt("run", 0) + 1);
    }
}
