using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nest : MonoBehaviour
{
    int totalShinies;
    int bankedShinies = 0;
    public int BankedShinies
    {
        get => bankedShinies;
        set
        {
            bankedShinies = value;
            if (bankedShinies == totalShinies)
            {
                menu.gameObject.SetActive(true);
                menu.FinishGame(levelTimer);
            }
        }
    }

    float levelTimer = 0.0f;
    MainMenu menu;

    void Start()
    {
        totalShinies = FindObjectsOfType<ShinyThing>().Length;
        menu = FindObjectOfType<MainMenu>();
    }

    void Update()
    {
        if (Time.timeScale != 0.0f)
        {
            levelTimer += Time.deltaTime;
        }
    }
}
