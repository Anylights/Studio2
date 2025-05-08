using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yarn.Unity;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 存档系统 - 单场景版本
/// 用于管理游戏存档
/// </summary>
public class SaveSystem : MonoBehaviour
{
    #region 单例实现
    private static SaveSystem _instance;
    public static SaveSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveSystem>();
                if (_instance == null)
                {
                    Debug.LogError("场景中找不到SaveSystem实例！");
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("组件引用")]
    [SerializeField] private MinimalDialogueRunner dialogueRunner;
    [SerializeField] private SceneContentManager sceneManager;

    [Header("存档设置")]
    [SerializeField] private string saveFileName = "game_save.json";
    [SerializeField] private bool enableDebugLog = true;

    // 用于加载存档后调用
    public event Action<SaveData> OnSaveLoaded;

    [System.Serializable]
    public class SaveData
    {
        public string currentNodeName;
        public string currentSceneName;
        public string saveDateTime;

        // 下面这些Dictionary字段无法被JsonUtility序列化
        [System.NonSerialized]
        public Dictionary<string, float> floatVariables = new Dictionary<string, float>();
        [System.NonSerialized]
        public Dictionary<string, string> stringVariables = new Dictionary<string, string>();
        [System.NonSerialized]
        public Dictionary<string, bool> boolVariables = new Dictionary<string, bool>();

        // 使用数组来序列化变量
        public string[] floatKeys;
        public float[] floatValues;
        public string[] stringKeys;
        public string[] stringValues;
        public string[] boolKeys;
        public bool[] boolValues;

        // 从Dictionary转换为数组
        public void PrepareForSerialization()
        {
            floatKeys = floatVariables.Keys.ToArray();
            floatValues = floatVariables.Values.ToArray();
            stringKeys = stringVariables.Keys.ToArray();
            stringValues = stringVariables.Values.ToArray();
            boolKeys = boolVariables.Keys.ToArray();
            boolValues = boolVariables.Values.ToArray();
        }

        // 从数组恢复Dictionary
        public void RestoreFromSerialization()
        {
            floatVariables = new Dictionary<string, float>();
            stringVariables = new Dictionary<string, string>();
            boolVariables = new Dictionary<string, bool>();

            if (floatKeys != null && floatValues != null && floatKeys.Length == floatValues.Length)
            {
                for (int i = 0; i < floatKeys.Length; i++)
                {
                    floatVariables[floatKeys[i]] = floatValues[i];
                }
            }

            if (stringKeys != null && stringValues != null && stringKeys.Length == stringValues.Length)
            {
                for (int i = 0; i < stringKeys.Length; i++)
                {
                    stringVariables[stringKeys[i]] = stringValues[i];
                }
            }

            if (boolKeys != null && boolValues != null && boolKeys.Length == boolValues.Length)
            {
                for (int i = 0; i < boolKeys.Length; i++)
                {
                    boolVariables[boolKeys[i]] = boolValues[i];
                }
            }
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 查找对话运行器
        if (dialogueRunner == null)
        {
            dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
        }

        // 查找场景管理器
        if (sceneManager == null)
        {
            sceneManager = FindObjectOfType<SceneContentManager>();
        }

        if (dialogueRunner == null || sceneManager == null)
        {
            Debug.LogError("无法找到关键组件，保存系统不可用！");
        }
    }

    // 保存游戏
    public void SaveGame()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Error("无法保存游戏：对话运行器或变量存储不可用");
            return;
        }

        // 获取当前场景名称
        string currentSceneName = sceneManager?.GetCurrentSceneName();
        if (string.IsNullOrEmpty(currentSceneName))
        {
            Error("无法保存游戏：当前场景名称不可用");
            return;
        }

        SaveData saveData = new SaveData();

        // 保存当前节点
        saveData.currentNodeName = dialogueRunner.CurrentNodeName;

        // 保存当前场景
        saveData.currentSceneName = currentSceneName;

        // 保存时间
        saveData.saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 保存所有变量
        var (floats, strings, bools) = dialogueRunner.VariableStorage.GetAllVariables();
        saveData.floatVariables = floats;
        saveData.stringVariables = strings;
        saveData.boolVariables = bools;

        // 准备序列化
        saveData.PrepareForSerialization();

        // 将保存数据转换为JSON并写入文件
        string json = JsonUtility.ToJson(saveData, true);
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        try
        {
            File.WriteAllText(path, json);
            Log($"游戏已保存：{path}");
        }
        catch (Exception e)
        {
            Error($"保存游戏失败：{e.Message}");
        }
    }

    // 加载游戏
    public void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(path))
        {
            Log("没有找到存档，无法加载");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // 从序列化恢复Dictionary
            saveData.RestoreFromSerialization();

            Log($"正在加载存档: 场景={saveData.currentSceneName}, 节点={saveData.currentNodeName}");

            // 触发存档加载事件，让GameManager处理场景切换和对话启动
            OnSaveLoaded?.Invoke(saveData);
        }
        catch (Exception e)
        {
            Error($"加载游戏失败：{e.Message}");
        }
    }

    // 获取存档数据但不应用
    public SaveData GetSaveData()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(path))
        {
            Log("没有找到存档");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            saveData.RestoreFromSerialization();
            return saveData;
        }
        catch (Exception e)
        {
            Error($"读取存档数据失败：{e.Message}");
            return null;
        }
    }

    // 应用存档数据到对话系统
    public void ApplyVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools)
    {
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.SetAllVariables(floats, strings, bools);
        }
    }

    // 重置游戏
    public void ResetGame()
    {
        // 删除存档
        DeleteSaveFile();

        // 清除所有变量
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.Clear();
        }
    }

    // 检查存档是否存在
    public bool DoesSaveExist()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        return File.Exists(path);
    }

    // 删除存档文件
    private void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Log($"存档已删除: {path}");
            }
            catch (Exception e)
            {
                Error($"删除存档失败：{e.Message}");
            }
        }
    }

    #region 日志辅助方法

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SaveSystem] {message}");
        }
    }

    private void Error(string message)
    {
        Debug.LogError($"[SaveSystem] {message}");
    }

    #endregion

    [YarnCommand("save_game")]
    public static void SaveGameCommand()
    {
        if (Instance != null)
        {
            Instance.SaveGame();
        }
        else
        {
            Debug.LogError("无法保存游戏：SaveSystem实例不可用");
        }
    }

    [YarnCommand("reset_game")]
    public static void ResetGameCommand()
    {
        if (Instance != null)
        {
            Instance.ResetGame();
        }
        else
        {
            Debug.LogError("无法重置游戏：SaveSystem实例不可用");
        }
    }
}
