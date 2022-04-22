using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public enum E_NODESTATUS
{
    NOT_COMBINED = 0, // 정해지지 않은 노드 ( 자식노드 접근 가능 )
    COMBINED_DEAD = 1, // 정해진 노드_0
    COMBINED_ALIVE = 2, // 정해진 노드_1
}

public class QuadTreeNode
{
    public E_NODESTATUS status = E_NODESTATUS.NOT_COMBINED;

    public int level = 0;
    public Vector2 startPos;

    public QuadTreeNode parent;
    public QuadTreeNode[] childs = new QuadTreeNode[4];
    public List<GameObject> objList = new List<GameObject>();
}

public class QuadTreeManager : MonoBehaviour
{
    [SerializeField] GameObject player;
    QuadTreeNode rootNode = new QuadTreeNode();

    int mapSize;

    /// <summary>
    /// 쿼드 트리를 이용해서 정보를 다듬고 구분해주는 함수
    /// </summary>
    /// <param name="points">0과 1로 이루어진 N*N형식의 데이터</param>
    /// <param name="trans_stones">1차원 형식으로 이루어져있는 GameObject들의 부모</param>
    /// <param name="startX">검색 시작 X</param>
    /// <param name="startY">검색 시작 Y</param>
    /// <param name="size">가로/세로길이 </param>
    public void StartQuadTree(int[,] points, Transform trans_stones, int startX, int startY, int size)
    {
        mapSize = size;
        rootNode.status = ProvidingQuadTree(points, trans_stones, startX, startY, size, rootNode, 0);
    }

    /// <summary>
    /// 쿼드 트리를 이용하여 노드를 나누어주는 함수
    /// </summary>
    /// <param name="points">int로 구성된 타일정보</param>
    /// <param name="trans_stones">1차원 형식으로 이루어져있는 GameObject들의 부모</param>
    /// <param name="startX">시작지점 X</param>
    /// <param name="startY">시작지점 Y</param>
    /// <param name="size">N*N 검색 범위 사이즈</param>
    /// <param name="node">QuadTree 데이터를 저장할 노드</param>
    /// <param name="level">몇단계까지 진행했나</param>
    /// <returns></returns>
    private E_NODESTATUS ProvidingQuadTree(int[,] points, Transform trans_stones, int startX, int startY, int size, QuadTreeNode node, int level)
    {
        node.level = level;
        node.startPos.x = startX;
        node.startPos.y = startY;

        bool isCombined = true; // 병합 가능한가?
        int startStatus = points[startY, startX]; // 제일 처음 시작하는 곳을 기준으로 잡음
        for(int y = startY; y < startY + size; y++)
        {
            for(int x = startX; x < startX + size; x++)
            {
                // 제일 처음으로 시작하는 곳을 기준으로 잡고,
                // 검사 부위만큼 쭉 돌았을때 전부 같은지 체크.
                if(points[y, x] != startStatus)
                {
                    // 다른 불순물이 섞여있으면 병합 불가능
                    isCombined = false;
                    break;
                }
            }
            if (isCombined == false)
                break;
        }

        if (isCombined) // 병합 가능할 경우
        {
            for (int y = startY; y < startY + size; y++)
            {
                for (int x = startX; x < startX + size; x++)
                {
                    if (points[y, x] == (int)E_TILEALIVE.DEAD)
                        node.objList.Add(trans_stones.GetChild((y * mapSize) + x).gameObject);
                }
            }
            return startStatus == (int)E_TILEALIVE.ALIVE ? E_NODESTATUS.COMBINED_ALIVE : E_NODESTATUS.COMBINED_DEAD;
        }

        // 아닐경우 4분할
        int halfSize = (int)(size * 0.5f);
        for(int i = 0; i < 4; i++)
        {
            node.childs[i] = new QuadTreeNode();
            node.childs[i].objList = new List<GameObject>();
            node.childs[i].parent = node;
        }
        node.childs[0].status = ProvidingQuadTree(points, trans_stones, startX, startY, halfSize, node.childs[0], level + 1);
        node.childs[1].status = ProvidingQuadTree(points, trans_stones, startX + halfSize, startY, halfSize, node.childs[1], level + 1);
        node.childs[2].status = ProvidingQuadTree(points, trans_stones, startX, startY + halfSize, halfSize, node.childs[2], level + 1);
        node.childs[3].status = ProvidingQuadTree(points, trans_stones, startX + halfSize, startY + halfSize, halfSize, node.childs[3], level + 1);

        return E_NODESTATUS.NOT_COMBINED;
    }

    public void GetTile()
    {
        QuadTreeNode node = new QuadTreeNode();
        SearchNodeFromWorldPosition(player.transform.position, rootNode, ref node);

        for(int i = 0; i < node.objList.Count; i++)
        {
            node.objList[i].GetComponent<SpriteRenderer>().color = Color.green;
        }
        Debug.Log("Search Done!");
    }

    public void SearchNodeFromWorldPosition(Vector2 target, QuadTreeNode node, ref QuadTreeNode finalNode)
    {
        // 자식이 존재할 수 없을 때, ( 더이상 나눌 수 없을 때 ) 정해지는 COMBINED_DEAD의 status 성질을 이용하여 제작되었습니다.
        switch (node.status)
        {
            // ▼ 4갈래로 갈라진 노드면 플레이어 위치 계산
            case E_NODESTATUS.NOT_COMBINED: 
                for(int i = 0; i < node.childs.Length; i++)
                {
                    // 플레이어의 위치가 있는 분할 노드에 접근
                    if(node.childs[i].startPos.x - 0.5f <= target.x && target.x <= node.childs[i].startPos.x + GetSizebyLevel(node.level) + 0.5f &&
                       node.childs[i].startPos.y - 0.5f <= target.y && target.y <= node.childs[i].startPos.y + GetSizebyLevel(node.level) + 0.5f)
                    {
                        SearchNodeFromWorldPosition(target, node.childs[i], ref finalNode);
                    }
                }
                break;  
            
            // ▼ 벽으로 막혀있는 곳이면 무조건 제외
            case E_NODESTATUS.COMBINED_ALIVE: return;

            // 만약 노드의 최소 단위까지 내려갔으면
            case E_NODESTATUS.COMBINED_DEAD:
                finalNode = node; 
                break; // 플레이어가 있는 최소 노드임을 인정하고 FINAL_NODE 에 넣어줌
        }
    }

    public int GetSizebyLevel(int level)
    {
        return mapSize / ((level + 1) * 2);
    }
}
