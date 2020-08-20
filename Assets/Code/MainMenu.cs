using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    PlayerInput player;

    [SerializeField]
    Text levelTimeMessage;

    void Start()
    {
        if (levelTimeMessage == null)
        {
            Debug.LogError("Must provide Text to MainMenu.");
        }

        player = FindObjectOfType<PlayerInput>();
        Time.timeScale = 0.0f;
        player.windSource.enabled = false;
    }

    public void StartGame()
    {
        Time.timeScale = 1.0f;
        player.windSource.enabled = true;
        player.cursorMobile.gameObject.SetActive(player.controlMethod != PlayerInput.ControlMethod.Mouse);
    }

    public void FinishGame(float finalTime)
    {
        player.rb.velocity = Vector3.zero;
        player.windSource.enabled = false;
        player.SetCursor(PlayerInput.CursorDir.Centre);
        player.cursorMobile.gameObject.SetActive(false);
        Time.timeScale = 0.0f;

        levelTimeMessage.text += (Mathf.RoundToInt(1000 * finalTime) / 1000.0f).ToString();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
