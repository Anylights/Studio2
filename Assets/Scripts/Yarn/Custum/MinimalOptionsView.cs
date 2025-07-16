using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using TMPro;

public class MinimalOptionsView : MonoBehaviour
{
    [Header("选项UI设置")]
    [Tooltip("手动设置的选项UI列表，请确保每个物体都有TextMeshProUGUI组件")]
    [SerializeField] private List<GameObject> optionUIObjects = new List<GameObject>();

    [SerializeField] private TextMeshProUGUI lastLineText;
    [SerializeField] private float fadeTime = 0.25f; // 淡入淡出时间
    [SerializeField] private float selectionDelay = 0.5f; // 选择选项后的延迟时间

    [SerializeField] private Color optionStrip1Color = Color.red;   // 显示对话选项时灯带1的颜色
    [SerializeField] private Color optionStrip2Color = Color.green; // 显示对话选项时灯带2的颜色
    [SerializeField] private bool enableDebugLog = true;

    [Header("输入设置")] // 添加一个新的Header以便在Inspector中分类
    [Tooltip("选项显示后，输入开始响应前的额外冷却时间（秒）")]
    [SerializeField] private float optionDisplayInputCooldown = 0.1f;
    private float blockInputUntilTime = 0f; // 输入将被阻塞直到这个时间点

    // 每个选项UI对应的组件缓存
    private List<TextMeshProUGUI> optionTextComponents = new List<TextMeshProUGUI>();
    private List<CanvasGroup> optionCanvasGroups = new List<CanvasGroup>();

    // 当前显示的选项
    private DialogueOption[] currentOptions;
    // 当前可用选项的索引列表
    private List<int> availableOptionIndices = new List<int>();

    private LocalizedLine lastSeenLine;
    private MinimalDialogueRunner runner;

    private bool optionsActive = false;  // 选项是否激活中
    private bool selectionInProgress = false; // 选择过程是否进行中

    // 事件声明
    public event System.Action OnOptionsShown;
    public event System.Action OnOptionsHidden;
    public event System.Action<int> OnSelectionComplete; // 新增：选项处理完成事件

    // 上次选择的选项索引
    private int lastSelectedOptionIndex = -1;

