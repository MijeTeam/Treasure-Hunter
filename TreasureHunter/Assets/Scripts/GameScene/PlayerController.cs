using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform trans_tiles; // 실제 하이어라키에 등록되어있는 타일들의 부모
    [SerializeField] Transform trans_Nevigate;
    //[SerializeField] QuadTreeManager qtMNG;

    [Header("[ Movement Options ]")]
    public float maxSpeed; // 최대 스피드
    public float fowardAngle; // 플레이어가 바라보는 방향의 각도
    private Vector3 firstPos; // 마우스로 찍은 최초 위치
    private SpriteRenderer spr_body;
    public bool isPause; // 정지 상태인가 ( 정지 상태에서는 못 움직임 )

    public int MapLength = 80; // 맵의 크기
    private Vector2 playerPosition;

    [Header("[ Coordinate Data ]")]
    public int playerX;
    public int playerY;

    private void Awake()
    {
        spr_body = GetComponent<SpriteRenderer>();

        Initialize();
    }

    public void Initialize()
    {
        playerPosition = Vector2.zero;
        playerX = 0;
        playerY = 0;
        fowardAngle = 0;
        // Make Something ...
    }

    public void PlayerClipping()
    {
        if (playerPosition.x > MapLength - 1) playerPosition.x = MapLength - 1;
        if (playerPosition.x < 0) playerPosition.x = 0;
        if (playerPosition.y > MapLength - 1) playerPosition.y = MapLength - 1;
        if (playerPosition.y < 0) playerPosition.y = 0;
    }

    public void Update()
    {
        if (isPause) return;

        playerX = (int)playerPosition.x;
        playerY = (int)playerPosition.y;

        transform.position = Vector2.Lerp(transform.position, trans_tiles.GetChild((playerX * MapLength) + playerY).position, Time.deltaTime * 5);
        trans_Nevigate.rotation = Quaternion.Lerp(trans_Nevigate.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * fowardAngle), Time.deltaTime * 8);
        
        if (Input.GetMouseButtonDown(0))
        {
            firstPos = Input.mousePosition;
        } 
        if (Input.GetMouseButton(0))
        {
            Vector2 pos = Input.mousePosition - firstPos;
            Vector2 dir = pos.normalized;
            fowardAngle = Mathf.Atan2(pos.y, pos.x);
            playerPosition += dir * Mathf.Clamp(pos.magnitude / 45, 0, maxSpeed) * Time.deltaTime;

            PlayerClipping();
        }
    }
}
