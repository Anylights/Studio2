using UnityEngine;
using Yarn.Unity;
using System;

/// <summary>
/// 时间相关的Yarn命令集合
/// 提供获取当前系统时间并设置为Yarn变量的功能
/// </summary>
public class TimeCommands : MonoBehaviour
{
    [Header("调试选项")]
    [SerializeField] private bool enableDebugLogs = true;

    // Yarn命令：获取当前完整日期时间字符串
    [YarnCommand("get_datetime")]
    public static void GetCurrentDateTime(string variableName, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogError("变量名不能为空");
            return;
        }

        // 确保变量名以$开头
        if (!variableName.StartsWith("$"))
        {
            variableName = "$" + variableName;
        }

        try
        {
            // 获取当前时间并格式化
            string currentDateTime = DateTime.Now.ToString(format);

            // 找到MinimalDialogueRunner并设置变量
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, currentDateTime);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置时间变量 {variableName} = {currentDateTime}");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置时间变量失败: {e.Message}");
        }
    }

    // Yarn命令：获取当前时间（仅时间部分）
    [YarnCommand("get_time")]
    public static void GetCurrentTime(string variableName, string format = "HH:mm:ss")
    {
        GetCurrentDateTime(variableName, format);
    }

    // Yarn命令：获取当前日期（仅日期部分）
    [YarnCommand("get_date")]
    public static void GetCurrentDate(string variableName, string format = "yyyy-MM-dd")
    {
        GetCurrentDateTime(variableName, format);
    }

    // Yarn命令：获取Unix时间戳
    [YarnCommand("get_timestamp")]
    public static void GetUnixTimestamp(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogError("变量名不能为空");
            return;
        }

        // 确保变量名以$开头
        if (!variableName.StartsWith("$"))
        {
            variableName = "$" + variableName;
        }

        try
        {
            // 获取Unix时间戳
            long timestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

            // 找到MinimalDialogueRunner并设置变量（作为浮点数）
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, (float)timestamp);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置时间戳变量 {variableName} = {timestamp}");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置时间戳变量失败: {e.Message}");
        }
    }

    // Yarn命令：获取小时数（0-23）
    [YarnCommand("get_hour")]
    public static void GetCurrentHour(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogError("变量名不能为空");
            return;
        }

        // 确保变量名以$开头
        if (!variableName.StartsWith("$"))
        {
            variableName = "$" + variableName;
        }

        try
        {
            // 获取当前小时
            float hour = DateTime.Now.Hour;

            // 找到MinimalDialogueRunner并设置变量
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, hour);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置小时变量 {variableName} = {hour}");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置小时变量失败: {e.Message}");
        }
    }

    // Yarn命令：获取星期几（0=周日，1=周一，...，6=周六）
    [YarnCommand("get_weekday")]
    public static void GetCurrentWeekday(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogError("变量名不能为空");
            return;
        }

        // 确保变量名以$开头
        if (!variableName.StartsWith("$"))
        {
            variableName = "$" + variableName;
        }

        try
        {
            // 获取当前星期几
            float weekday = (float)DateTime.Now.DayOfWeek;

            // 找到MinimalDialogueRunner并设置变量
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, weekday);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置星期变量 {variableName} = {weekday} ({DateTime.Now.DayOfWeek})");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置星期变量失败: {e.Message}");
        }
    }

    // 单例模式支持（用于访问实例变量）
    private static TimeCommands Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("场景中存在多个TimeCommands实例，销毁重复的实例");
            }
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}