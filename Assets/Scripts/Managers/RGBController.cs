using UnityEngine;
using Uduino;
using System.Collections;
using System;
using Yarn.Unity;

public class RgbController : MonoBehaviour
{
    public static RgbController Instance { get; private set; }

    [Header("调试选项")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("灯带对话选项设置")]
    [SerializeField] private Color optionStrip1Color = Color.red;   // 显示对话选项时灯带1的颜色
    [SerializeField] private Color optionStrip2Color = Color.green; // 显示对话选项时灯带2的颜色
    [SerializeField] private Color defaultColor = Color.white;      // 默认灯带颜色

    // 当前灯带的颜色，用于在脉冲效果结束后恢复
    private Color strip1CurrentColor = Color.white;
    private Color strip2CurrentColor = Color.white;

    // 检查Uduino管理器是否可用
    private bool IsUduinoAvailable => UduinoManager.Instance != null;

    [Header("按钮映射")]
    [SerializeField] private int redButtonOptionIndex = 0;   // 红按钮默认选择选项0
    [SerializeField] private int greenButtonOptionIndex = 1; // 绿按钮默认选择选项1

    // 当前脉冲效果类型
    private string currentPulseEffectType = "default";

    // 获取当前按钮映射
    public int GetRedButtonOptionIndex() => redButtonOptionIndex;
    public int GetGreenButtonOptionIndex() => greenButtonOptionIndex;

    // Yarn命令：设置选项灯带颜色
    [YarnCommand("set_option_colors")]
    public void SetOptionColors(string strip1ColorHex, string strip2ColorHex)
    {
        if (string.IsNullOrEmpty(strip1ColorHex) || string.IsNullOrEmpty(strip2ColorHex))
            return;

        // 解析十六进制颜色
        if (ColorUtility.TryParseHtmlString(strip1ColorHex, out Color color1))
            optionStrip1Color = color1;

        if (ColorUtility.TryParseHtmlString(strip2ColorHex, out Color color2))
            optionStrip2Color = color2;

        if (enableDebugLogs)
            Debug.Log($"已设置选项灯带颜色：strip1={strip1ColorHex}, strip2={strip2ColorHex}");
    }

    // Yarn命令：设置按钮映射
    [YarnCommand("set_button_mapping")]
    public void SetButtonMapping(int redButtonOption, int greenButtonOption)
    {
        redButtonOptionIndex = redButtonOption;
        greenButtonOptionIndex = greenButtonOption;

        if (enableDebugLogs)
            Debug.Log($"已设置按钮映射：红按钮={redButtonOption}, 绿按钮={greenButtonOption}");
    }

    // Yarn命令：设置脉冲效果类型
    [YarnCommand("set_pulse_effect")]
    public void SetPulseEffectType(string effectType)
    {
        if (string.IsNullOrEmpty(effectType))
            effectType = "default";

        currentPulseEffectType = effectType;

        if (enableDebugLogs)
            Debug.Log($"已设置脉冲效果类型：{effectType}");
    }

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

        EventCenter.Instance.Subscribe<int>("buttonPressed", HandlePulseEffect);
        // EventCenter.Instance.Subscribe<int>("ContinueDialogue", HandleDefaultPulseEffect);
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
        EventCenter.Instance.Unsubscribe<int>("buttonPressed", HandlePulseEffect);
        // EventCenter.Instance.Unsubscribe<int>("ContinueDialogue", HandleDefaultPulseEffect);

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

    // 修改HandlePulseEffect方法，现在接收的是按钮索引而不是选项索引
    void HandlePulseEffect(int buttonIndex)
    {
        if (!IsUduinoAvailable) return;

        try
        {
            // 根据按钮索引获取对应的颜色
            Color pulseColor = (buttonIndex == 0) ? optionStrip1Color : optionStrip2Color;

            // 转换颜色为RGB值
            int r = Mathf.RoundToInt(pulseColor.r * 255);
            int g = Mathf.RoundToInt(pulseColor.g * 255);
            int b = Mathf.RoundToInt(pulseColor.b * 255);

            // 现在buttonIndex直接对应灯带索引：0=红按钮/0号灯带，1=绿按钮/1号灯带
            // 发送命令：灯带索引、效果类型、RGB颜色值
            UduinoManager.Instance.sendCommand("PulseEffect",
                buttonIndex.ToString(),
                currentPulseEffectType,
                r.ToString(),
                g.ToString(),
                b.ToString());

            if (enableDebugLogs)
                Debug.Log($"已发送脉冲效果命令，按钮/灯带索引: {buttonIndex}, 效果类型: {currentPulseEffectType}, 颜色: R={r},G={g},B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"发送脉冲效果命令失败: {e.Message}");
        }
    }

    // void HandleDefaultPulseEffect(int stripIndex)
    // {
    //     if (!IsUduinoAvailable) return;

    //     try
    //     {
    //         UduinoManager.Instance.sendCommand("DefaultPulseEffect", stripIndex.ToString());

    //         if (enableDebugLogs)
    //             Debug.Log($"已发送默认脉冲效果命令，灯带索引: {stripIndex}");
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError($"发送默认脉冲效果命令失败: {e.Message}");
    //     }
    // }
}