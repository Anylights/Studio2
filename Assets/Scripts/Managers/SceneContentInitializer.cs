using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 场景内容初始化器
/// 用于在场景内容激活时执行自定义逻辑
/// </summary>
public class SceneContentInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool runOnAwake = false;
    [SerializeField] private bool logInitialization = true;

    private void Awake()
    {
        if (runOnAwake)
        {
            InitializeContent();
        }
    }

    /// <summary>
    /// 当场景内容被激活时由SceneContentManager调用
    /// </summary>
    public void OnSceneActivated()
    {
        InitializeContent();
    }

    /// <summary>
    /// 初始化场景内容
    /// </summary>
    public void InitializeContent()
    {
        if (logInitialization)
        {
            Debug.Log($"初始化场景内容: {gameObject.name}");
        }

        // 执行自定义初始化逻辑
        OnInitialize();
    }

    /// <summary>
    /// 子类重写此方法以添加自定义初始化逻辑
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 子类中实现
    }
}