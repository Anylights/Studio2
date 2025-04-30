using UnityEngine;
using Uduino;
using System.Collections;
using System;

public class RgbController : MonoBehaviour
{
    public static RgbController Instance { get; private set; }

    [Header("调试选项")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("灯带对话选项设置")]
    [SerializeField] private Color optionStrip1Color = Color.red;   // 显示对话选项时灯带1的颜色
    [SerializeField] private Color optionStrip2Color = Color.green; // 显示对话选项时灯带2的颜色
    [SerializeField] private Color defaultColor = Color.white;      // 默认灯带颜色

    [Header("脉冲效果设置")]
    [SerializeField] private float pulseEffectDuration = 0.5f; // 脉冲效果持续时间，默认0.5秒
    [SerializeField] private int pulseTailLength = 5; // 脉冲效果的尾部长度

    // 当前灯带的颜色，用于在脉冲效果结束后恢复
    private Color strip1CurrentColor = Color.white;
    private Color strip2CurrentColor = Color.white;

    // 检查Uduino管理器是否可用
    private bool IsUduinoAvailable => UduinoManager.Instance != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        // 延迟一秒再设置颜色，确保Uduino已初始化
        StartCoroutine(DelayedStart());

        // 查找并订阅MinimalOptionsView事件
        MinimalOptionsView optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null)
        {
            optionsView.OnOptionsShown += OnDialogueOptionsShown;
            optionsView.OnOptionsHidden += OnDialogueOptionsHidden;

            if (enableDebugLogs)
                Debug.Log("已成功订阅对话选项事件");
        }
        else
        {
            Debug.LogWarning("未找到MinimalOptionsView，灯带将无法对对话选项作出响应");
        }

        EventCenter.Instance.Subscribe<int>("optionSelected", HandlePulseEffect);
    }

    private void OnDestroy()
    {
        // 取消事件订阅，防止内存泄漏
        MinimalOptionsView optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null)
        {
            optionsView.OnOptionsShown -= OnDialogueOptionsShown;
            optionsView.OnOptionsHidden -= OnDialogueOptionsHidden;
        }
        EventCenter.Instance.Unsubscribe<int>("optionSelected", HandlePulseEffect);

        // 关闭灯带
        TurnOffLights();
    }

    // 当显示对话选项时调用
    private void OnDialogueOptionsShown()
    {
        if (enableDebugLogs)
            Debug.Log("检测到对话选项显示，设置灯带颜色");

        SetStripColor(0, optionStrip1Color); // 灯带1设为红色
        SetStripColor(1, optionStrip2Color); // 灯带2设为绿色
    }

    // 当隐藏对话选项时调用
    private void OnDialogueOptionsHidden()
    {

    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);

        // 确认Uduino可用
        if (!IsUduinoAvailable)
        {
            Debug.LogError("Uduino管理器未找到，无法控制灯带");
            yield break;
        }

        SetDefaultColor();
    }

    // 设置两条灯带为默认颜色
    public void SetDefaultColor()
    {
        SetColor(defaultColor);
    }

    // 关闭所有灯带
    public void TurnOffLights()
    {
        if (!IsUduinoAvailable) return;

        try
        {
            SetColor(Color.black);

            if (enableDebugLogs)
                Debug.Log("灯带已关闭");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"关闭灯带时出错: {e.Message}");
        }
    }

    // 提供公共方法以供其他脚本调用
    public void SetColor(Color color)
    {
        if (!IsUduinoAvailable) return;

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            // 确保命令名称与Arduino中注册的完全一致
            UduinoManager.Instance.sendCommand("SetColor", r.ToString(), g.ToString(), b.ToString());

            if (enableDebugLogs)
                Debug.Log($"灯带颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置灯带颜色时出错: {e.Message}");
        }
    }

    // 设置特定灯带的颜色
    public void SetStripColor(int stripIndex, Color color)
    {
        if (!IsUduinoAvailable) return;

        // Arduino代码中灯带索引从1开始
        int arduinoStripIndex = stripIndex + 1;

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            UduinoManager.Instance.sendCommand("SetStripColor", arduinoStripIndex.ToString(), r.ToString(), g.ToString(), b.ToString());

            // 保存当前颜色以便后续恢复
            if (stripIndex == 0)
                strip1CurrentColor = color;
            else if (stripIndex == 1)
                strip2CurrentColor = color;

            if (enableDebugLogs)
                Debug.Log($"灯带{stripIndex}颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置灯带颜色时出错: {e.Message}");
        }
    }

    // 设置指定灯带指定灯珠的颜色
    public void SetPixelColor(int stripIndex, int pixelIndex, Color color)
    {
        if (!IsUduinoAvailable) return;

        // Arduino代码中灯带索引从1开始
        int arduinoStripIndex = stripIndex + 1;

        // 确保像素索引在有效范围内
        if (pixelIndex < 0 || pixelIndex >= 52)
        {
            Debug.LogError($"无效的像素索引: {pixelIndex}，应为0-51之间");
            return;
        }

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            // 确保命令名称与Arduino中注册的完全一致
            UduinoManager.Instance.sendCommand("SetPixelColor",
                arduinoStripIndex.ToString(),
                pixelIndex.ToString(),
                r.ToString(),
                g.ToString(),
                b.ToString());

            if (enableDebugLogs)
                Debug.Log($"灯带{stripIndex}的第{pixelIndex}个像素颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置像素颜色时出错: {e.Message}");
        }
    }

    void HandlePulseEffect(int optionIndex)
    {
        int stripIndex = optionIndex;
        StartCoroutine(PulseEffectWithTailCoroutine(stripIndex));
    }

    // // 脉冲效果协程
    // private IEnumerator PulseEffectCoroutine(int stripIndex)
    // {
    //     if (!IsUduinoAvailable) yield break;

    //     Color targetColor = stripIndex == 0 ? optionStrip1Color : optionStrip2Color;

    //     // 获取灯带LED总数量
    //     int ledCount = 52;

    //     // 先关闭灯带
    //     SetStripColor(stripIndex, Color.black);

    //     yield return new WaitForSeconds(0.1f);

    //     // 计算时间步长
    //     float stepTime = pulseEffectDuration / ledCount;

    //     // 执行脉冲效果 - 一个接一个亮起
    //     for (int i = 0; i < ledCount; i++)
    //     {
    //         // 只设置当前位置的LED，不重置整个灯带
    //         SetPixelColor(stripIndex, i, targetColor);

    //         yield return new WaitForSeconds(stepTime);
    //     }

    //     // 短暂显示所有灯亮起的状态
    //     yield return new WaitForSeconds(0.3f);

    //     // 明确地重置整个灯带 - 这是关键
    //     SetStripColor(stripIndex, defaultColor);

    //     if (enableDebugLogs)
    //         Debug.Log($"脉冲效果结束，灯带{stripIndex}已恢复为默认颜色");
    // }

    // 另一种实现 - 带"尾巴"的流动效果
    private IEnumerator PulseEffectWithTailCoroutine(int stripIndex)
    {
        if (!IsUduinoAvailable) yield break;

        Color targetColor = stripIndex == 0 ? optionStrip1Color : optionStrip2Color;
        int ledCount = 52;

        // 重置灯带
        SetStripColor(stripIndex, Color.black);

        yield return new WaitForSeconds(0.1f);

        float stepTime = pulseEffectDuration / ledCount;

        // 实现带"尾巴"的效果
        for (int i = 0; i < ledCount + pulseTailLength; i++)
        {
            // 先清除所有LED
            SetStripColor(stripIndex, Color.black);

            // 设置"尾巴"中的每个LED，带有亮度渐变
            for (int j = 0; j < pulseTailLength; j++)
            {
                int pixelIndex = i - j;

                // 确保像素索引在有效范围内
                if (pixelIndex >= 0 && pixelIndex < ledCount)
                {
                    // 根据位置计算亮度
                    float intensity = 1.0f - ((float)j / pulseTailLength);

                    // 创建渐变颜色
                    Color fadeColor = new Color(
                        targetColor.r * intensity,
                        targetColor.g * intensity,
                        targetColor.b * intensity
                    );

                    SetPixelColor(stripIndex, pixelIndex, fadeColor);
                }
            }

            yield return new WaitForSeconds(stepTime);
        }

        // 在PulseEffectCoroutine结尾修改为：
        // 短暂显示所有灯亮起的状态
        yield return new WaitForSeconds(0.3f);

        // 先明确关闭所有灯带
        SetStripColor(0, Color.black);
        SetStripColor(1, Color.black);
        yield return new WaitForSeconds(0.1f);  // 给Arduino一些处理时间

        // 再明确地设置默认颜色
        SetDefaultColor();
    }
}