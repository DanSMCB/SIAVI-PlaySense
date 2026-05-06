using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadTicTacToe()
    {
        SceneManager.LoadScene("TicTacToe");
    }

    public void LoadGame2()
    {
        SceneManager.LoadScene("Game2");
    }

    public void LoadGame3()
    {
        SceneManager.LoadScene("Game3");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}