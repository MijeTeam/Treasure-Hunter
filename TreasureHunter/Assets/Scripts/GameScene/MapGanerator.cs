using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_TILETYPE
{
    FLOOR = 0,
    WALL = 1
}

public class MapGanerator : MonoBehaviour
{
    [SerializeField] QuadTreeManager quadtree;
    [SerializeField] Transform trans_obj;

    public int width; // 가로길이
    public int height; // 세로길이
    public int smoothCycles; // 다듬어주는 횟수

    public int[,] mapPoints; // cavePoints가 1이면 살아있는 객체, 0이면 죽어있는 객체

    [Range(0, 100)]
    public int randFillPercent; // 벽 생성률
    [Range(0, 8)]
    public int threshold; // 주변에 몇개의 이웃 생물이 있어야 살아남을 것인가

    public GameObject stone;

    // FillIsland전용 변수들
    bool[,] mapEntered; // Fill 전용
    // 각 Island들 저장용
    List<List<Vector2>> islands;

    private void Awake()
    {
        GenerateMap();
        FillIsland();
    }

    void Start()
    {
        PlaceGrid();
    }

    private void GenerateMap()
    {
        mapPoints = new int[width, height]; // 가로 세로 길이만큼 CavePoint 생성

        int seed = Random.Range(0, 1000000); // 랜덤 시드 생성 ( 완전 랜덤 )
        System.Random randChoice = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) // 반복중, 맵의 각 가장자리에서..
                {
                    // 각 모서리의 끝 변을 살아있는 생물로 잡음
                    mapPoints[x, y] = 1; // cavePoints를 1로 잡음
                }
                else if (randChoice.Next(0, 100) < randFillPercent) // 또한..
                {
                    // 퍼센테이지에 따라 살아있는 생물을 더 만듬
                    mapPoints[x, y] = 1;
                }
                else
                {
                    // 그게 아니라면.. 아무런 생물도 없음
                    mapPoints[x, y] = 0;
                }
            }
        }

        for (int i = 0; i < smoothCycles; i++) // 알고리즘 반복 횟수
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighboringWalls = GetNeighbors(x, y); // 이웃의 벽 개수를 가져옴

                    // 벽 개수보다 많으면
                    if (neighboringWalls > threshold)
                    {
                        // 난 살아있다!
                        mapPoints[x, y] = (int)E_TILETYPE.WALL;
                    }
                    else if (neighboringWalls < threshold)
                    {
                        // 이웃의 벽이 목표치보다 적으면 죽음
                        mapPoints[x, y] = (int)E_TILETYPE.FLOOR;
                    }
                }
            }
        }
    }

    private int GetNeighbors(int pointX, int pointY)
    {
        int wallNeighbors = 0;

        for (int x = pointX - 1; x <= pointX + 1; x++)
        {
            for (int y = pointY - 1; y <= pointY + 1; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (x != pointX || y != pointY)
                    {
                        if (mapPoints[x, y] == (int)E_TILETYPE.WALL)
                        {
                            wallNeighbors++;
                        }
                    }
                }
                else
                {
                    wallNeighbors++;
                }
            }
        }
        return wallNeighbors;
    }

    private void PlaceGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject kObj = Instantiate(stone, new Vector2(x, y), Quaternion.identity, trans_obj);
                kObj.name = $"{x},{y}";
                kObj.GetComponent<SpriteRenderer>().color = mapPoints[x, y] == (int)E_TILETYPE.WALL ? Color.white : Color.black;
            }
        }
        quadtree.StartQuadTree(mapPoints, trans_obj, 0, 0, width);
    }

    /// <summary>
    /// 벽이 아닌 곳에 좌표를 하나 찍어서 맵을 한번 쫙 돈다.
    /// 돌면서 n번째 Island : {좌표들} 형식으로 저장한다.
    /// 다 돌았다면, 가장 큰 Island를 제외한 나머지 Island들은 모두 벽으로 만들어버린다.
    /// </summary>
    private void FillIsland()
    {
        mapEntered = new bool[width, height];
        islands = new List<List<Vector2>>();
        int index = 0;
        int maxIslandSize = 0;
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                // 진입하지 않았으면서 동시에 FLOOR이라면?
                if (mapEntered[i, j] == false && mapPoints[i, j] == (int)E_TILETYPE.FLOOR)
                {
                    // FloodFill 시작
                    islands.Add(new List<Vector2>());
                    FloodFill(index, i, j);
                    if (maxIslandSize < islands[index].Count)
                        maxIslandSize = islands[index].Count;
                    ++index;
                }
            }
        }

        // 다 채웠다면? Island 크기 비교
        for (int i = 0; i < islands.Count; ++i)
        {
            if (maxIslandSize > islands[i].Count)
            {
                for (int j = 0; j < islands[i].Count; ++j)
                {
                    mapPoints[(int)islands[i][j].x, (int)islands[i][j].y] = (int)E_TILETYPE.WALL;
                }
            }
        }
    }

    private void FloodFill(int index, int x, int y)
    {
        if (x >= width)
            return;
        if (y >= height)
            return;
        if (x < 0)
            return;
        if (y < 0)
            return;

        if (mapEntered[x, y] == true)
            return;

        if (mapPoints[x, y] == (int)E_TILETYPE.WALL)
            return;

        islands[index].Add(new Vector2(x, y));
        mapEntered[x, y] = true;

        FloodFill(index, x + 1, y);
        FloodFill(index, x - 1, y);
        FloodFill(index, x, y + 1);
        FloodFill(index, x, y - 1);
    }

    ///
    public void Update()
    {

    }
    ///
}
