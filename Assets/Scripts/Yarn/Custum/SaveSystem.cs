using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using System.IO;
using System;
using System.Linq;

public class SaveSystem : MonoBehaviour
{
    // 单例模式
    public static SaveSystem Instance { get; private set; }

    // Yarn对话运行器引用
    private MinimalDialogueRunner dialogueRunner;

    // 变量存储引用
    private InMemoryVariableStorage variableStorage;

    // 用于序列化的字典项
    [Serializable]
    private class SerializableKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    // 可序列化的字典容器
    [Serializable]
    private class SerializableDictionary<TKey, TValue>
    {
        public List<SerializableKeyValuePair<TKey, TValue>> Items = new List<SerializableKeyValuePair<TKey, TValue>>();

        public SerializableDictionary() { }

        public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
        {
            Items = dictionary.Select(kvp => new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value)).ToList();
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            return Items.ToDictionary(item => item.Key, item => item.Value);
        }
    }

    // 保存数据的类
    [Serializable]
    private class SaveData
    {
        // 存档的创建时间
        public string saveTime;

        // 当前节点名称
        public string currentNode;

        // 变量状态 - 使用可序列化的字典
        public SerializableDictionary<string, float> floatVariables = new SerializableDictionary<string, float>();
        public SerializableDictionary<string, string> stringVariables = new SerializableDictionary<string, string>();
        public SerializableDictionary<string, bool> boolVariables = new SerializableDictionary<string, bool>();
    }

    private SaveData currentSaveData = new SaveData();
    private Dictionary<string, bool> boolVariables = new Dictionary<string, bool>();
    private Dictionary<string, float> floatVariables = new Dictionary<string, float>();
    private Dictionary<string, string> stringVariables = new Dictionary<string, string>();

    // 存档文件路径
    private string saveFilePath;

    private void Awake()
    {
        // 单例模式设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 设置存档文件路径
        saveFilePath = Path.Combine(Application.persistentDataPath, "yarn_save.json");

        Debug.Log($"存档文件路径: {saveFilePath}");
    }

    private void Start()
    {
        // 查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
        if (dialogueRunner == null)
        {
            Debug.LogWarning("没有找到MinimalDialogueRunner，存档系统将无法正常工作");
            return;
        }

        // 获取变量存储组件
        variableStorage = dialogueRunner.VariableStorage as InMemoryVariableStorage;
        if (variableStorage == null)
        {
            Debug.LogWarning("没有找到InMemoryVariableStorage，存档系统将无法正常工作");
            return;
        }

        // 设置存档状态变量
        bool hasSave = File.Exists(saveFilePath);
        variableStorage.SetValue("$has_save", hasSave);

        // 自动加载存档（如果存在）
        if (hasSave)
        {
            LoadGame();
        }

        // 订阅节点完成事件，用于自动保存
        dialogueRunner.NodeEnded.AddListener(AutoSave);
    }

    // 在每个节点结束时自动保存
    private void AutoSave(string nodeName)
    {
        SaveGame();
    }

    // 保存游戏
    [YarnCommand("save_game")]
    public void SaveGame()
    {
        if (dialogueRunner == null || variableStorage == null)
        {
            Debug.LogWarning("无法保存游戏：对话运行器或变量存储为空");
            return;
        }

        // 创建新的存档数据
        currentSaveData = new SaveData
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            currentNode = ""
        };

        // 如果对话正在运行，记录当前节点
        if (dialogueRunner.isRunning)
        {
            currentSaveData.currentNode = dialogueRunner.CurrentNodeName;
            Debug.Log($"保存当前节点: {currentSaveData.currentNode}");
        }

        // 清空字典
        boolVariables.Clear();
        floatVariables.Clear();
        stringVariables.Clear();

        // 保存关键变量
        SaveBoolVariable("$has_save");
        SaveBoolVariable("$visited_save_example");
        SaveBoolVariable("$NotAgree");

        // 将字典转换为可序列化的格式
        currentSaveData.boolVariables = new SerializableDictionary<string, bool>(boolVariables);
        currentSaveData.floatVariables = new SerializableDictionary<string, float>(floatVariables);
        currentSaveData.stringVariables = new SerializableDictionary<string, string>(stringVariables);

        // 将数据转换为JSON并保存到文件
        string jsonData = JsonUtility.ToJson(currentSaveData, true);

        try
        {
            File.WriteAllText(saveFilePath, jsonData);
            // 更新存档状态变量
            variableStorage.SetValue("$has_save", true);
            Debug.Log($"游戏已保存到 {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏时出错: {e.Message}");
        }
    }

    // 辅助方法，保存布尔变量
    private void SaveBoolVariable(string name)
    {
        if (variableStorage.TryGetValue(name, out bool value))
        {
            boolVariables[name] = value;
        }
    }

    // 辅助方法，保存浮点变量
    private void SaveFloatVariable(string name)
    {
        if (variableStorage.TryGetValue(name, out float value))
        {
            floatVariables[name] = value;
        }
    }

    // 辅助方法，保存字符串变量
    private void SaveStringVariable(string name)
    {
        if (variableStorage.TryGetValue(name, out string value))
        {
            stringVariables[name] = value;
        }
    }

    // 加载游戏
    [YarnCommand("load_game")]
    public void LoadGame()
    {
        if (dialogueRunner == null || variableStorage == null)
        {
            Debug.LogWarning("无法加载游戏：对话运行器或变量存储为空");
            return;
        }

        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning($"找不到存档文件: {saveFilePath}");
            return;
        }

        try
        {
            // 从文件读取JSON数据
            string jsonData = File.ReadAllText(saveFilePath);

            // 将JSON转换为SaveData对象
            currentSaveData = JsonUtility.FromJson<SaveData>(jsonData);

            // 转换字典
            var boolDict = currentSaveData.boolVariables.ToDictionary();
            var floatDict = currentSaveData.floatVariables.ToDictionary();
            var stringDict = currentSaveData.stringVariables.ToDictionary();

            // 恢复布尔变量
            foreach (var pair in boolDict)
            {
                variableStorage.SetValue(pair.Key, pair.Value);
            }

            // 恢复浮点数变量
            foreach (var pair in floatDict)
            {
                variableStorage.SetValue(pair.Key, pair.Value);
            }

            // 恢复字符串变量
            foreach (var pair in stringDict)
            {
                variableStorage.SetValue(pair.Key, pair.Value);
            }

            Debug.Log($"游戏已从 {saveFilePath} 加载");

            // 如果有保存的节点，并且不是空，则跳转到该节点
            if (!string.IsNullOrEmpty(currentSaveData.currentNode))
            {
                if (dialogueRunner.NodeExists(currentSaveData.currentNode))
                {
                    // 如果当前正在运行对话，先停止
                    if (dialogueRunner.isRunning)
                    {
                        dialogueRunner.StopDialogue();
                    }

                    // 跳转到保存的节点
                    dialogueRunner.StartDialogue(currentSaveData.currentNode);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载游戏时出错: {e.Message}");
        }
    }

    // 重置游戏
    [YarnCommand("reset_game")]
    public void ResetGame()
    {
        if (dialogueRunner == null || variableStorage == null)
        {
            Debug.LogWarning("无法重置游戏：对话运行器或变量存储为空");
            return;
        }

        // 重置已知的变量
        variableStorage.SetValue("$has_save", false);
        variableStorage.SetValue("$visited_save_example", false);
        variableStorage.SetValue("$NotAgree", false);

        // 如果存档文件存在，则删除
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log($"存档文件已删除: {saveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"删除存档文件时出错: {e.Message}");
            }
        }

        // 重置存档数据
        currentSaveData = new SaveData();

        Debug.Log("游戏已重置");

        // 如果当前正在运行对话，先停止
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 跳转到开始节点
        dialogueRunner.StartDialogue("Start");
    }

    // 判断是否有存档
    public bool HasSaveData()
    {
        return File.Exists(saveFilePath);
    }

    // 获取存档时间
    public string GetSaveTime()
    {
        if (HasSaveData() && currentSaveData != null)
        {
            return currentSaveData.saveTime;
        }
        return "";
    }
}