using UnityEngine;
using UnityEngine.Events;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [Header("当前流程节点")]
    public FlowNode currentNode;

    [Header("调试模式")]
    public bool debugMode = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void StartFlow(FlowNode startNode)
    {
        currentNode = startNode;
        ExecuteCurrentNode();
    }

    public void ExecuteCurrentNode()
    {
        if (currentNode == null) return;

        if (debugMode) Debug.Log($"执行节点: {currentNode.name}");

        currentNode.Execute(() =>
        {
            currentNode = currentNode.GetNextNode();
            ExecuteCurrentNode();
        });
    }
}