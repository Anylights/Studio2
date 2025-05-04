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

    public event System.Action OnOptionsShown;
    public event System.Action OnOptionsHidden;

    public void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();

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
    private void Update()
    {
        if (!optionsActive) return;

        // 检查键盘输入
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentOptions.Length > 0)
        {
            // 选择第一个选项（索引0）
            SelectOption(0);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && currentOptions.Length > 1)
        {
            // 选择第二个选项（索引1）
            SelectOption(1);
            return;
        }

        // 检查 Arduino 控制器实例是否存在
        if (ArduinoController.Instance == null)
        {
            return;
        }

        // 检测 Arduino 按钮按下事件
        // 按下红色按钮 (Pin 2) - 始终选择显示的第一个选项（无论是否可用）
        if (ArduinoController.Instance.RedButtonDown && currentOptions.Length > 0)
        {
            // 选择第一个选项（索引0）
            SelectOption(0);
        }
        // 按下绿色按钮 (Pin 3) - 始终选择显示的第二个选项（无论是否可用）
        else if (ArduinoController.Instance.GreenButtonDown && currentOptions.Length > 1)
        {
            // 选择第二个选项（索引1）
            SelectOption(1);
        }
    }

    public void RunLine(LocalizedLine dialogueLine)
    {
        lastSeenLine = dialogueLine;
    }

    public void RunOptions(DialogueOption[] options)
    {
        currentOptions = options;
        availableOptionIndices.Clear();
        optionsActive = true;

        // 触发选项显示事件
        OnOptionsShown?.Invoke();

        // 隐藏所有选项UI
        HideAllOptions(false); // 立即隐藏，不使用淡出效果

        // 为每个选项设置UI
        int availableCount = 0;
        List<int> uiIndices = new List<int>(); // 记录需要显示的UI索引

        for (int i = 0; i < options.Length; i++)
        {
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
                Debug.LogWarning($"选项数量超过了可用UI数量，最多显示 {optionUIObjects.Count} 个选项");
                break;
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
        if (optionIndex >= 0 && optionIndex < currentOptions.Length && currentOptions[optionIndex].IsAvailable)
        {
            // 先发送事件
            EventCenter.Instance.TriggerEvent<int>("optionSelected", optionIndex);

            // 标记选项为非活动
            optionsActive = false;

            // 开始淡出效果，但延迟实际的选项选择
            StartCoroutine(DelayedOptionSelection(optionIndex));
        }
    }

    // 先淡出UI，然后延迟选择选项
    private IEnumerator DelayedOptionSelection(int optionIndex)
    {
        // 先淡出所有选项
        yield return StartCoroutine(FadeOutAllOptions());

        // 等待额外的时间让Arduino完成脉冲效果
        yield return new WaitForSeconds(0.5f);

        // 再选择选项
        runner.SetSelectedOption(currentOptions[optionIndex].DialogueOptionID);
    }

    // 添加一个公共方法，供其他脚本检查选项是否正在显示
    public bool IsShowingOptions()
    {
        return optionsActive;
    }

    public void HandleExternalButtonPress(int buttonIndex)
    {
        if (!optionsActive) return;

        // 确保索引有效
        if (buttonIndex >= 0 && buttonIndex < currentOptions.Length)
        {
            Debug.Log($"外部按钮按下，选择选项：{buttonIndex}");
            SelectOption(buttonIndex);
        }
    }
}