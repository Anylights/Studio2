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

    // 单独的灯带脉冲效果类型
    private string strip1PulseEffectType = "default"; // 0号灯带（红按钮）的脉冲效果
    private string strip2PulseEffectType = "default"; // 1号灯带（绿按钮）的脉冲效果

    // 渐变脉冲效果参数
    [System.Serializable]
    public class GradientParams
    {
        public Color startColor = Color.red;
        public Color endColor = Color.blue;
        public int duration = 200; // 毫秒
    }

    private GradientParams strip1GradientParams = new GradientParams();
    private GradientParams strip2GradientParams = new GradientParams();

    [Header("充能效果设置")]
    // 充能效果状态
    private bool[] stripChargingMode = new bool[2] { false, false }; // 每条灯带是否处于充能模式
    private float[] currentChargePosition = new float[2] { 0f, 0f }; // 当前充能位置（0-51）
    private float[] maxChargePosition = new float[2] { 0f, 0f }; // 最远充能位置
    private bool[] isCharging = new bool[2] { false, false }; // 是否正在充能中

    // 充能效果参数（可通过Yarn命令设置）
    private float chargeDistance = 8f; // 每次按钮推进的距离
    private float chargeSpeed = 20f; // 推出去的速度（灯珠/秒）
    private float decaySpeed = 3f; // 往回退的速度（灯珠/秒）
    private int targetButtonMapping = 0; // 充能完成后要设置的按钮映射
    private int chargingStripIndex = -1; // 当前正在充能的灯带索引

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
        // 同时设置两条灯带为相同效果（保持向后兼容）
        strip1PulseEffectType = effectType;
        strip2PulseEffectType = effectType;

        if (enableDebugLogs)
            Debug.Log($"已设置脉冲效果类型：{effectType}");
    }

    // Yarn命令：分别设置每条灯带的脉冲效果类型
    [YarnCommand("set_strip_pulse_effects")]
    public void SetStripPulseEffects(string strip1Effect, string strip2Effect)
    {
        if (string.IsNullOrEmpty(strip1Effect))
            strip1Effect = "default";
        if (string.IsNullOrEmpty(strip2Effect))
            strip2Effect = "default";

        strip1PulseEffectType = strip1Effect;
        strip2PulseEffectType = strip2Effect;

        if (enableDebugLogs)
            Debug.Log($"已设置灯带脉冲效果：strip1={strip1Effect}, strip2={strip2Effect}");
    }

    // Yarn命令：设置单条灯带的脉冲效果类型
    [YarnCommand("set_single_strip_pulse_effect")]
    public void SetSingleStripPulseEffect(int stripIndex, string effectType)
    {
        if (string.IsNullOrEmpty(effectType))
            effectType = "default";

        if (stripIndex == 0)
        {
            strip1PulseEffectType = effectType;
        }
        else if (stripIndex == 1)
        {
            strip2PulseEffectType = effectType;
        }

        if (enableDebugLogs)
            Debug.Log($"已设置灯带{stripIndex}的脉冲效果类型：{effectType}");
    }

    // Yarn命令：设置渐变脉冲效果参数
    [YarnCommand("set_gradient_pulse_params")]
    public void SetGradientPulseParams(int stripIndex, string startColorHex, string endColorHex, int duration = 200)
    {
        if (stripIndex < 0 || stripIndex > 1)
        {
            Debug.LogError($"无效的灯带索引: {stripIndex}");
            return;
        }

        GradientParams targetParams = (stripIndex == 0) ? strip1GradientParams : strip2GradientParams;

        // 解析颜色
        if (!string.IsNullOrEmpty(startColorHex) && ColorUtility.TryParseHtmlString(startColorHex, out Color startColor))
        {
            targetParams.startColor = startColor;
        }

        if (!string.IsNullOrEmpty(endColorHex) && ColorUtility.TryParseHtmlString(endColorHex, out Color endColor))
        {
            targetParams.endColor = endColor;
        }

        // 设置持续时间
        if (duration > 0)
        {
            targetParams.duration = duration;
        }

        if (enableDebugLogs)
            Debug.Log($"已设置灯带{stripIndex}的渐变参数：起始颜色={startColorHex}, 结束颜色={endColorHex}, 持续时间={duration}ms");
    }

    // Yarn命令：同时设置两条灯带的渐变脉冲效果参数
    [YarnCommand("set_both_gradient_pulse_params")]
    public void SetBothGradientPulseParams(string strip1StartColor, string strip1EndColor, int strip1Duration,
                                         string strip2StartColor, string strip2EndColor, int strip2Duration)
    {
        SetGradientPulseParams(0, strip1StartColor, strip1EndColor, strip1Duration);
        SetGradientPulseParams(1, strip2StartColor, strip2EndColor, strip2Duration);
    }

    // Yarn命令：启动充能效果
    [YarnCommand("start_charging_effect")]
    public void StartChargingEffect(int stripIndex, int targetMapping, float pushDistance = 8f, float pushSpeed = 20f, float decaySpd = 3f)
    {
        if (stripIndex < 0 || stripIndex > 1)
        {
            Debug.LogError($"无效的灯带索引: {stripIndex}");
            return;
        }

        // 设置充能参数
        chargeDistance = pushDistance;
        chargeSpeed = pushSpeed;
        decaySpeed = decaySpd;
        targetButtonMapping = targetMapping;
        chargingStripIndex = stripIndex;

        // 初始化充能状态
        stripChargingMode[stripIndex] = true;
        currentChargePosition[stripIndex] = 0f;
        maxChargePosition[stripIndex] = 0f;
        isCharging[stripIndex] = false;

        if (enableDebugLogs)
            Debug.Log($"已启动灯带{stripIndex}的充能效果，目标映射: {targetMapping}, 推进距离: {pushDistance}, 推进速度: {pushSpeed}, 衰减速度: {decaySpd}");
    }

    // Yarn命令：停止充能效果
    [YarnCommand("stop_charging_effect")]
    public void StopChargingEffect(int stripIndex)
    {
        if (stripIndex < 0 || stripIndex > 1)
        {
            Debug.LogError($"无效的灯带索引: {stripIndex}");
            return;
        }

        stripChargingMode[stripIndex] = false;
        currentChargePosition[stripIndex] = 0f;
        maxChargePosition[stripIndex] = 0f;
        isCharging[stripIndex] = false;

        if (enableDebugLogs)
            Debug.Log($"已停止灯带{stripIndex}的充能效果");
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

    void Update()
    {
        // 处理充能效果的衰减
        for (int i = 0; i < 2; i++)
        {
            if (stripChargingMode[i] && !isCharging[i] && maxChargePosition[i] > 0)
            {
                // 如果没有达到末端，则慢慢往回退
                if (maxChargePosition[i] < 51f)
                {
                    maxChargePosition[i] -= decaySpeed * Time.deltaTime;
                    maxChargePosition[i] = Mathf.Max(0f, maxChargePosition[i]);

                    // 更新当前位置为最远位置
                    currentChargePosition[i] = maxChargePosition[i];

                    // 发送更新命令到Arduino
                    UpdateChargingEffect(i);
                }
            }
        }
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

        // 检查是否处于充能模式
        if (buttonIndex >= 0 && buttonIndex < 2 && stripChargingMode[buttonIndex])
        {
            HandleChargingEffect(buttonIndex);
            return;
        }

        try
        {
            // 根据按钮索引获取对应的脉冲效果类型
            string effectType = (buttonIndex == 0) ? strip1PulseEffectType : strip2PulseEffectType;

            // 现在buttonIndex直接对应灯带索引：0=红按钮/0号灯带，1=绿按钮/1号灯带
            if (effectType == "gradient")
            {
                // 渐变效果需要发送更多参数
                GradientParams gradientParams = (buttonIndex == 0) ? strip1GradientParams : strip2GradientParams;

                // 转换两个颜色为RGB值
                int r1 = Mathf.RoundToInt(gradientParams.startColor.r * 255);
                int g1 = Mathf.RoundToInt(gradientParams.startColor.g * 255);
                int b1 = Mathf.RoundToInt(gradientParams.startColor.b * 255);

                int r2 = Mathf.RoundToInt(gradientParams.endColor.r * 255);
                int g2 = Mathf.RoundToInt(gradientParams.endColor.g * 255);
                int b2 = Mathf.RoundToInt(gradientParams.endColor.b * 255);

                // 发送渐变脉冲效果命令：灯带索引、效果类型、第一个颜色RGB、第二个颜色RGB、持续时间
                UduinoManager.Instance.sendCommand("PulseEffect",
                    buttonIndex.ToString(),
                    effectType,
                    r1.ToString(),
                    g1.ToString(),
                    b1.ToString(),
                    r2.ToString(),
                    g2.ToString(),
                    b2.ToString(),
                    gradientParams.duration.ToString());

                if (enableDebugLogs)
                    Debug.Log($"已发送渐变脉冲效果命令，灯带索引: {buttonIndex}, 起始颜色: R={r1},G={g1},B={b1}, 结束颜色: R={r2},G={g2},B={b2}, 持续时间: {gradientParams.duration}ms");
            }
            else
            {
                // 其他效果使用原有逻辑
                // 根据按钮索引获取对应的颜色
                Color pulseColor = (buttonIndex == 0) ? optionStrip1Color : optionStrip2Color;

                // 转换颜色为RGB值
                int r = Mathf.RoundToInt(pulseColor.r * 255);
                int g = Mathf.RoundToInt(pulseColor.g * 255);
                int b = Mathf.RoundToInt(pulseColor.b * 255);

                // 发送命令：灯带索引、效果类型、RGB颜色值
                UduinoManager.Instance.sendCommand("PulseEffect",
                    buttonIndex.ToString(),
                    effectType,
                    r.ToString(),
                    g.ToString(),
                    b.ToString());

                if (enableDebugLogs)
                    Debug.Log($"已发送脉冲效果命令，按钮/灯带索引: {buttonIndex}, 效果类型: {effectType}, 颜色: R={r},G={g},B={b}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"发送脉冲效果命令失败: {e.Message}");
        }
    }

    // 处理充能效果
    void HandleChargingEffect(int stripIndex)
    {
        if (isCharging[stripIndex]) return; // 如果正在充能中，忽略新的按钮按下

        // 播放充能音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Energy", 1f, false);
            if (enableDebugLogs)
                Debug.Log("播放充能音效：Energy");
        }
        else
        {
            Debug.LogWarning("AudioManager未找到，无法播放充能音效");
        }

        // 触发摄像机震动
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.2f, 0.01f * currentChargePosition[stripIndex]); // 短暂轻微的震动
            if (enableDebugLogs)
                Debug.Log("触发摄像机震动");
        }
        else
        {
            Debug.LogWarning("CameraShake未找到，无法触发震动效果");
        }

        // 开始充能协程
        StartCoroutine(ChargingCoroutine(stripIndex));
    }

    // 充能协程
    System.Collections.IEnumerator ChargingCoroutine(int stripIndex)
    {
        isCharging[stripIndex] = true;

        float startPosition = maxChargePosition[stripIndex];
        float targetPosition = Mathf.Min(51f, maxChargePosition[stripIndex] + chargeDistance);

        // 推进阶段
        float elapsedTime = 0f;
        float duration = (targetPosition - startPosition) / chargeSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            currentChargePosition[stripIndex] = Mathf.Lerp(startPosition, targetPosition, progress);
            maxChargePosition[stripIndex] = Mathf.Max(maxChargePosition[stripIndex], currentChargePosition[stripIndex]);

            // 发送更新命令到Arduino
            UpdateChargingEffect(stripIndex);

            yield return null;
        }

        // 确保到达目标位置
        currentChargePosition[stripIndex] = targetPosition;
        maxChargePosition[stripIndex] = targetPosition;
        UpdateChargingEffect(stripIndex);

        // 检查是否达到末端
        if (maxChargePosition[stripIndex] >= 51f)
        {
            // 充能完成！
            OnChargingComplete(stripIndex);
        }

        isCharging[stripIndex] = false;
    }

    // 更新充能效果到Arduino
    void UpdateChargingEffect(int stripIndex)
    {
        if (!IsUduinoAvailable) return;

        try
        {
            // 获取颜色
            Color chargeColor = (stripIndex == 0) ? optionStrip1Color : optionStrip2Color;
            int r = Mathf.RoundToInt(chargeColor.r * 255);
            int g = Mathf.RoundToInt(chargeColor.g * 255);
            int b = Mathf.RoundToInt(chargeColor.b * 255);

            int chargePos = Mathf.RoundToInt(currentChargePosition[stripIndex]);

            // 发送充能效果命令
            UduinoManager.Instance.sendCommand("ChargingEffect",
                stripIndex.ToString(),
                chargePos.ToString(),
                r.ToString(),
                g.ToString(),
                b.ToString());

            if (enableDebugLogs)
                Debug.Log($"发送充能效果命令: 灯带{stripIndex}, 位置{chargePos}, 颜色R={r},G={g},B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"发送充能效果命令失败: {e.Message}");
        }
    }

    // 充能完成处理
    void OnChargingComplete(int stripIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"灯带{stripIndex}充能完成！");

        // 播放充能完成音效
        if (AudioManager.Instance != null)
        {
            // 尝试播放专门的充能完成音效，如果不存在则使用Energy音效
            string completeSoundName = "EnergyComplete";
            if (AudioManager.Instance.HasSound(completeSoundName))
            {
                AudioManager.Instance.PlaySound(completeSoundName, 1f, false);
                if (enableDebugLogs)
                    Debug.Log($"播放充能完成音效：{completeSoundName}");
            }
            else
            {
                AudioManager.Instance.PlaySound("Energy", 1f, false);
                if (enableDebugLogs)
                    Debug.Log("播放充能完成音效：Energy（备用）");
            }
        }

        // 触发更强烈的摄像机震动
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.5f, 0.15f); // 更长时间、更强烈的震动
            if (enableDebugLogs)
                Debug.Log("触发充能完成震动效果");
        }

        // 停止充能模式
        stripChargingMode[stripIndex] = false;

        // 设置按钮映射
        if (stripIndex == 0)
        {
            redButtonOptionIndex = targetButtonMapping;
        }
        else if (stripIndex == 1)
        {
            greenButtonOptionIndex = targetButtonMapping;
        }

        // 设置脉冲效果为彩虹
        if (stripIndex == 0)
        {
            strip1PulseEffectType = "rainbow";
        }
        else if (stripIndex == 1)
        {
            strip2PulseEffectType = "rainbow";
        }

        if (enableDebugLogs)
            Debug.Log($"已设置灯带{stripIndex}的按钮映射为{targetButtonMapping}，脉冲效果为rainbow");
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