using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] QuadTreeManager qtMNG;

    public float maxSpeed;
    private Vector3 firstPos;
    private SpriteRenderer spr_body;

    public float fowardAngle;

    private void Awake()
    {
        spr_body = GetComponent<SpriteRenderer>();

        Initialize();
    }

    public void Initialize()
    {
        // Make Something ...
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            firstPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 pos = Input.mousePosition - firstPos;
            Vector3 dir = pos.normalized;
            fowardAngle = Mathf.Atan2(pos.y, pos.x);
            transform.Translate(dir * Mathf.Clamp(pos.magnitude / 45, 0, maxSpeed) * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.X))
            qtMNG.GetTile();
    }
}
