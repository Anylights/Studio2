using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Unity;
public class DialogueSupportComponent : MonoBehaviour
{
    MinimalDialogueRunner runner;

    // 用于存储已注册的YarnCommand
    private Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

    // 添加一个标志，用于跟踪对话是否已经开始过
    private bool hasStartedDialogue = false;

    void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();

        // 自动注册所有带有YarnCommand属性的方法
        RegisterYarnCommands();
    }

    void Update()
    {
        // 检查 Arduino 控制器实例是否存在
        // 以及 runner 是否已经初始化
        if (runner == null || ArduinoController.Instance == null)
        {
            // 如果 runner 还没找到，尝试再次查找
            if (runner == null) runner = FindObjectOfType<MinimalDialogueRunner>();
            // 如果任一实例仍未找到，则返回
            if (runner == null || ArduinoController.Instance == null) return;
        }

        // 修改：使用 Arduino 按钮开始对话，但只能使用一次
        // 检查对话是否未运行，是否未开始过对话，并且按下了红色或绿色按钮
        if (!runner.isRunning && !hasStartedDialogue && (ArduinoController.Instance.RedButtonDown || ArduinoController.Instance.GreenButtonDown))
        {
            Debug.Log("Arduino button pressed, starting dialogue for the first time.");
            runner.StartDialogue(); // 默认启动 "Start" 节点
            hasStartedDialogue = true; // 标记对话已经开始过
        }
    }

    public void HandleCommand(string[] commandText)
    {
        string commandName = commandText[0];
        Debug.Log($"Received a command: {commandName}");

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
                Debug.Log($"Successfully executed command: {commandName}");

                // 特殊处理：如果是play_timeline命令，不在这里继续对话
                if (commandName != "play_timeline")
                {
                    // 在命令执行完毕后继续对话
                    runner.Continue();
                }
                else
                {
                    Debug.Log("Timeline命令执行中，不立即继续对话...");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing command {commandName}: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"No handler found for command: {commandName}");
        }
    }

    // 注册所有标记了YarnCommand的方法
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

                    Debug.Log($"Found YarnCommand: {commandName} in {type.Name}");

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

    public void LogNodeStarted(string node) { Debug.Log($"entered node {node}"); }
    public void LogNodeEnded(string node) { Debug.Log($"exited node {node}"); }
    public void LogDialogueEnded() { Debug.Log("Dialogue has finished"); }
}