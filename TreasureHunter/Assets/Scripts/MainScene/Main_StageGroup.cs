using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Main_StageGroup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text_Title;

    public static int MAX_PAGE = 1;
    private int curPage;

    private void Start()
    {
        curPage = 0;
    }

    public void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(curPage * (-1920), 0, 0), Time.deltaTime * 8);
        text_Title.text = curPage.Equals(0) ? "World1 - Forest" : "World2 - Lost Temple";
    }

    public void AddPage(int value)
    {
        curPage += value;
        if (curPage < 0) curPage = 0;
        if (curPage > MAX_PAGE) curPage = MAX_PAGE;
    }
}
