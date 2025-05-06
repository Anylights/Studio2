using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Unity;

/// <summary>
/// 对话支持组件 - 负责处理来自Yarn脚本的命令
/// </summary>
public class DialogueSupportComponent : MonoBehaviour
{
    [SerializeField] private MinimalDialogueRunner runner;

    // 用于存储已注册的YarnCommand
    private Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

    void Start()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<MinimalDialogueRunner>();
            if (runner == null)
            {
                Debug.LogError("无法找到MinimalDialogueRunner，对话支持组件无法工作");
                return;
            }
        }

        // 自动注册所有带有YarnCommand属性的方法
        RegisterYarnCommands();
    }

    /// <summary>
    /// 处理来自Yarn脚本的命令
    /// </summary>
    public void HandleCommand(string[] commandText)
    {
        if (commandText == null || commandText.Length == 0)
        {
            Debug.LogWarning("收到空命令");
            return;
        }

        string commandName = commandText[0];
        Debug.Log($"收到命令: {commandName}");

        // 检查是否有对应的命令处理器
        if (commandHandlers.TryGetValue(commandName, out var handler))
        {
            try
            {
                // 获取参数（如果有）
                object[] parameters = null;
                if (commandText.Length > 1)
                {
                    parameters = new object[commandText.Length - 1];
                    Array.Copy(commandText, 1, parameters, 0, commandText.Length - 1);
                }
                else
                {
                    parameters = new object[0];
                }

                // 调用命令处理器
                handler.DynamicInvoke(parameters);
                Debug.Log($"命令执行成功: {commandName}");

                // 特殊处理：如果是play_timeline命令，不在这里继续对话
                if (commandName != "play_timeline")
                {
                    // 在命令执行完毕后继续对话，但先检查对话是否在运行
                    if (runner != null && runner.isRunning)
                    {
                        runner.Continue();
                    }
                    else
                    {
                        Debug.Log("对话已不在运行状态，无需继续");
                    }
                }
                else
                {
                    Debug.Log("Timeline命令执行中，不立即继续对话...");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"执行命令 {commandName} 时出错: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"未找到命令处理器: {commandName}");
        }
    }

    /// <summary>
    /// 注册所有标记了YarnCommand的方法
    /// </summary>
    private void RegisterYarnCommands()
    {
        // 查找所有MonoBehaviour
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();

        foreach (var script in scripts)
        {
            Type type = script.GetType();

            // 获取所有公共方法
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var method in methods)
            {
                // 查找带有YarnCommand特性的方法
                object[] attributes = method.GetCustomAttributes(typeof(YarnCommandAttribute), true);

                if (attributes.Length > 0)
                {
                    YarnCommandAttribute commandAttribute = attributes[0] as YarnCommandAttribute;
                    string commandName = commandAttribute.Name;

                    Debug.Log($"发现YarnCommand: {commandName} 在 {type.Name} 中");

                    // 创建委托
                    Delegate handler;
                    if (method.IsStatic)
                    {
                        // 静态方法
                        Type delegateType = Expression.GetDelegateType(
                            method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).ToArray());
                        handler = Delegate.CreateDelegate(delegateType, method);
                    }
                    else
                    {
                        // 实例方法
                        Type delegateType = Expression.GetDelegateType(
                            method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).ToArray());
                        handler = Delegate.CreateDelegate(delegateType, script, method);
                    }

                    // 添加到命令处理器字典
                    commandHandlers[commandName] = handler;
                }
            }
        }
    }

    // 日志辅助方法
    public void LogNodeStarted(string node) { Debug.Log($"进入节点: {node}"); }
    public void LogNodeEnded(string node) { Debug.Log($"离开节点: {node}"); }
    public void LogDialogueEnded() { Debug.Log("对话已结束"); }
}