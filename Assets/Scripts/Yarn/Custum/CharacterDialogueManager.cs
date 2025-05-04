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

    [Header("对话设置")]
    [SerializeField] private GameObject defaultDialogueBox; // 默认对话框
    [SerializeField] private List<CharacterDialogueInfo> characters = new List<CharacterDialogueInfo>();

    [Header("对话效果")]
    [SerializeField] private bool autoAdvanceDialogue = false; // 是否自动推进对话
    [SerializeField] private float dialogueDuration = 3f; // 自动关闭对话的时间

    // 当前活跃的对话框
    private GameObject activeDialogue = null;
    private TextMeshProUGUI activeText = null;
    private TypewriterByCharacter activeTypewriter = null;  // TextAnimator的打字机组件引用
    private Coroutine dismissCoroutine = null;
    private bool dialogueReady = false; // 对话是否准备好被推进

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

        // 确保文本为空并清除网格
        activeText.text = "";
        activeText.ClearMesh(true);

        // 查找TypewriterByCharacter组件
        activeTypewriter = dialogueBox.GetComponentInChildren<TypewriterByCharacter>();
        if (activeTypewriter == null)
        {
            Debug.LogWarning($"对话框 {dialogueBox.name} 上没有找到 TypewriterByCharacter 组件，将直接显示文本。");
            activeText.text = text;
            dialogueReady = true;  // 直接标记为准备好
        }
        else
        {
            // 使用TextAnimator显示文本
            activeTypewriter.ShowText(text);
        }

        // 如果是角色对话框并且设置了跟随，立即更新位置
        if (character != null && character.characterObject != null && character.followCharacter)
        {
            UpdateDialoguePosition(dialogueBox, character.characterObject, character.offset);
        }
    }

    // 当TextAnimator完成文本动画时调用
    private void OnTypewriterComplete()
    {
        // 文本完全显示后，标记对话准备好继续
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

    // 隐藏所有对话框
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

        if (defaultDialogueBox != null)
        {
            var defaultText = defaultDialogueBox.GetComponentInChildren<TextMeshProUGUI>();
            if (defaultText != null)
            {
                defaultText.text = "";
                defaultText.ClearMesh(true);
            }
            defaultDialogueBox.SetActive(false);
        }

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
                character.dialogueBox.SetActive(false);
            }
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