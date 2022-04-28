using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    private Camera cam;

    [SerializeField] GameObject target;
    
    [Header("[ Lerp Based Move ]")]
    public float orthoSize;
    public float lerpAmount;

    [Header("[ Lerp + Mouse Based Move ]")]
    [Range(0,1)]
    public float dragingAmount;
    public bool isDraging;
    public Vector3 eyetracking_pos;

    public void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = 1;
    }

    private void Update()
    {
        eyetracking_pos = Vector3.zero;
        if(isDraging) eyetracking_pos = cam.ScreenToWorldPoint(Input.mousePosition) - target.transform.position;

        Vector2 lerp = Vector2.Lerp(transform.position, target.transform.position + (eyetracking_pos * dragingAmount), Time.deltaTime * lerpAmount);
        transform.position = new Vector3(lerp.x, lerp.y, -10);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthoSize, Time.deltaTime * lerpAmount);
    }
}
