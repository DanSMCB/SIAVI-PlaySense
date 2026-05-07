using System;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void OnStartPressed()
    {
        Debug.Log("Start button pressed, loading main scene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("MainMenu");
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quit button pressed, exiting application...");
        Application.Quit();
    }
}