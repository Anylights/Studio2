using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LightPathEffect : MonoBehaviour
{
    public LineRenderer leftLine;
    public LineRenderer rightLine;
    public Image flashImage;
    public float moveDuration = 2f;
    public float flashDuration = 0.2f;

    private List<Vector3> leftPath;
    private List<Vector3> rightPath;

    void Start()
    {
        // 计算路径点（以正交相机为例，z=10是因为摄像机默认在z=-10）
        Camera cam = Camera.main;
        leftPath = new List<Vector3>
        {
            cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, 10f)), // 屏幕底部中央
            cam.ViewportToWorldPoint(new Vector3(0f, 0f, 10f)),   // 左下角
            cam.ViewportToWorldPoint(new Vector3(0f, 1f, 10f)),   // 左上角
            cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, 10f)), // 顶部中央
        };
        rightPath = new List<Vector3>
        {
            cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, 10f)), // 屏幕底部中央
            cam.ViewportToWorldPoint(new Vector3(1f, 0f, 10f)),   // 右下角
            cam.ViewportToWorldPoint(new Vector3(1f, 1f, 10f)),   // 右上角
            cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, 10f)), // 顶部中央
        };

        leftLine.positionCount = leftPath.Count;
        rightLine.positionCount = rightPath.Count;

        StartCoroutine(AnimateLines());
    }

    System.Collections.IEnumerator AnimateLines()
    {
        float t = 0;
        int pointCount = leftPath.Count;
        while (t < moveDuration)
        {
            float progress = t / moveDuration;
            float totalSegments = pointCount - 1;
            float current = progress * totalSegments;
            int finished = Mathf.FloorToInt(current); // 已经到位的点数
            float lerp = current - finished; // 当前段的插值进度

            // Left Line
            for (int i = 0; i < pointCount; i++)
            {
                if (i < finished)
                {
                    leftLine.SetPosition(i, leftPath[i]);
                }
                else if (i == finished && i > 0)
                {
                    // 当前段插值
                    leftLine.SetPosition(i, Vector3.Lerp(leftPath[i - 1], leftPath[i], lerp));
                }
                else
                {
                    // 还没到的点，保持在起点
                    leftLine.SetPosition(i, leftPath[0]);
                }
            }

            // Right Line
            for (int i = 0; i < pointCount; i++)
            {
                if (i < finished)
                {
                    rightLine.SetPosition(i, rightPath[i]);
                }
                else if (i == finished && i > 0)
                {
                    rightLine.SetPosition(i, Vector3.Lerp(rightPath[i - 1], rightPath[i], lerp));
                }
                else
                {
                    rightLine.SetPosition(i, rightPath[0]);
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        // 最终位置
        for (int i = 0; i < pointCount; i++)
        {
            leftLine.SetPosition(i, leftPath[i]);
            rightLine.SetPosition(i, rightPath[i]);
        }

        StartCoroutine(FlashScreen());
    }

    System.Collections.IEnumerator FlashScreen()
    {
        // 快速淡入
        float t = 0;
        while (t < flashDuration / 2)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(1, 1, 1, t / (flashDuration / 2));
            yield return null;
        }
        // 快速淡出
        t = 0;
        while (t < flashDuration / 2)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(1, 1, 1, 1 - t / (flashDuration / 2));
            yield return null;
        }
        flashImage.color = new Color(1, 1, 1, 0);
    }
}