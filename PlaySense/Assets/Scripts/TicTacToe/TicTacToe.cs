using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TicTacToe : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;

    public GameObject player1wins;
    public GameObject player2wins;
    public GameObject draw;
    public GameObject gameover;

    public TMP_Text player1NoWins;
    public TMP_Text player2NoWins;
    public TMP_Text NoDraws;

    public Sprite xSprite;
    public Sprite oSprite;

    public Button[] buttons;
    private int[] board = new int[9];

    void Start()
    {
        player1NoWins.text = GameManager.Instance.player1Wins.ToString();
        player2NoWins.text = GameManager.Instance.player2Wins.ToString();
        NoDraws.text = GameManager.Instance.draws.ToString();
    }

    public void Play(int index)
    {
        if (!buttons[index].interactable)
        {
            Debug.Log("Posição já usada!");
            return;
        }

        Button button = buttons[index];
        Image buttonImage = button.GetComponent<Image>();

        if (player1.activeSelf)
        {
            player1.SetActive(false);
            player2.SetActive(true);

            buttonImage.sprite = xSprite;
            board[index] = 1;
        }
        else
        {
            player1.SetActive(true);
            player2.SetActive(false);

            buttonImage.sprite = oSprite;
            board[index] = 2;
        }

        button.interactable = false;

        CheckWinner();
    }

    void CheckWinner()
    {
        int[,] winConditions = new int[,]
        {
            {0,1,2},
            {3,4,5},
            {6,7,8},
            {0,3,6},
            {1,4,7},
            {2,5,8},
            {0,4,8},
            {2,4,6}
        };

        for (int i = 0; i < 8; i++)
        {
            int a = winConditions[i, 0];
            int b = winConditions[i, 1];
            int c = winConditions[i, 2];

            if (board[a] != 0 &&
                board[a] == board[b] &&
                board[b] == board[c])
            {
                Debug.Log("Player " + board[a] + " Wins!");
                GameManager.Instance.AddWin(board[a]);
                if (board[a] == 1) {
                    player1wins.SetActive(true);
                    player1NoWins.text = GameManager.Instance.player1Wins.ToString();
                } else if (board[a] == 2) { 
                    player2wins.SetActive(true);

                    player2NoWins.text = GameManager.Instance.player2Wins.ToString();
                }

                lockButtons();
                gameover.SetActive(true);
                return;
            }
        }

        bool boardFull = true;

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                boardFull = false;
                break;
            }
        }

        if (boardFull)
        {
            Debug.Log("Draw!");
            GameManager.Instance.AddDraw();
            draw.SetActive(true);
            gameover.SetActive(true);
            NoDraws.text = GameManager.Instance.draws.ToString();
        }
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void Reset()
    {
        player1wins.SetActive(false);
        player2wins.SetActive(false);
        draw.SetActive(false);
        gameover.SetActive(false);

        player1.SetActive(true);
        player2.SetActive(false);

        board = new int[9];

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = true;
            buttons[i].GetComponent<Image>().sprite = null;
        }

        FindObjectOfType<GoogleSpeechManager>().StopAllCoroutines();
    }

    void lockButtons() {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }
    }
}
