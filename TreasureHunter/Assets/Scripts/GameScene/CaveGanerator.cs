using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_TILEALIVE
{
    DEAD = 0,
    ALIVE = 1
}

public class CaveGanerator : MonoBehaviour
{
    [SerializeField] QuadTreeManager quadtree;
    [SerializeField] Transform trans_obj;

    public int width; // 가로길이
    public int height; // 세로길이
    public int smoothCycles; // 다듬어주는 횟수

    public int[,] cavePoints; // cavePoints가 1이면 살아있는 객체, 0이면 죽어있는 객체

    [Range(0, 100)]
    public int randFillPercent; // 벽 생성률
    [Range(0, 8)]
    public int threshold; // 주변에 몇개의 이웃 생물이 있어야 살아남을 것인가

    public GameObject stone;

    private void Awake()
    {
        GenerateCave();
    }

    void Start()
    {
        PlaceGrid();
    }

    private void GenerateCave()
    {
        cavePoints = new int[width, height]; // 가로 세로 길이만큼 CavePoint 생성

        int seed = Random.Range(0, 1000000); // 랜덤 시드 생성 ( 완전 랜덤 )
        System.Random randChoice = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) // 반복중, 맵의 각 가장자리에서..
                {
                    // 각 모서리의 끝 변을 살아있는 생물로 잡음
                    cavePoints[x, y] = 1; // cavePoints를 1로 잡음
                }
                else if (randChoice.Next(0, 100) < randFillPercent) // 또한..
                {
                    // 퍼센테이지에 따라 살아있는 생물을 더 만듬
                    cavePoints[x, y] = 1;
                }
                else
                {
                    // 그게 아니라면.. 아무런 생물도 없음
                    cavePoints[x, y] = 0;
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
                        cavePoints[x, y] = (int)E_TILEALIVE.ALIVE;
                    }
                    else if (neighboringWalls < threshold)
                    {
                        // 이웃의 벽이 목표치보다 적으면 죽음
                        cavePoints[x, y] = (int)E_TILEALIVE.DEAD;
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
                        if (cavePoints[x, y] == (int)E_TILEALIVE.ALIVE)
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
                kObj.GetComponent<SpriteRenderer>().color = cavePoints[x, y] == (int)E_TILEALIVE.ALIVE ? Color.white : Color.black;
            }
        }
        quadtree.StartQuadTree(cavePoints, trans_obj, 0, 0, width);
    }


    ///
    public void Update()
    {

    }
    ///
}
