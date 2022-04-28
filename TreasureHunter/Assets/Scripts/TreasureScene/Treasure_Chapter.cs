using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Treasure_Chapter : MonoBehaviour
{
    [SerializeField] GameObject[] chapterObj;
    [SerializeField] GameObject[] ContentObj;
    private int curChapter;

    private void Start()
    {
        SetChapter(0);
    }

    public void Update()
    {
        for (int i = 0; i < chapterObj.Length; i++)
            chapterObj[i].transform.localPosition = Vector2.Lerp(chapterObj[i].transform.localPosition, new Vector2(chapterObj[i].transform.localPosition.x, curChapter == i ? 278 : 217), Time.deltaTime * 10);
    }

    public void SetChapter(int number)
    {
        for (int i = 0; i < ContentObj.Length; i++)
            ContentObj[i].SetActive(false);
        ContentObj[number].SetActive(true);
        curChapter = number;
    }
}
