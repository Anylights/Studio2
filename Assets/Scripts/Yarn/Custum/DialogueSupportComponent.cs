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

    void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();

        // 自动注册所有带有YarnCommand属性的方法
        RegisterYarnCommands();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!runner.isRunning)
            {
                runner.StartDialogue();
            }
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