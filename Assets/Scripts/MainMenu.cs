using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadScene(1); 
    }

    public void Quit()
    {
       #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
       #else
        Application.Quit();
       #endif
    }
}
