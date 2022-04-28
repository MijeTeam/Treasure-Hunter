using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoot : MonoBehaviour
{
    [SerializeField] GameObject obj_pause;
    public PlayerController player;

    private float tick_timer;
    private bool isPaused;

    public void Start() => Initialize();

    public void Initialize()
    {
        tick_timer = 0;
        isPaused = false;
        obj_pause.SetActive(false);
    }

    public void TogglePause() // 일시 정지 단축키
    {
        isPaused = !isPaused;
        player.isPause = isPaused;
        obj_pause.SetActive(isPaused);
    }

    public void GoMainScene()
    {
        TransitionFade.Inst.SceneLoad("MainScene");
    }

    public void Update()
    {
        if (isPaused) return;

        tick_timer += Time.deltaTime; // 시간 더해주기
    }
}

