using UnityEngine;
using Yarn.Unity;

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Game/Nodes/Dialogue")]
public class DialogueNode : FlowNode
{
    [Header("对话设置")]
    public DialogueRunner dialogueRunner;
    public string startNode = "Start";

    public override void Execute(System.Action onComplete)
    {
        onNodeStart.Invoke();

        dialogueRunner.StartDialogue(startNode);
        dialogueRunner.onDialogueComplete.AddListener(() =>
        {
            onNodeEnd.Invoke();
            onComplete?.Invoke();
        });
    }
}