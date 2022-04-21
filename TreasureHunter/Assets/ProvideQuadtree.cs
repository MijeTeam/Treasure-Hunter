using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ProvideQuadtree : MonoBehaviour
{
    StringBuilder sb = new StringBuilder();

    public string StartQuadTree(int[,] points, int startX, int startY, int size)
    {
        sb.Clear();
        ProvidingQuadTree(points, startX, startY, size);
        return sb.ToString();
    }

    private void ProvidingQuadTree(int[,] points, int startX, int startY, int size)
    {
        bool isCombined = true; // 병합 가능한가?
        int startStatus = points[startX, startY]; // 제일 처음 시작하는 곳을 기준으로 잡음
        for(int i = startX; i < startX + size; i++)
        {
            for(int j = startY; j < startY + size; j++)
            {
                // 제일 처음으로 시작하는 곳을 기준으로 잡고,
                // 검사 부위만큼 쭉 돌았을때 전부 같은지 체크.
                if(points[i, j] == startStatus)
                {
                    // 다른 불순물이 섞여있으면 병합 불가능
                    isCombined = false;
                    break;
                }
            }
            if (isCombined == false)
                break;
        }

        if(isCombined) // 병합 가능할 경우
        {
            sb.Append(startStatus);
            return;
        }

        // 아닐경우 4분할
        int halfSize = (int)(size * 0.5f);
        sb.Append("{");
        ProvidingQuadTree(points, startX, startY, halfSize);
        ProvidingQuadTree(points, startX + halfSize, startY, halfSize);
        ProvidingQuadTree(points, startX, startY + halfSize, halfSize);
        ProvidingQuadTree(points, startX + halfSize, startY + halfSize, halfSize);
        sb.Append("}");
    }
}
