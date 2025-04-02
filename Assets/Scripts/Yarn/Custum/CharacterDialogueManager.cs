using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using TMPro;

public class CharacterDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterDialogueInfo
    {
        public string characterName; // Yarn对话中使用的角色名
        public GameObject characterObject; // 角色游戏对象
        public GameObject dialogueBox; // 该角色的对话框对象
        public Vector3 offset = new Vector3(0, 2, 0); // 对话框偏移量
    }

    [Header("对话设置")]
    [SerializeField] private GameObject defaultDialogueBox; // 默认对话框
    [SerializeField] private List<CharacterDialogueInfo> characters = new List<CharacterDialogueInfo>();

    [Header("对话效果")]
    [SerializeField] private float typingSpeed = 0.05f; // 打字效果速度
    [SerializeField] private bool useTypingEffect = true; // 是否使用打字效果
    [SerializeField] private bool autoAdvanceDialogue = false; // 是否自动推进对话
    [SerializeField] private float dialogueDuration = 3f; // 自动关闭对话的时间

    // 当前活跃的对话框
    private GameObject activeDialogue = null;
    private TextMeshProUGUI activeText = null;
    private Coroutine typingCoroutine = null;
    private Coroutine dismissCoroutine = null;
    private bool dialogueReady = false; // 对话是否准备好被推进
    private bool typingInProgress = false; // 是否正在打字中

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

    // 在Update方法中修改空格键检测逻辑
    private void Update()
    {
        // 检查是否有选项界面显示
        bool optionsAreShowing = false;
        var optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null)
        {
            // 检查选项界面是否激活
            CanvasGroup optionsCanvasGroup = optionsView.GetComponent<CanvasGroup>();
            if (optionsCanvasGroup != null && optionsCanvasGroup.alpha > 0)
            {
                optionsAreShowing = true;
            }
        }

        // 只有在没有选项界面显示且对话准备好的情况下，空格键才有效
        if (Input.GetKeyDown(KeyCode.Space) && dialogueReady && !optionsAreShowing)
        {
            // 如果正在打字，则先完成打字
            if (typingInProgress)
            {
                CompleteTyping();
            }
            else
            {
                ContinueDialogue();
            }
        }

        // 更新所有活跃对话框的位置
        UpdateDialoguePositions();
    }


    // 处理新的对话行
    // 修改OnLinePresentation方法
    public void OnLinePresentation(LocalizedLine dialogueLine)
    {
        // 停止任何正在进行的对话
        StopAllCoroutines();
        HideAllDialogues();
        dialogueReady = false;
        typingInProgress = false;

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

    // 修改ShowDialogue方法，确保对话框可见
    private void ShowDialogue(GameObject dialogueBox, string text, CharacterDialogueInfo character = null)
    {
        if (dialogueBox == null) return;

        activeDialogue = dialogueBox;
        dialogueBox.SetActive(true);

        // 查找并启用CanvasGroup
        CanvasGroup canvasGroup = dialogueBox.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // 强制设置为可见
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        // 查找文本组件
        activeText = dialogueBox.GetComponentInChildren<TextMeshProUGUI>();
        if (activeText == null) return;

        // 设置文本
        if (useTypingEffect)
        {
            activeText.text = "";
            typingInProgress = true;
            typingCoroutine = StartCoroutine(TypeText(activeText, text));
        }
        else
        {
            activeText.text = text;
            typingInProgress = false;
            OnTypingComplete();
        }

        // 如果是角色对话框，立即更新位置
        if (character != null && character.characterObject != null)
        {
            UpdateDialoguePosition(dialogueBox, character.characterObject, character.offset);
        }

        // 添加调试日志
        Debug.Log($"显示对话框: {dialogueBox.name}, 文本: {text}, CanvasGroup: {(canvasGroup ? canvasGroup.alpha.ToString() : "无")}");
    }

    // 打字效果协程
    private System.Collections.IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";
        foreach (char letter in fullText.ToCharArray())
        {
            textComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingInProgress = false;
        OnTypingComplete();
    }

    // 完成打字过程
    private void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (activeText != null)
        {
            string fullText = activeText.text;
            activeText.text = fullText;
        }

        typingInProgress = false;
        OnTypingComplete();
    }

    // 打字效果完成后的处理
    private void OnTypingComplete()
    {
        dialogueReady = true;

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

    // 更新对话框位置
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
            if (character.dialogueBox != null && character.dialogueBox.activeSelf && character.characterObject != null)
            {
                UpdateDialoguePosition(character.dialogueBox, character.characterObject, character.offset);
            }
        }
    }

    // 隐藏所有对话框
    private void HideAllDialogues()
    {
        if (defaultDialogueBox != null)
            defaultDialogueBox.SetActive(false);

        foreach (var character in characters)
        {
            if (character.dialogueBox != null)
                character.dialogueBox.SetActive(false);
        }
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
}