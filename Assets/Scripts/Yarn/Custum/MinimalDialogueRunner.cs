using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.Events;
using System.Linq;

public class MinimalDialogueRunner : MonoBehaviour
{
    public YarnProject project;
    public InMemoryVariableStorage VariableStorage;
    public LineProviderBehaviour LineProvider;

    public bool isRunning { get; internal set; } = false;

    // 添加当前节点属性
    public string CurrentNodeName { get; private set; } = "";

    private Yarn.Dialogue dialogue;

    public void StartDialogue(string nodeName = "Start")
    {
        if (isRunning)
        {
            Debug.LogWarning("Can't start a dialogue that is already running");
            return;
        }

        // 记录当前节点
        CurrentNodeName = nodeName;

        isRunning = true;
        dialogue.SetNode(nodeName);
        dialogue.Continue();
    }
    public void StopDialogue()
    {
        dialogue.Stop();
        isRunning = false;
    }
    private void HandleOptions(Yarn.OptionSet options)
    {
        DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++)
        {
            var line = LineProvider.GetLocalizedLine(options.Options[i].Line);
            var text = Yarn.Dialogue.ExpandSubstitutions(line.RawText, options.Options[i].Line.Substitutions);
            dialogue.LanguageCode = LineProvider.LocaleCode;
            line.Text = dialogue.ParseMarkup(text);

            optionSet[i] = new DialogueOption
            {
                TextID = options.Options[i].Line.ID,
                DialogueOptionID = options.Options[i].ID,
                Line = line,
                IsAvailable = options.Options[i].IsAvailable,
            };
        }
        OptionsNeedPresentation?.Invoke(optionSet);
    }
    private void HandleCommand(Yarn.Command command)
    {
        var elements = Yarn.Unity.DialogueRunner.SplitCommandText(command.Text).ToArray();

        if (elements[0] == "wait")
        {
            if (elements.Length < 2)
            {
                Debug.LogWarning("Asked to wait but given no duration!");
                return;
            }
            float duration = float.Parse(elements[1]);
            if (duration > 0)
            {
                IEnumerator Wait(float time)
                {
                    isRunning = false;
                    yield return new WaitForSeconds(time);
                    isRunning = true;
                    Continue();
                }
                StartCoroutine(Wait(duration));
            }
        }
        else
        {
            CommandNeedsHandling?.Invoke(elements);
        }
    }
    private void HandleLine(Yarn.Line line)
    {
        var finalLine = LineProvider.GetLocalizedLine(line);
        var text = Yarn.Dialogue.ExpandSubstitutions(finalLine.RawText, line.Substitutions);
        dialogue.LanguageCode = LineProvider.LocaleCode;
        finalLine.Text = dialogue.ParseMarkup(text);

        LineNeedsPresentation?.Invoke(finalLine);
    }
    private void HandleNodeStarted(string nodeName)
    {
        // 记录当前节点
        CurrentNodeName = nodeName;

        NodeStarted?.Invoke(nodeName);
    }
    private void HandleNodeEnded(string nodeName)
    {
        NodeEnded?.Invoke(nodeName);
    }
    private void HandleDialogueComplete()
    {
        isRunning = false;
        DialogueComplete?.Invoke();
    }
    public void Continue()
    {
        if (!isRunning)
        {
            Debug.LogWarning("Can't continue dialogue when we aren't currently running any");
            return;
        }

        dialogue.Continue();
    }
    public void SetSelectedOption(int optionIndex)
    {
        if (!isRunning)
        {
            Debug.LogWarning("Can't select an option when not currently running dialogue");
            return;
        }

        // 确保dialogue不为null
        if (dialogue == null)
        {
            Debug.LogError("对话系统已被销毁，无法继续");
            isRunning = false;
            return;
        }

        dialogue.SetSelectedOption(optionIndex);

        // 确保对话仍然在运行状态后再继续
        if (isRunning)
        {
            dialogue.Continue();
        }
        else
        {
            Debug.Log("对话已不在运行状态，无法继续");
        }
    }

    private void PrepareForLines(IEnumerable<string> lineIDs) { LineProvider.PrepareForLines(lineIDs); }
    public bool NodeExists(string nodeName) => dialogue.NodeExists(nodeName);

    [SerializeField] private CharacterDialogueManager characterDialogueManager;

    public UnityEvent<DialogueOption[]> OptionsNeedPresentation;
    public UnityEvent<string[]> CommandNeedsHandling;
    public UnityEvent<LocalizedLine> LineNeedsPresentation;
    public UnityEvent<string> NodeStarted;
    public UnityEvent<string> NodeEnded;
    public UnityEvent DialogueComplete;

    void Awake()
    {
        if (VariableStorage == null)
        {
            VariableStorage = gameObject.AddComponent<InMemoryVariableStorage>();
        }
        dialogue = CreateDialogueInstance();
        dialogue.SetProgram(project.Program);

        if (LineProvider == null)
        {
            LineProvider = gameObject.AddComponent<TextLineProvider>();
        }
        LineProvider.YarnProject = project;

        // 查找或添加CharacterDialogueManager
        if (characterDialogueManager == null)
        {
            characterDialogueManager = FindObjectOfType<CharacterDialogueManager>();
            if (characterDialogueManager == null && GetComponent<CharacterDialogueManager>() == null)
            {
                characterDialogueManager = gameObject.AddComponent<CharacterDialogueManager>();
            }
        }
    }

    private Yarn.Dialogue CreateDialogueInstance()
    {
        var dialogue = new Yarn.Dialogue(VariableStorage)
        {
            LogDebugMessage = delegate (string message)
            {
                Debug.Log(message);
            },
            LogErrorMessage = delegate (string message)
            {
                Debug.LogError(message);
            },

            LineHandler = HandleLine,
            CommandHandler = HandleCommand,
            OptionsHandler = HandleOptions,
            NodeStartHandler = HandleNodeStarted,
            NodeCompleteHandler = HandleNodeEnded,
            DialogueCompleteHandler = HandleDialogueComplete,
            PrepareForLinesHandler = PrepareForLines
        };
        return dialogue;
    }

    // 添加一个YarnCommand，用于从脚本中跳转到指定节点
    [YarnCommand("jump_to_node")]
    public void JumpToNode(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning("无法跳转到空节点名");
            return;
        }

        if (!NodeExists(nodeName))
        {
            Debug.LogWarning($"节点 '{nodeName}' 不存在");
            return;
        }

        // 如果当前正在运行对话，先停止
        if (isRunning)
        {
            StopDialogue();
        }

        // 跳转到指定节点
        StartDialogue(nodeName);
    }
}