using System.Collections;  // 添加这一行
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using TMPro;
using Febucci.UI;  // 添加 TextAnimator 命名空间

public class CharacterDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterDialogueInfo
    {
        public string characterName; // Yarn对话中使用的角色名
        public GameObject characterObject; // 角色游戏对象
        public GameObject dialogueBox; // 该角色的对话框对象
        public Vector3 offset = new Vector3(0, 2, 0); // 对话框偏移量
        public bool followCharacter = false; // 此对话框是否应跟随角色
    }

    [Header("对话框设置")]
    [SerializeField] private GameObject defaultDialogueBox; // 默认对话框
    [SerializeField] private List<CharacterDialogueInfo> characters = new List<CharacterDialogueInfo>();
    [SerializeField] private float maxDialogueWidth = 400f; // 添加最大宽度设置

    [Header("对话框效果")]
    [SerializeField] private bool autoAdvanceDialogue = false; // 是否自动推进对话
    [SerializeField] private float dialogueDuration = 3f; // 自动关闭对话的时间
    [SerializeField] private float fadeInDuration = 0.3f; // 对话框淡入时间
    [SerializeField] private float fadeOutDuration = 0.3f; // 对话框淡出时间

    // 当前活跃的对话框
    private GameObject activeDialogue = null;
    private TextMeshProUGUI activeText = null;
    private TypewriterByCharacter activeTypewriter = null;  // TextAnimator的打字机组件引用
    private Coroutine dismissCoroutine = null;
    private bool dialogueReady = false; // 对话是否准备好被推进
    private bool autoPlayNext = false; // 下一句对话是否自动播放（只影响下一句）

    // 字典用于快速查找角色
    private Dictionary<string, CharacterDialogueInfo> characterMap = new Dictionary<string, CharacterDialogueInfo>();

    private void Awake()
    {
        // 初始化字典
        foreach (var character in characters)
        {
            if (!string.IsNullOrEmpty(character.characterName))
            {
                characterMap[character.characterName.ToLower()] = character;
            }
        }

        // 确保所有对话框初始状态为隐藏
        if (defaultDialogueBox != null)
            defaultDialogueBox.SetActive(false);

        foreach (var character in characters)
        {
            if (character.dialogueBox != null)
                character.dialogueBox.SetActive(false);
        }
    }

    // 在Update方法中修改输入检测逻辑
    private void Update()
    {
        // 检查是否有选项界面显示
        bool optionsAreShowing = false;
        var optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null && optionsView.IsShowingOptions())
        {
            optionsAreShowing = true;
        }

        // 检查是否有Timeline在播放
        bool timelinePlaying = false;
        if (TimeLineManager.Instance != null && TimeLineManager.Instance.IsAnyPlaying())
        {
            timelinePlaying = true;
        }

        // 检查文本是否正在动画中显示
        bool textAnimating = false;
        if (activeTypewriter != null && activeTypewriter.isShowingText)
        {
            textAnimating = true;
        }
        else if (activeTypewriter != null && !activeTypewriter.isShowingText && activeDialogue != null)
        {
            // 如果文本显示完成，设置对话为准备好状态
            dialogueReady = true;
        }

        // 检查Arduino控制器是否可用
        bool arduinoInputAvailable = ArduinoController.Instance != null;
        bool arduinoButtonPressed = false;
        if (arduinoInputAvailable)
        {
            arduinoButtonPressed = ArduinoController.Instance.RedButtonDown || ArduinoController.Instance.GreenButtonDown;
        }

        // 只有在对话准备好且没有选项显示、没有Timeline播放、文本不在动画中的情况下，输入才有效
        if (dialogueReady && !optionsAreShowing && !timelinePlaying && !textAnimating &&
            (Input.GetKeyDown(KeyCode.Space) || arduinoButtonPressed))
        {
            ContinueDialogue();

            //触发默认脉冲效果，有bug，暂时不实现
            // int the_Button_Pressed = ArduinoController.Instance.RedButtonDown ? 0 : 1;
            // EventCenter.Instance.TriggerEvent<int>("ContinueDialogue", the_Button_Pressed);
        }

        // 更新所有活跃对话框的位置(如果设置了跟随)
        UpdateDialoguePositions();
    }

    // 处理新的对话行
    public void OnLinePresentation(LocalizedLine dialogueLine)
    {
        // 停止任何正在进行的对话
        StopAllCoroutines();
        HideAllDialogues();
        dialogueReady = false;

        string characterName = dialogueLine.CharacterName?.ToLower();

        // 判断使用哪个对话框
        if (string.IsNullOrEmpty(characterName) || !characterMap.ContainsKey(characterName))
        {
            // 使用默认对话框
            ShowDialogue(defaultDialogueBox, dialogueLine.TextWithoutCharacterName.Text);
        }
        else
        {
            // 使用角色对话框
            var character = characterMap[characterName];
            ShowDialogue(character.dialogueBox, dialogueLine.TextWithoutCharacterName.Text, character);
        }
    }

    // 修改ShowDialogue方法，确保对话框可见并使用TextAnimator
    private void ShowDialogue(GameObject dialogueBox, string text, CharacterDialogueInfo character = null)
    {
        if (dialogueBox == null) return;

        // 先清除当前活跃对话框的文本
        if (activeText != null)
        {
            activeText.text = "";
            activeText.ClearMesh(true);
        }

        activeDialogue = dialogueBox;
        dialogueBox.SetActive(true);

        // 查找并启用对话框的CanvasGroup
        CanvasGroup dialogueCanvasGroup = dialogueBox.GetComponent<CanvasGroup>();
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f;
            dialogueCanvasGroup.blocksRaycasts = true;
            dialogueCanvasGroup.interactable = true;
        }

        // 查找文本组件
        activeText = dialogueBox.GetComponentInChildren<TextMeshProUGUI>();
        if (activeText == null) return;

        // 获取DialogueBox的RectTransform
        RectTransform dialogueRect = dialogueBox.GetComponent<RectTransform>();
        if (dialogueRect != null)
        {
            // 设置固定宽度
            dialogueRect.sizeDelta = new Vector2(maxDialogueWidth, dialogueRect.sizeDelta.y);
        }

        // 强制设置文本属性以确保换行
        activeText.enableWordWrapping = true;
        activeText.overflowMode = TextOverflowModes.Overflow;
        activeText.alignment = TextAlignmentOptions.TopLeft;
        activeText.margin = new Vector4(10, 10, 10, 10);
        activeText.enableAutoSizing = false;
        activeText.horizontalAlignment = HorizontalAlignmentOptions.Left;

        // 直接设置文本框大小
        RectTransform textRect = activeText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        // 直接限制文本宽度
        float textWidth = maxDialogueWidth - 20;  // 减去左右边距
        activeText.rectTransform.sizeDelta = new Vector2(textWidth, activeText.rectTransform.sizeDelta.y);

        // 确保文本对象的CanvasGroup也设置为可见
        CanvasGroup textCanvasGroup = activeText.GetComponent<CanvasGroup>();
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 1f;
            textCanvasGroup.blocksRaycasts = true;
            textCanvasGroup.interactable = true;
        }

        // 确保文本为空并清除网格
        activeText.text = "";
        activeText.ClearMesh(true);

        // 查找TypewriterByCharacter组件
        activeTypewriter = dialogueBox.GetComponentInChildren<TypewriterByCharacter>();
        if (activeTypewriter == null)
        {
            Debug.LogWarning($"对话框 {dialogueBox.name} 上没有找到 TypewriterByCharacter 组件，将直接显示文本。");

            // 强制换行处理
            string processedText = ForceTextWrapping(text, textWidth);
            activeText.text = processedText;

            // 强制重新计算文本
            activeText.ForceMeshUpdate(true);
            dialogueReady = true;
        }
        else
        {
            // 强制换行处理
            string processedText = ForceTextWrapping(text, textWidth);

            // 先设置文本以强制计算布局
            activeText.text = processedText;

            // 强制重新计算文本和布局
            Canvas.ForceUpdateCanvases();
            activeText.ForceMeshUpdate(true);

            // 清空文本，准备打字机效果
            activeText.text = "";

            // 使用处理过的文本显示
            activeTypewriter.ShowText(processedText);
            activeTypewriter.onTextShowed.AddListener(OnTypewriterComplete);
        }

        // 如果是角色对话框并且设置了跟随，立即更新位置
        if (character != null && character.characterObject != null && character.followCharacter)
        {
            UpdateDialoguePosition(dialogueBox, character.characterObject, character.offset);
        }

        // 开始淡入动画
        StartCoroutine(FadeInDialogue(dialogueCanvasGroup));
    }

    // 添加强制换行处理方法
    private string ForceTextWrapping(string originalText, float maxWidth)
    {
        if (string.IsNullOrEmpty(originalText) || activeText == null)
            return originalText;

        // 估算每行大约能容纳的字符数
        float fontSize = activeText.fontSize;
        int charsPerLine = Mathf.FloorToInt(maxWidth / (fontSize * 0.5f)); // 0.5f是粗略估计的字符宽度比例

        // 如果字符数太小，使用最小值
        charsPerLine = Mathf.Max(charsPerLine, 10);

        // 分割成单词
        string[] words = originalText.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            // 如果当前行加上这个单词会超过最大字符数，则换行
            if (currentLine.Length + word.Length + 1 > charsPerLine)
            {
                result += currentLine.Trim() + "\n";
                currentLine = word + " ";
            }
            else
            {
                currentLine += word + " ";
            }
        }

        // 添加最后一行
        if (!string.IsNullOrEmpty(currentLine))
            result += currentLine.Trim();

        return result;
    }

    // 添加淡入动画协程
    private IEnumerator FadeInDialogue(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) yield break;

        // 获取文本对象的CanvasGroup
        CanvasGroup textCanvasGroup = activeText?.GetComponent<CanvasGroup>();

        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            canvasGroup.alpha = alpha;
            if (textCanvasGroup != null) textCanvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;
    }

    // 当TextAnimator完成文本动画时调用
    private void OnTypewriterComplete()
    {
        // 文本完全显示后，标记对话准备好继续
        dialogueReady = true;

        // 如果设置了单次自动播放下一句，立即继续对话
        if (autoPlayNext)
        {
            autoPlayNext = false; // 重置标志，确保只影响一句对话
            StartCoroutine(AutoContinueAfterDelay());
            return;
        }

        // 如果设置了自动推进，则启动自动关闭计时器
        if (autoAdvanceDialogue)
        {
            if (dismissCoroutine != null)
                StopCoroutine(dismissCoroutine);

            dismissCoroutine = StartCoroutine(AutoDismiss());
        }
    }

    // 自动关闭对话框
    private System.Collections.IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(dialogueDuration);

        if (autoAdvanceDialogue && dialogueReady)
            ContinueDialogue();
    }

    // 自动继续到下一句对话（稍微延迟以确保界面更新完成）
    private System.Collections.IEnumerator AutoContinueAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // 短暂延迟确保打字机效果完全结束

        if (dialogueReady)
            ContinueDialogue();
    }

    // 更新对话框位置
    private void UpdateDialoguePosition(GameObject dialogueBox, GameObject character, Vector3 offset)
    {
        if (dialogueBox == null || character == null) return;

        RectTransform rectTransform = dialogueBox.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 获取Canvas的世界缩放
        float canvasScale = canvas.transform.localScale.x;

        if (Camera.main != null)
        {
            // 将角色位置转换为Canvas上的本地坐标
            Vector3 characterWorldPos = character.transform.position + offset;

            // 对于World Space Canvas，需要考虑Canvas的位置和旋转
            Vector3 canvasLocalPoint;

            // 将世界坐标转换为Canvas的局部坐标
            canvasLocalPoint = canvas.transform.InverseTransformPoint(characterWorldPos);

            // 设置RectTransform的锚点位置（本地坐标）
            rectTransform.localPosition = canvasLocalPoint;
        }
    }

    // 更新所有活跃对话框的位置
    private void UpdateDialoguePositions()
    {
        foreach (var character in characters)
        {
            // 检查对话框是否激活，是否有角色对象，以及是否设置了跟随
            if (character.dialogueBox != null && character.dialogueBox.activeSelf && character.characterObject != null && character.followCharacter)
            {
                UpdateDialoguePosition(character.dialogueBox, character.characterObject, character.offset);
            }
        }
    }

    // 修改隐藏对话框的方法
    public void HideAllDialogues()
    {
        // 清除当前活跃对话框的文本
        if (activeText != null)
        {
            activeText.text = "";
            activeText.ClearMesh(true);
        }

        // 重置状态
        activeText = null;
        activeTypewriter = null;
        dialogueReady = false;

        // 使用淡出效果隐藏默认对话框
        if (defaultDialogueBox != null)
        {
            var defaultText = defaultDialogueBox.GetComponentInChildren<TextMeshProUGUI>();
            if (defaultText != null)
            {
                defaultText.text = "";
                defaultText.ClearMesh(true);
            }
            StartCoroutine(FadeOutDialogue(defaultDialogueBox));
        }

        // 使用淡出效果隐藏所有角色对话框
        foreach (var character in characters)
        {
            if (character.dialogueBox != null)
            {
                var characterText = character.dialogueBox.GetComponentInChildren<TextMeshProUGUI>();
                if (characterText != null)
                {
                    characterText.text = "";
                    characterText.ClearMesh(true);
                }
                StartCoroutine(FadeOutDialogue(character.dialogueBox));
            }
        }

        if (activeTypewriter != null)
        {
            activeTypewriter.onTextShowed.RemoveListener(OnTypewriterComplete);
        }
    }

    // 添加淡出动画协程
    private IEnumerator FadeOutDialogue(GameObject dialogueBox)
    {
        if (dialogueBox == null) yield break;

        CanvasGroup canvasGroup = dialogueBox.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        // 获取文本对象的CanvasGroup
        CanvasGroup textCanvasGroup = dialogueBox.GetComponentInChildren<TextMeshProUGUI>()?.GetComponent<CanvasGroup>();

        float elapsedTime = 0f;
        canvasGroup.alpha = 1f;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            canvasGroup.alpha = alpha;
            if (textCanvasGroup != null) textCanvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
        dialogueBox.SetActive(false);
    }

    // 继续对话
    public void ContinueDialogue()
    {
        if (!dialogueReady) return;

        dialogueReady = false;
        StopAllCoroutines();
        HideAllDialogues();

        var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
        if (dialogueRunner != null)
            dialogueRunner.Continue();
    }

    // Yarn命令：设置下一句对话自动播放
    [YarnCommand("auto_next")]
    public void SetAutoNext()
    {
        autoPlayNext = true;
        Debug.Log("设置下一句对话自动播放");
    }
}