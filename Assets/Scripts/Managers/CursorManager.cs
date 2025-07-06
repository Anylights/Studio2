using UnityEngine;

/// <summary>
/// 鼠标管理器
/// 负责控制鼠标的显示、隐藏和锁定状态
/// </summary>
public class CursorManager : MonoBehaviour
{
    #region 单例实现
    private static CursorManager _instance;
    public static CursorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CursorManager>();
                if (_instance == null)
                {
                    GameObject cursorManagerObj = new GameObject("CursorManager");
                    _instance = cursorManagerObj.AddComponent<CursorManager>();
                    DontDestroyOnLoad(cursorManagerObj);
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("鼠标设置")]
    [SerializeField] private bool hideCursorOnStart = true;
    [SerializeField] private bool lockCursorOnStart = false;
    [SerializeField] private bool enableDebugLog = true;

    private bool isCursorVisible = true;
    private CursorLockMode initialLockState = CursorLockMode.None;

    private void Awake()
    {
        // 确保只有一个实例
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 保存初始状态
        isCursorVisible = Cursor.visible;
        initialLockState = Cursor.lockState;
    }

    private void Start()
    {
        // 如果设置为在开始时隐藏鼠标
        if (hideCursorOnStart)
        {
            HideCursor();
        }

        // 如果设置为在开始时锁定鼠标
        if (lockCursorOnStart)
        {
            LockCursor();
        }

        if (enableDebugLog)
        {
            Debug.Log($"CursorManager初始化完成 - 隐藏鼠标: {hideCursorOnStart}, 锁定鼠标: {lockCursorOnStart}");
        }
    }

    /// <summary>
    /// 隐藏鼠标
    /// </summary>
    public void HideCursor()
    {
        Cursor.visible = false;
        isCursorVisible = false;

        if (enableDebugLog) Debug.Log("鼠标已隐藏");
    }

    /// <summary>
    /// 显示鼠标
    /// </summary>
    public void ShowCursor()
    {
        Cursor.visible = true;
        isCursorVisible = true;

        if (enableDebugLog) Debug.Log("鼠标已显示");
    }

    /// <summary>
    /// 切换鼠标显示状态
    /// </summary>
    public void ToggleCursor()
    {
        if (isCursorVisible)
        {
            HideCursor();
        }
        else
        {
            ShowCursor();
        }
    }

    /// <summary>
    /// 锁定鼠标到屏幕中心
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (enableDebugLog) Debug.Log("鼠标已锁定");
    }

    /// <summary>
    /// 解锁鼠标
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;

        if (enableDebugLog) Debug.Log("鼠标已解锁");
    }

    /// <summary>
    /// 将鼠标限制在游戏窗口内
    /// </summary>
    public void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;

        if (enableDebugLog) Debug.Log("鼠标已限制在窗口内");
    }

    /// <summary>
    /// 重置鼠标到初始状态
    /// </summary>
    public void ResetCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        isCursorVisible = true;

        if (enableDebugLog) Debug.Log("鼠标状态已重置");
    }

    /// <summary>
    /// 获取鼠标是否可见
    /// </summary>
    public bool IsCursorVisible()
    {
        return Cursor.visible;
    }

    /// <summary>
    /// 获取鼠标锁定状态
    /// </summary>
    public CursorLockMode GetCursorLockState()
    {
        return Cursor.lockState;
    }

    private void Update()
    {
        // 可以添加快捷键控制（可选）
        // 例如：按下Alt键时临时显示鼠标
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            ShowCursor();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            if (hideCursorOnStart)
            {
                HideCursor();
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 当应用程序失去焦点时显示鼠标，获得焦点时恢复设置
        if (!hasFocus)
        {
            ShowCursor();
            UnlockCursor();
        }
        else if (hasFocus && hideCursorOnStart)
        {
            HideCursor();
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 当应用程序暂停时显示鼠标
        if (pauseStatus)
        {
            ShowCursor();
            UnlockCursor();
        }
        else if (!pauseStatus && hideCursorOnStart)
        {
            HideCursor();
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }
    }
}