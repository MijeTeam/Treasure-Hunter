using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResolution : MonoBehaviour
{
    public int width;
    public int height;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();
        // 카메라 컴포넌트의 Viewport Rect
        Rect rt = cam.rect;

        float scale_height = ((float)Screen.width / Screen.height) / ((float)width / height); // (가로 / 세로)
        float scale_width = 1f / scale_height;

        if (scale_height < 1)
        {
            rt.height = scale_height;
            rt.y = (1f - scale_height) / 2f;
        }
        else
        {
            rt.width = scale_width;
            rt.x = (1f - scale_width) / 2f;
        }
        cam.rect = rt;
    }

    void OnPreCull() => GL.Clear(true, true, Color.black);
}
