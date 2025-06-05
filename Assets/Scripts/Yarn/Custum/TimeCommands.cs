using UnityEngine;
using Yarn.Unity;
using System;
using System.Collections;

/// <summary>
/// 时间相关的Yarn命令集合
/// 提供获取当前系统时间并设置为Yarn变量的功能
/// </summary>
public class TimeCommands : MonoBehaviour
{
    [Header("调试选项")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("倒计时设置")]
    [SerializeField] private bool showCountdownInConsole = true;

    // 倒计时相关变量
    private static bool isCountdownActive = false;
    private static DateTime countdownStartTime;
    private static float countdownDuration; // 秒
    private static string countdownTargetNode = "";
    private static Coroutine countdownCoroutine;

    // Yarn命令：开始倒计时
    [YarnCommand("start_countdown")]
    public static void StartCountdown(float durationInSeconds, string targetNodeName = "")
    {
        if (Instance == null)
        {
            Debug.LogError("TimeCommands实例未找到");
            return;
        }

        if (isCountdownActive)
        {
            Debug.LogWarning("倒计时已在运行中，将停止当前倒计时并开始新的");
            StopCountdown();
        }

        countdownStartTime = DateTime.Now;
        countdownDuration = durationInSeconds;
        countdownTargetNode = targetNodeName;
        isCountdownActive = true;

        // 启动倒计时协程
        countdownCoroutine = Instance.StartCoroutine(Instance.CountdownCoroutine());

        if (Instance.enableDebugLogs)
        {
            Debug.Log($"开始倒计时：{durationInSeconds}秒，结束后跳转到节点：{targetNodeName}");
        }
    }

    // Yarn命令：停止倒计时
    [YarnCommand("stop_countdown")]
    public static void StopCountdown()
    {
        if (!isCountdownActive)
        {
            Debug.LogWarning("没有活跃的倒计时可以停止");
            return;
        }

        isCountdownActive = false;
        countdownTargetNode = "";

        if (countdownCoroutine != null && Instance != null)
        {
            Instance.StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (Instance?.enableDebugLogs == true)
        {
            Debug.Log("倒计时已停止");
        }
    }

    // Yarn命令：获取倒计时剩余时间
    [YarnCommand("get_countdown_remaining")]
    public static void GetCountdownRemaining(string variableName)
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

        float remainingTime = 0f;

        if (isCountdownActive)
        {
            TimeSpan elapsed = DateTime.Now - countdownStartTime;
            remainingTime = Mathf.Max(0f, countdownDuration - (float)elapsed.TotalSeconds);
        }

        try
        {
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, remainingTime);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置倒计时剩余时间变量 {variableName} = {remainingTime:F1}秒");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置倒计时剩余时间变量失败: {e.Message}");
        }
    }

    // Yarn命令：检查倒计时是否活跃
    [YarnCommand("is_countdown_active")]
    public static void IsCountdownActive(string variableName)
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
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner?.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue(variableName, isCountdownActive);

                if (Instance?.enableDebugLogs == true)
                {
                    Debug.Log($"已设置倒计时活跃状态变量 {variableName} = {isCountdownActive}");
                }
            }
            else
            {
                Debug.LogError("无法找到MinimalDialogueRunner或VariableStorage");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"设置倒计时活跃状态变量失败: {e.Message}");
        }
    }

    // 倒计时协程
    private IEnumerator CountdownCoroutine()
    {
        while (isCountdownActive)
        {
            TimeSpan elapsed = DateTime.Now - countdownStartTime;
            float remainingTime = countdownDuration - (float)elapsed.TotalSeconds;

            if (showCountdownInConsole && enableDebugLogs)
            {
                Debug.Log($"倒计时剩余：{remainingTime:F1}秒");
            }

            if (remainingTime <= 0f)
            {
                // 倒计时结束
                OnCountdownComplete();
                yield break;
            }

            yield return new WaitForSeconds(1f); // 每秒检查一次
        }
    }

    // 倒计时完成处理
    private void OnCountdownComplete()
    {
        if (enableDebugLogs)
        {
            Debug.Log("倒计时结束！");
        }

        isCountdownActive = false;
        countdownCoroutine = null;

        // 如果指定了目标节点，跳转到该节点
        if (!string.IsNullOrEmpty(countdownTargetNode))
        {
            var dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner != null)
            {
                if (dialogueRunner.NodeExists(countdownTargetNode))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"倒计时结束，跳转到节点：{countdownTargetNode}");
                    }

                    // 使用jump_to_node命令跳转
                    dialogueRunner.JumpToNode(countdownTargetNode);
                }
                else
                {
                    Debug.LogError($"倒计时结束但目标节点不存在：{countdownTargetNode}");
                }
            }
            else
            {
                Debug.LogError("倒计时结束但无法找到MinimalDialogueRunner");
            }
        }

        // 清理变量
        countdownTargetNode = "";
    }

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
            // 停止倒计时
            if (isCountdownActive)
            {
                StopCountdown();
            }
            Instance = null;
        }
    }
}