    // 启用事件中心集成
    private void OnEnable()
    {
        // 发布选项选择完成事件
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.Subscribe<int>("optionSelectionComplete", HandleSelectionComplete);
        }
    }

    private void OnDisable()
    {
        // 取消订阅事件中心事件
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.Unsubscribe<int>("optionSelectionComplete", HandleSelectionComplete);
        }
    }

    // 内部处理选项选择完成
    private void HandleSelectionComplete(int optionIndex)
    {
        if (enableDebugLog)
        {
            Debug.Log($"选项选择完成：{optionIndex}");
        }
    }

    public void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("无法找到MinimalDialogueRunner，选项视图无法工作");
        }

        // 初始化时缓存所有文本组件
        InitializeComponents();

        // 初始时隐藏所有选项UI
        HideAllOptions(false); // 立即隐藏，不使用淡出效果
    }

    private void InitializeComponents()
    {
        optionTextComponents.Clear();
        optionCanvasGroups.Clear();

        // 获取并缓存每个UI对象的组件
        foreach (var uiObject in optionUIObjects)
        {
            if (uiObject != null)
            {
                // 获取TextMeshProUGUI组件
                TextMeshProUGUI text = uiObject.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    optionTextComponents.Add(text);
                }
                else
                {
                    Debug.LogWarning($"选项UI {uiObject.name} 没有TextMeshProUGUI组件!");
                    optionTextComponents.Add(null);
                }

                // 获取或添加CanvasGroup组件
                CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = uiObject.AddComponent<CanvasGroup>();
                }
                optionCanvasGroups.Add(canvasGroup);

                // 初始时隐藏
                uiObject.SetActive(false);
            }
            else
            {
                optionTextComponents.Add(null);
                optionCanvasGroups.Add(null);
            }
        }
    }

    // 脉冲渐变参数结构体
    private struct PulseGradientParams
    {
        public Color? startColor;
        public Color? endColor;
        public float? duration;
    }
    private PulseGradientParams? nextPulseGradient = null;

    // 充能参数类
    private class ChargingEffectParams
    {
        public float step = 0.2f;
        public float max = 1f;
        public float duration = 0.5f;
        public Color color = Color.red;
        public float progress = 0f;
        public bool charging = false;
        public float decaySpeed = 0.1f; // 每秒衰减
        public int optionIndex = 0; // 充能目标选项
    }
    private ChargingEffectParams chargingEffect = null;

    // Yarn命令：设置下次脉冲渐变参数
    [YarnCommand("set_line_gradient")]
    public void SetLineGradient(string startColorHex, string endColorHex, float duration)
    {
        Color start, end;
        if (!ColorUtility.TryParseHtmlString(startColorHex, out start)) start = Color.red;
        if (!ColorUtility.TryParseHtmlString(endColorHex, out end)) end = Color.green;
        nextPulseGradient = new PulseGradientParams { startColor = start, endColor = end, duration = duration };
        if (enableDebugLog)
            Debug.Log($"[LineEffect] 设置下次脉冲渐变: {startColorHex} -> {endColorHex}, 持续{duration}s");
    }

    // Yarn命令：设置充能光效参数
    [YarnCommand("start_line_charging_effect")]
    public void StartLineChargingEffect(float step, float max, float duration, string colorHex, int optionIndex = 0, float decaySpeed = 0.1f)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(colorHex, out color)) color = Color.red;
        chargingEffect = new ChargingEffectParams
        {
            step = step,
            max = max,
            duration = duration,
            color = color,
            progress = 0f,
            charging = true,
            optionIndex = optionIndex,
            decaySpeed = decaySpeed
        };
        Debug.Log($"[充能命令] StartLineChargingEffect 被调用: step={step}, max={max}, duration={duration}, color={colorHex}, optionIndex={optionIndex}, decaySpeed={decaySpeed}");
        if (enableDebugLog)
            Debug.Log($"[LineEffect] 启动充能光效: step={step}, max={max}, duration={duration}, color={colorHex}, option={optionIndex}, decay={decaySpeed}");
    }

    private void Update()
    {
        // 充能模式优先，绝不提前return
        if (chargingEffect != null && chargingEffect.charging)
        {
            Debug.Log($"[充能推进] chargingEffect.charging={chargingEffect.charging}, progress={chargingEffect.progress}/{chargingEffect.max}");
            int chargingRedOptionIndex = 0;
            int chargingGreenOptionIndex = 1;
            bool chargingLeftKeyPressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) ||
                                          Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F);
            bool chargingRightKeyPressed = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) ||
                                           Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Semicolon);
            // 充能时Bloom强度为常量
            if (LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
            {
                LinePathEffect.Instance.bloom.intensity.value = LinePathEffect.Instance.bloomChargingIntensity;
            }
            bool trigger = false;
            if (chargingEffect.optionIndex == chargingRedOptionIndex && chargingLeftKeyPressed) trigger = true;
            if (chargingEffect.optionIndex == chargingRedOptionIndex && ArduinoController.Instance != null && ArduinoController.Instance.RedButtonDown) trigger = true;
            if (chargingEffect.optionIndex == chargingGreenOptionIndex && chargingRightKeyPressed) trigger = true;
            if (chargingEffect.optionIndex == chargingGreenOptionIndex && ArduinoController.Instance != null && ArduinoController.Instance.GreenButtonDown) trigger = true;
            if (trigger)
            {
                // 推进充能
                chargingEffect.progress += chargingEffect.step;
                chargingEffect.progress = Mathf.Min(chargingEffect.progress, chargingEffect.max);
                Debug.Log($"[充能推进] 按钮触发: 当前进度={chargingEffect.progress}/{chargingEffect.max}");
                // 绘制充能线条
                if (LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
                {
                    // 按进度线性插值Bloom强度，充能满时为最大
                    // bloomIntensity = Mathf.Lerp(0f, LinePathEffect.Instance.bloomMaxIntensity, chargingEffect.progress / chargingEffect.max);
                    // LinePathEffect.Instance.bloom.intensity.value = bloomIntensity;
                }
                LinePathEffect.Instance.DrawChargingLine(
                    chargingEffect.progress / chargingEffect.max,
                    chargingEffect.color,
                    chargingEffect.color
                );
                Debug.Log($"[充能推进] DrawChargingLine: ratio={chargingEffect.progress / chargingEffect.max}, Bloom={LinePathEffect.Instance.bloom.intensity.value}");
                // 充能满，自动选择
                if (chargingEffect.progress >= chargingEffect.max)
                {
                    chargingEffect.charging = false;
                    if (LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
                    {
                        LinePathEffect.Instance.bloom.intensity.value = LinePathEffect.Instance.bloomMaxIntensity;
                    }
                    Debug.Log($"[充能推进] 充能满，自动选择选项: {chargingEffect.optionIndex}");
                    LinePathEffect.Instance.DrawLines(chargingEffect.duration, chargingEffect.color);
                    SelectOption(chargingEffect.optionIndex);
                }
                return; // 只拦截充能按钮
            }
            // 衰减进度
            if (chargingEffect.progress > 0f)
            {
                chargingEffect.progress -= chargingEffect.decaySpeed * Time.deltaTime;
                chargingEffect.progress = Mathf.Max(0f, chargingEffect.progress);
            }
            // 绘制充能线条
            if (LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
            {
                // 按进度线性插值Bloom强度，充能满时为最大
                // bloomIntensity = Mathf.Lerp(0f, LinePathEffect.Instance.bloomMaxIntensity, chargingEffect.progress / chargingEffect.max);
                // LinePathEffect.Instance.bloom.intensity.value = bloomIntensity;
            }
            LinePathEffect.Instance.DrawChargingLine(
                chargingEffect.progress / chargingEffect.max,
                chargingEffect.color,
                chargingEffect.color
            );
            Debug.Log($"[充能推进] DrawChargingLine: ratio={chargingEffect.progress / chargingEffect.max}, Bloom={LinePathEffect.Instance.bloom.intensity.value}");
            // 其他按钮（如右键）不return，继续走后面的普通分支
        }
        // 在处理任何输入之前，检查是否在输入阻塞期
        if (Time.time < blockInputUntilTime)
        {
            return; // 如果在阻塞期，则不处理任何输入
        }

        // 如果选项不活跃或者选择正在进行中，不处理输入
        if (!optionsActive || selectionInProgress) return;

        // 获取当前按钮映射
        int redOptionIndex = 0;
        int greenOptionIndex = 1;
        var rgbController = FindObjectOfType<RgbController>();
        if (rgbController != null)
        {
            redOptionIndex = rgbController.GetRedButtonOptionIndex();
            greenOptionIndex = rgbController.GetGreenButtonOptionIndex();
        }

        // 检查键盘输入：ASDF 作为左按钮，JKL和分号作为右按钮
        bool leftKeyPressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) ||
                              Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F);
        bool rightKeyPressed = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) ||
                               Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Semicolon);

        // 充能光效逻辑（只拦截目标按钮，其他按钮正常）
        if (chargingEffect != null && chargingEffect.charging)
        {
            // 只允许目标按钮推进充能
            // float bloomIntensity = 0f; // This variable is no longer needed
            bool trigger = false;
            if (chargingEffect.optionIndex == redOptionIndex && leftKeyPressed) trigger = true;
            if (chargingEffect.optionIndex == greenOptionIndex && rightKeyPressed) trigger = true;
            if (ArduinoController.Instance != null)
            {
                if (chargingEffect.optionIndex == redOptionIndex && ArduinoController.Instance.RedButtonDown) trigger = true;
                if (chargingEffect.optionIndex == greenOptionIndex && ArduinoController.Instance.GreenButtonDown) trigger = true;
            }
            if (trigger)
            {
                // 只推进进度，不允许直接SelectOption，也不播放普通光效
                chargingEffect.progress += chargingEffect.step;
                chargingEffect.progress = Mathf.Min(chargingEffect.progress, chargingEffect.max);
                if (LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
                {
                    // 按进度线性插值Bloom强度，充能满时为最大
                    // bloomIntensity = Mathf.Lerp(0f, LinePathEffect.Instance.bloomMaxIntensity, chargingEffect.progress / chargingEffect.max);
                    // LinePathEffect.Instance.bloom.intensity.value = bloomIntensity;
                }
                LinePathEffect.Instance.DrawChargingLine(
                    chargingEffect.progress / chargingEffect.max,
                    chargingEffect.color,
                    chargingEffect.color
                );
                if (enableDebugLog)
                    Debug.Log($"[LineEffect] 充能推进: 当前进度={chargingEffect.progress}/{chargingEffect.max}");
                if (chargingEffect.progress >= chargingEffect.max)
                {
                    chargingEffect.charging = false;
                    LinePathEffect.Instance.DrawLines(chargingEffect.duration, chargingEffect.color);
                    SelectOption(chargingEffect.optionIndex);
                }
                return; // 只要充能推进被处理，必须return，防止走到默认分支
            }
            // 非目标按钮，充能模式下不允许直接选择目标选项
            // 其他按钮可正常选择其他选项
            // 但此时不应播放普通光效
        }

        // 键盘左键（ASDF）选择红色按钮映射的选项
        if (leftKeyPressed)
        {
            if (nextPulseGradient != null)
            {
                var p = nextPulseGradient.Value;
                LinePathEffect.Instance.DrawLines(p.duration ?? 0.5f, p.startColor, p.endColor);
                nextPulseGradient = null;
            }
            else
            {
                LinePathEffect.Instance.DrawLines(0.5f, optionStrip1Color);
            }
            if (redOptionIndex >= 0 && redOptionIndex < currentOptions.Length)
            {
                SelectOption(redOptionIndex);
            }
            return;
        }
        // 键盘右键（JKL;）选择绿色按钮映射的选项
        if (rightKeyPressed)
        {
            if (nextPulseGradient != null)
            {
                var p = nextPulseGradient.Value;
                LinePathEffect.Instance.DrawLines(p.duration ?? 0.5f, p.startColor, p.endColor);
                nextPulseGradient = null;
            }
            else
            {
                LinePathEffect.Instance.DrawLines(0.5f, optionStrip2Color);
            }
            if (greenOptionIndex >= 0 && greenOptionIndex < currentOptions.Length)
            {
                SelectOption(greenOptionIndex);
            }
            return;
        }

        // 检测 Arduino 按钮按下事件（原有逻辑保留）
        if (ArduinoController.Instance != null)
        {
            if (ArduinoController.Instance.RedButtonDown)
            {
                EventCenter.Instance.TriggerEvent<int>("buttonPressed", 0);
                if (nextPulseGradient != null)
                {
                    var p = nextPulseGradient.Value;
                    LinePathEffect.Instance.DrawLines(p.duration ?? 0.5f, p.startColor, p.endColor);
                    nextPulseGradient = null;
                }
                else
                {
                    LinePathEffect.Instance.DrawLines(0.5f, optionStrip1Color);
                }
                if (redOptionIndex >= 0 && redOptionIndex < currentOptions.Length)
                {
                    SelectOption(redOptionIndex);
                }
            }
            else if (ArduinoController.Instance.GreenButtonDown)
            {
                EventCenter.Instance.TriggerEvent<int>("buttonPressed", 1);
                if (nextPulseGradient != null)
                {
                    var p = nextPulseGradient.Value;
                    LinePathEffect.Instance.DrawLines(p.duration ?? 0.5f, p.startColor, p.endColor);
                    nextPulseGradient = null;
                }
                else
                {
                    LinePathEffect.Instance.DrawLines(0.5f, optionStrip2Color);
                }
                if (greenOptionIndex >= 0 && greenOptionIndex < currentOptions.Length)
                {
                    SelectOption(greenOptionIndex);
                }
            }
        }
        // 充能结束后（非充能状态），自动归零Bloom
        if ((chargingEffect == null || !chargingEffect.charging) && LinePathEffect.Instance != null && LinePathEffect.Instance.bloom != null)
        {
            LinePathEffect.Instance.bloom.intensity.value = 0f;
        }
    }

    public void RunLine(LocalizedLine dialogueLine)
    {
        lastSeenLine = dialogueLine;
    }

    public void RunOptions(DialogueOption[] options)
    {
        // 如果有进行中的选择，先取消
        if (selectionInProgress)
        {
            StopAllCoroutines(); // 确保停止所有相关协程，例如FadeOutAllOptions和DelayedOptionSelection
            selectionInProgress = false;
            // 如果之前的选择正在淡出，确保它们被立即隐藏
            HideAllOptions(false);
        }

        currentOptions = options;
        availableOptionIndices.Clear();
        optionsActive = true; // 选项现在处于活动状态，准备显示

        // 触发选项显示事件
        OnOptionsShown?.Invoke();

        // 立即隐藏所有旧选项UI，以防万一 (之前可能已在StopAllCoroutines后处理，但双重保险)
        HideAllOptions(false);

        // 为每个选项设置UI
        int availableCount = 0;
        List<int> uiIndices = new List<int>(); // 记录需要显示的UI索引

        for (int i = 0; i < options.Length; i++)
        {
            if (availableCount >= optionUIObjects.Count)
            {
                Debug.LogWarning($"选项数量超过了可用UI数量，最多显示 {optionUIObjects.Count} 个选项");
                break;
            }

            GameObject optionUI = optionUIObjects[availableCount];
            TextMeshProUGUI textComponent = optionTextComponents[availableCount];

            if (optionUI != null && textComponent != null)
            {
                // 准备选项UI
                optionUI.SetActive(true);

                // 设置选项文本
                textComponent.text = options[i].Line.TextWithoutCharacterName.Text;

                // 初始化CanvasGroup
                CanvasGroup canvasGroup = optionCanvasGroups[availableCount];
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = options[i].IsAvailable;
                    canvasGroup.blocksRaycasts = options[i].IsAvailable;
                }

                // 如果选项不可用，应用划线样式
                if (!options[i].IsAvailable)
                {
                    textComponent.fontStyle = FontStyles.Strikethrough;
                }
                else
                {
                    textComponent.fontStyle = FontStyles.Normal;
                    availableOptionIndices.Add(i);
                }

                uiIndices.Add(availableCount);
                availableCount++;
            }
            else
            {
                Debug.LogWarning($"选项UI出现问题，可能有UI物体或文本组件为空");
            }
        }

        // 设置最后一行文本
        if (lastLineText != null && lastSeenLine != null)
        {
            lastLineText.gameObject.SetActive(true);
            lastLineText.text = lastSeenLine.Text.Text;
        }

        // 淡入显示所有选项
        StartCoroutine(FadeInOptions(uiIndices));

        // 设置输入阻塞期：覆盖淡入时间和额外的冷却时间
        blockInputUntilTime = Time.time + fadeTime + optionDisplayInputCooldown;

        if (enableDebugLog)
        {
            Debug.Log($"显示 {options.Length} 个选项，其中 {availableOptionIndices.Count} 个可用");
        }
    }

    // 淡入显示选项
    private IEnumerator FadeInOptions(List<int> uiIndices)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime = Time.time - startTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeTime);

            // 更新所有需要显示的选项UI的透明度
            foreach (int index in uiIndices)
            {
                if (index < optionCanvasGroups.Count && optionCanvasGroups[index] != null)
                {
                    optionCanvasGroups[index].alpha = normalizedTime;
                }
            }

            yield return null;
        }

        // 确保所有选项完全显示
        foreach (int index in uiIndices)
        {
            if (index < optionCanvasGroups.Count && optionCanvasGroups[index] != null)
            {
                optionCanvasGroups[index].alpha = 1f;
            }
        }
    }

    // 隐藏所有选项UI
    private void HideAllOptions(bool withFade = true)
    {
        if (withFade)
        {
            // 使用淡出效果隐藏
            StartCoroutine(FadeOutAllOptions());
        }
        else
        {
            // 立即隐藏
            for (int i = 0; i < optionUIObjects.Count; i++)
            {
                if (optionUIObjects[i] != null)
                {
                    optionUIObjects[i].SetActive(false);
                }
            }
        }
    }

    // 淡出所有选项
    private IEnumerator FadeOutAllOptions()
    {
        List<int> activeIndices = new List<int>();

        // 收集当前活跃的选项索引
        for (int i = 0; i < optionUIObjects.Count; i++)
        {
            if (optionUIObjects[i] != null && optionUIObjects[i].activeSelf)
            {
                activeIndices.Add(i);
            }
        }

        if (activeIndices.Count == 0)
        {
            yield break;
        }

        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime = Time.time - startTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeTime);

            // 更新所有活跃选项的透明度
            foreach (int index in activeIndices)
            {
                if (optionCanvasGroups[index] != null)
                {
                    optionCanvasGroups[index].alpha = 1f - normalizedTime;
                }
            }

            yield return null;
        }

        // 完全隐藏所有选项
        foreach (int index in activeIndices)
        {
            if (optionUIObjects[index] != null)
            {
                optionUIObjects[index].SetActive(false);
            }
        }
    }

    // 选择指定索引的选项
    private void SelectOption(int optionIndex)
    {
        // 检查选项是否有效
        if (optionIndex < 0 || optionIndex >= currentOptions.Length || !currentOptions[optionIndex].IsAvailable)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"选项 {optionIndex} 不可用或无效");
            }
            return;
        }

        // 确保不同时处理多个选择
        if (selectionInProgress)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("已有选择正在处理中，忽略当前选择");
            }
            return;
        }

        // 当一个选项被选择时，我们认为输入阻塞期可以结束了（如果有的话）。
        // 主要的阻塞目的是防止RunOptions后立即选择，一旦选择发生，此特定阻塞解除。
        blockInputUntilTime = Time.time;

        // 标记选择进行中
        selectionInProgress = true;
        optionsActive = false;

        // 先发送事件
        EventCenter.Instance.TriggerEvent<int>("optionSelected", optionIndex);

        if (enableDebugLog)
        {
            Debug.Log($"选择了选项: {optionIndex}, ID: {currentOptions[optionIndex].DialogueOptionID}");
        }

        // 开始延迟选项选择过程
        StartCoroutine(DelayedOptionSelection(optionIndex));
    }

    // 先等待Arduino脉冲效果，然后淡出UI，最后延迟选择选项
    private IEnumerator DelayedOptionSelection(int optionIndex)
    {
        // 保存当前选择的选项索引
        lastSelectedOptionIndex = optionIndex;

        // 保存我们需要的选项ID，防止数组后面被清空
        int selectedOptionID = -1;

        // 保存当前选项信息，防止后续处理中currentOptions变为null
        if (currentOptions != null && optionIndex >= 0 && optionIndex < currentOptions.Length)
        {
            selectedOptionID = currentOptions[optionIndex].DialogueOptionID;
        }
        else
        {
            // 如果选项数据不可用，中止处理
            Debug.LogError("选项数据不可用");
            selectionInProgress = false;

            // 触发选择完成事件
            OnSelectionComplete?.Invoke(optionIndex);
            EventCenter.Instance.TriggerEvent<int>("optionSelectionComplete", optionIndex);

            yield break;
        }

        // 首先等待额外的时间让Arduino完成脉冲效果
        yield return new WaitForSeconds(selectionDelay);

        // 播放选项选择音效
        AudioManager.Instance.PlaySound("option_selected", 1f, false);

        // 触发选项隐藏事件
        OnOptionsHidden?.Invoke();

        // 立即隐藏所有其他选项，只保留被选择的选项
        for (int i = 0; i < optionUIObjects.Count; i++)
        {
            if (i != optionIndex && optionUIObjects[i] != null)
            {
                optionUIObjects[i].SetActive(false);
            }
        }

        // 让被选择的选项闪烁0.5秒
        float flashStartTime = Time.time;
        float flashDuration = 0.5f;
        float flashInterval = 0.1f; // 闪烁间隔
        bool isVisible = true;

        while (Time.time - flashStartTime < flashDuration)
        {
            if (optionUIObjects[optionIndex] != null)
            {
                isVisible = !isVisible;
                optionUIObjects[optionIndex].SetActive(isVisible);
            }
            yield return new WaitForSeconds(flashInterval);
        }

        // 确保选项最后是隐藏的
        if (optionUIObjects[optionIndex] != null)
        {
            optionUIObjects[optionIndex].SetActive(false);
        }

        if (runner.isRunning && selectedOptionID >= 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"准备向对话运行器发送选项ID: {selectedOptionID}");
            }

            runner.SetSelectedOption(selectedOptionID);
        }
        else
        {
            Debug.LogWarning($"对话运行器状态异常，无法选择选项: runner.isRunning={runner?.isRunning}, selectedOptionID={selectedOptionID}");
        }

        // 重置标志，以允许将来的选择
        selectionInProgress = false;

        // 触发选择完成事件
        OnSelectionComplete?.Invoke(optionIndex);
        EventCenter.Instance.TriggerEvent<int>("optionSelectionComplete", optionIndex);
    }

    // 获取上次选择的选项索引
    public int GetLastSelectedOptionIndex()
    {
        return lastSelectedOptionIndex;
    }

    // 添加一个公共方法，供其他脚本检查选项是否正在显示
    public bool IsShowingOptions()
    {
        return optionsActive;
    }

    // 添加一个公共方法，检查是否有选择操作正在进行
    public bool IsSelectionInProgress()
    {
        return selectionInProgress;
    }

    // 处理外部按钮按下事件
    public void HandleExternalButtonPress(int buttonIndex)
    {
        if (!optionsActive || selectionInProgress) return;

        // 确保索引有效
        if (buttonIndex >= 0 && buttonIndex < currentOptions.Length)
        {
            if (enableDebugLog)
            {
                Debug.Log($"外部按钮按下，选择选项：{buttonIndex}");
            }
            SelectOption(buttonIndex);
        }
    }

    // Yarn命令：设置选项选择延迟时间
    [YarnCommand("set_selection_delay")]
    public void SetSelectionDelay(float delay)
    {
        if (delay >= 0f)
        {
            selectionDelay = delay;
            if (enableDebugLog)
            {
                Debug.Log($"已设置选项选择延迟时间为: {delay}秒");
            }
        }
        else
        {
            Debug.LogWarning($"无效的延迟时间: {delay}，延迟时间必须大于等于0");
        }
    }
}