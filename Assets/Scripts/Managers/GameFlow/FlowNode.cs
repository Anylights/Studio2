using UnityEngine;
using UnityEngine.Events;

public abstract class FlowNode : ScriptableObject
{
    [Header("基础设置")]
    public string nodeName;
    [TextArea] public string description;

    [Header("节点事件")]
    public UnityEvent onNodeStart;
    public UnityEvent onNodeEnd;

    public abstract void Execute(System.Action onComplete);

    public virtual FlowNode GetNextNode()
    {
        return null; // 默认无后续节点
    }
}