using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ProvideQuadtree : MonoBehaviour
{
    [SerializeField] GameObject prefab_stone;
    [SerializeField] Transform trans_stone;

    StringBuilder sb = new StringBuilder();
    QuadTreeNode rootNode = new QuadTreeNode();

    public string StartQuadTree(int[,] points, int startX, int startY, int size)
    {
        sb.Clear();
        ProvidingQuadTree(points, startX, startY, size, rootNode, 0);
        Debug.Log("Providing Done!");
        return sb.ToString();
    }

    //public void QuadtreeVisuality(QuadTreeNode node)
    //{
    //    Instantiate(prefab_stone, trans_stone).transform.position = node.myPosition;
    //}

    /// <summary>
    /// 쿼드 트리를 이용하여 노드를 나누어주는 함수
    /// </summary>
    /// <param name="points">int로 구성된 타일정보</param>
    /// <param name="startX">시작지점 X</param>
    /// <param name="startY">시작지점 Y</param>
    /// <param name="size">N*N 검색 범위 사이즈</param>
    /// <param name="node">QuadTree 데이터를 저장할 노드</param>
    /// <param name="level">몇단계까지 진행했나</param>
    /// <param name="pos">노드의 비주얼 표시 위치</param>
    /// <returns></returns>
    private E_NODESTATUS ProvidingQuadTree(int[,] points, int startX, int startY, int size, QuadTreeNode node, int level)
    {
        node.level = level;
        //node.quadtree = this;
        //node.myPosition = pos;

        bool isCombined = true; // 병합 가능한가?
        int startStatus = points[startX, startY]; // 제일 처음 시작하는 곳을 기준으로 잡음
        for(int i = startX; i < startX + size; i++)
        {
            for(int j = startY; j < startY + size; j++)
            {
                // 제일 처음으로 시작하는 곳을 기준으로 잡고,
                // 검사 부위만큼 쭉 돌았을때 전부 같은지 체크.
                if(points[i, j] != startStatus)
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
            return startStatus == 1 ? E_NODESTATUS.COMBINED_1 : E_NODESTATUS.COMBINED_0;
        }

        // 아닐경우 4분할
        int halfSize = (int)(size * 0.5f);
        sb.Append("{");
        for(int i = 0; i < 4; i++)
        {
            node.childs[i] = new QuadTreeNode();
            node.childs[i].parent = node;
        }
        node.childs[0].status = ProvidingQuadTree(points, startX, startY, halfSize, node.childs[0], level + 1);
        node.childs[1].status = ProvidingQuadTree(points, startX + halfSize, startY, halfSize, node.childs[1], level + 1);
        node.childs[2].status = ProvidingQuadTree(points, startX, startY + halfSize, halfSize, node.childs[2], level + 1);
        node.childs[3].status = ProvidingQuadTree(points, startX + halfSize, startY + halfSize, halfSize, node.childs[3], level + 1);
        sb.Append("}");

        return E_NODESTATUS.NOT_COMBINED;
    }
}
