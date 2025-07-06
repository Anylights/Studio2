using UnityEngine;

/// <summary>
/// 游戏开始时隐藏鼠标的简单脚本
/// 将此脚本添加到场景中的任何GameObject上即可生效
/// </summary>
public class HideCursorOnStart : MonoBehaviour
{
    [Header("鼠标控制设置")]
    [SerializeField] private bool hideCursor = true;
    [SerializeField] private bool lockCursor = false;
    [SerializeField] private bool enableDebugLog = true;

    private void Start()
    {
        if (hideCursor)
        {
            Cursor.visible = false;
            if (enableDebugLog) Debug.Log("鼠标已隐藏");
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (enableDebugLog) Debug.Log("鼠标已锁定到屏幕中心");
        }
    }

    private void Update()
    {
        // 按下Alt键时临时显示鼠标
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            if (enableDebugLog) Debug.Log("临时显示鼠标");
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt) && hideCursor)
        {
            Cursor.visible = false;
            if (enableDebugLog) Debug.Log("恢复隐藏鼠标");
        }

        // 按下Escape键时显示鼠标并解锁
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (enableDebugLog) Debug.Log("鼠标已解锁并显示");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 当应用程序失去焦点时显示鼠标，获得焦点时恢复隐藏
        if (!hasFocus)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (hasFocus && hideCursor)
        {
            Cursor.visible = false;
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}