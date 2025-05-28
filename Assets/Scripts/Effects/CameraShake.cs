using UnityEngine;
using System.Collections;
using Yarn.Unity;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("震动设置")]
    [SerializeField] private float defaultDuration = 0.3f;
    [SerializeField] private float defaultMagnitude = 0.1f;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Cinemachine设置")]
    [SerializeField] private bool useCinemachine = true;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("传统震动设置（备用）")]
    private Vector3 originalPosition;
    private bool isShaking = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 自动查找Cinemachine组件
        if (useCinemachine)
        {
            // 查找ImpulseSource
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
                if (impulseSource == null)
                {
                    // 如果没有找到，尝试添加一个
                    impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                    if (enableDebugLogs)
                        Debug.Log("已自动添加CinemachineImpulseSource组件");
                }
            }

            // 查找Virtual Camera
            if (virtualCamera == null)
            {
                virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
                if (virtualCamera == null && enableDebugLogs)
                {
                    Debug.LogWarning("未找到CinemachineVirtualCamera，将使用传统震动方式");
                    useCinemachine = false;
                }
                else if (virtualCamera != null && enableDebugLogs)
                {
                    Debug.Log($"找到Virtual Camera: {virtualCamera.name}");

                    // 检查Impulse Listener
                    var impulseListener = virtualCamera.GetComponent<CinemachineImpulseListener>();
                    var independentListener = virtualCamera.GetComponent<CinemachineIndependentImpulseListener>();

                    if (impulseListener != null)
                    {
                        Debug.Log($"找到ImpulseListener，Channel Filter: {impulseListener.m_ChannelMask}");
                    }
                    else if (independentListener != null)
                    {
                        Debug.Log($"找到IndependentImpulseListener，Channel Filter: {independentListener.m_ChannelMask}");
                    }
                    else
                    {
                        Debug.LogError("Virtual Camera上没有找到任何Impulse Listener组件！这是震动无效果的原因。");
                    }
                }
            }
        }

        // 记录摄像机的原始位置（用于传统震动）
        originalPosition = transform.localPosition;

        if (enableDebugLogs)
        {
            Debug.Log($"CameraShake初始化完成，使用模式：{(useCinemachine ? "Cinemachine" : "传统Transform")}");
            if (useCinemachine && impulseSource != null)
            {
                Debug.Log($"ImpulseSource Channel: {impulseSource.m_ImpulseDefinition.m_ImpulseChannel}");
            }
        }
    }

    /// <summary>
    /// 触发摄像机震动
    /// </summary>
    /// <param name="duration">震动持续时间</param>
    /// <param name="magnitude">震动强度</param>
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        if (magnitude < 0) magnitude = defaultMagnitude;

        if (useCinemachine && impulseSource != null)
        {
            // 使用Cinemachine Impulse震动
            ShakeWithCinemachine(duration, magnitude);
        }
        else
        {
            // 使用传统Transform震动
            ShakeWithTransform(duration, magnitude);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"摄像机震动开始：持续时间={duration}s, 强度={magnitude}, 模式={(useCinemachine ? "Cinemachine" : "Transform")}");
        }
    }

    /// <summary>
    /// 使用Cinemachine Impulse进行震动
    /// </summary>
    private void ShakeWithCinemachine(float duration, float magnitude)
    {
        if (impulseSource == null)
        {
            Debug.LogError("ImpulseSource为空，无法执行Cinemachine震动");
            return;
        }

        // 设置Impulse参数
        impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;

        // 生成随机方向的冲击
        Vector3 impulseDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f
        ).normalized;

        // 触发冲击，magnitude作为力的大小
        impulseSource.GenerateImpulse(impulseDirection * magnitude);

        if (enableDebugLogs)
        {
            Debug.Log($"Cinemachine冲击已触发: 方向={impulseDirection}, 强度={magnitude}, 持续时间={duration}");
            Debug.Log($"ImpulseSource配置: Channel={impulseSource.m_ImpulseDefinition.m_ImpulseChannel}, Duration={impulseSource.m_ImpulseDefinition.m_ImpulseDuration}");
        }
    }

    /// <summary>
    /// 使用传统Transform进行震动
    /// </summary>
    private void ShakeWithTransform(float duration, float magnitude)
    {
        if (isShaking)
        {
            // 如果已经在震动，停止当前震动
            StopAllCoroutines();
        }

        StartCoroutine(DoTransformShake(duration, magnitude));
    }

    /// <summary>
    /// Yarn命令：触发摄像机震动
    /// </summary>
    [YarnCommand("camera_shake")]
    public static void YarnShake(float duration = 0.3f, float magnitude = 0.1f)
    {
        if (Instance != null)
        {
            Instance.Shake(duration, magnitude);
        }
        else
        {
            Debug.LogWarning("CameraShake实例未找到，无法执行震动");
        }
    }

    /// <summary>
    /// Yarn命令：停止摄像机震动
    /// </summary>
    [YarnCommand("camera_shake_stop")]
    public static void YarnStopShake()
    {
        if (Instance != null)
        {
            Instance.StopShake();
        }
        else
        {
            Debug.LogWarning("CameraShake实例未找到，无法停止震动");
        }
    }

    /// <summary>
    /// 执行传统Transform震动的协程
    /// </summary>
    private IEnumerator DoTransformShake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 生成随机偏移
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // 应用震动偏移
            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 震动结束，恢复原始位置
        transform.localPosition = originalPosition;
        isShaking = false;

        if (enableDebugLogs)
        {
            Debug.Log("传统Transform震动结束");
        }
    }

    /// <summary>
    /// 停止震动
    /// </summary>
    public void StopShake()
    {
        if (useCinemachine && impulseSource != null)
        {
            // Cinemachine的震动会自然衰减，这里可以强制清除所有冲击
            CinemachineImpulseManager.Instance.Clear();
            if (enableDebugLogs)
            {
                Debug.Log("已清除所有Cinemachine冲击");
            }
        }
        else if (isShaking)
        {
            // 停止传统震动
            StopAllCoroutines();
            transform.localPosition = originalPosition;
            isShaking = false;

            if (enableDebugLogs)
            {
                Debug.Log("传统Transform震动被强制停止");
            }
        }
    }

    /// <summary>
    /// 检查是否正在震动
    /// </summary>
    public bool IsShaking()
    {
        if (useCinemachine)
        {
            // 对于Cinemachine，检查是否有活跃的冲击
            // 由于HasImpulses方法不存在，我们简单返回false或使用其他方式检测
            return false; // Cinemachine的冲击会自然衰减，这里简化处理
        }
        else
        {
            return isShaking;
        }
    }

    /// <summary>
    /// 切换震动模式
    /// </summary>
    public void SetUseCinemachine(bool useCine)
    {
        useCinemachine = useCine && impulseSource != null;
        if (enableDebugLogs)
        {
            Debug.Log($"震动模式切换为：{(useCinemachine ? "Cinemachine" : "传统Transform")}");
        }
    }

    /// <summary>
    /// 获取当前震动模式
    /// </summary>
    public bool IsUsingCinemachine()
    {
        return useCinemachine;
    }
}