using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 存档系统 - 单场景版本
/// 用于管理游戏存档和场景内容切换
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

    [Header("对话运行器引用")]
    [SerializeField] private MinimalDialogueRunner dialogueRunner;

    [Header("存档设置")]
    [SerializeField] private string saveFileName = "game_save.json";
    [SerializeField] private bool enableDebugLog = true;

    // 场景与节点关系映射（场景名称->节点名称集合）
    [SerializeField] private SceneNodeMapping[] sceneNodeMappings;

    // 重要数据
    private string currentSceneName;

    [System.Serializable]
    public class SceneNodeMapping
    {
        public string sceneName;
        public string[] nodeNames;
    }

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

        // 确保能访问当前场景名称
        if (SceneContentManager.Instance != null)
        {
            currentSceneName = SceneContentManager.Instance.GetCurrentSceneName();
        }
        else
        {
            Debug.LogError("无法找到SceneContentManager实例！");
        }
    }

    private void Start()
    {
        // 查找对话运行器
        if (dialogueRunner == null)
        {
            dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner == null)
            {
                Debug.LogError("无法找到MinimalDialogueRunner，保存系统不可用！");
                return;
            }
        }

        // 订阅SceneContentManager的事件
        if (SceneContentManager.Instance != null)
        {
            SceneContentManager.Instance.OnSceneChanged += OnSceneChanged;
            SceneContentManager.Instance.OnGameStart += OnGameStart;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (SceneContentManager.Instance != null)
        {
            SceneContentManager.Instance.OnSceneChanged -= OnSceneChanged;
            SceneContentManager.Instance.OnGameStart -= OnGameStart;
        }
    }

    // 处理场景变化事件
    private void OnSceneChanged(string previousScene, string newScene)
    {
        currentSceneName = newScene;
        Log($"场景已变更: {previousScene} -> {newScene}");
    }

    // 处理游戏开始事件
    private void OnGameStart()
    {
        StartGame();
    }

    // 启动游戏
    public void StartGame()
    {
        Log("开始游戏");

        // 检查是否有存档
        if (DoesSaveExist())
        {
            // 加载存档
            LoadGame();
        }
        else
        {
            // 新游戏
            NewGame();
        }
    }

    // 创建新游戏
    private void NewGame()
    {
        Log("开始新游戏");

        // 清空变量存储
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.Clear();
        }

        // 从lab节点开始对话
        if (SceneContentManager.Instance != null)
        {
            ChangeSceneAndStartDialogue("Level1_lab", "lab");
        }
        else
        {
            Error("无法创建新游戏：场景管理器不可用");
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
            Log("没有找到存档，开始新游戏");
            NewGame();
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // 从序列化恢复Dictionary
            saveData.RestoreFromSerialization();

            Log($"正在加载存档: 场景={saveData.currentSceneName}, 节点={saveData.currentNodeName}");

            // 切换到保存的场景并加载对话
            ChangeSceneAndContinueDialogue(saveData);
        }
        catch (Exception e)
        {
            Error($"加载游戏失败：{e.Message}");
            NewGame();
        }
    }

    // 切换场景并继续对话
    private void ChangeSceneAndContinueDialogue(SaveData saveData)
    {
        if (saveData == null)
        {
            Error("无法继续对话：存档数据为空");
            return;
        }

        if (SceneContentManager.Instance == null)
        {
            Error("无法切换场景：场景管理器不可用");
            return;
        }

        // 如果已经在正确的场景中
        if (currentSceneName == saveData.currentSceneName)
        {
            Log($"已在目标场景 {saveData.currentSceneName} 中，直接继续对话");
            ContinueDialogueFromSaveData(saveData);
            return;
        }

        // 检查目标场景是否存在
        if (!SceneContentManager.Instance.SceneExists(saveData.currentSceneName))
        {
            Error($"无法切换到场景 {saveData.currentSceneName}：场景不存在");
            return;
        }

        // 停止当前对话
        if (dialogueRunner != null && dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 保存需要加载的节点和变量
        string targetNodeName = saveData.currentNodeName;
        var variables = saveData.floatVariables;
        var stringVars = saveData.stringVariables;
        var boolVars = saveData.boolVariables;

        // 切换场景
        Log($"切换到场景 {saveData.currentSceneName}");
        SceneContentManager.Instance.OnSceneChanged += (prev, next) =>
        {
            if (next == saveData.currentSceneName)
            {
                StartCoroutine(DelayedDialogueContinue(targetNodeName, variables, stringVars, boolVars));
            }
        };

        SceneContentManager.Instance.ChangeScene(saveData.currentSceneName);
    }

    // 延迟继续对话
    private IEnumerator DelayedDialogueContinue(string nodeName, Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools)
    {
        yield return new WaitForSeconds(0.5f);

        // 重新查找对话运行器，因为场景切换可能会导致引用失效
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        if (dialogueRunner == null)
        {
            Error("场景切换后找不到对话运行器");
            yield break;
        }

        // 设置变量
        if (dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.SetAllVariables(floats, strings, bools);
        }

        // 确保对话不在运行
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 开始对话
        Log($"开始对话节点: {nodeName}");
        dialogueRunner.StartDialogue(nodeName);
    }

    // 直接在当前场景继续对话
    private void ContinueDialogueFromSaveData(SaveData saveData)
    {
        if (dialogueRunner == null)
        {
            Error("无法继续对话：对话运行器不可用");
            return;
        }

        // 停止当前对话
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 设置变量
        if (dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.SetAllVariables(
                saveData.floatVariables,
                saveData.stringVariables,
                saveData.boolVariables
            );
        }

        // 开始新对话
        Log($"在当前场景继续对话: {saveData.currentNodeName}");
        dialogueRunner.StartDialogue(saveData.currentNodeName);
    }

    // 切换场景并开始特定对话
    private void ChangeSceneAndStartDialogue(string sceneName, string nodeName)
    {
        if (SceneContentManager.Instance == null)
        {
            Error("无法切换场景：场景管理器不可用");
            return;
        }

        if (!SceneContentManager.Instance.SceneExists(sceneName))
        {
            Error($"无法切换到场景 {sceneName}：场景不存在");
            return;
        }

        // 停止当前对话
        if (dialogueRunner != null && dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 切换场景
        Log($"切换到场景 {sceneName} 并准备对话节点 {nodeName}");
        SceneContentManager.Instance.OnSceneChanged += (prev, next) =>
        {
            if (next == sceneName)
            {
                StartCoroutine(DelayedDialogueStart(nodeName));
            }
        };

        SceneContentManager.Instance.ChangeScene(sceneName);
    }

    // 延迟开始对话
    private IEnumerator DelayedDialogueStart(string nodeName)
    {
        yield return new WaitForSeconds(0.5f);

        // 重新查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        if (dialogueRunner == null)
        {
            Error("场景切换后找不到对话运行器");
            yield break;
        }

        // 确保对话不在运行
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 开始对话
        Log($"开始对话节点: {nodeName}");
        dialogueRunner.StartDialogue(nodeName);
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

        // 切换到初始场景
        if (SceneContentManager.Instance != null)
        {
            SceneContentManager.Instance.ChangeScene(SceneContentManager.Instance.GetCurrentSceneName());
        }
    }

    // 退出游戏
    public void QuitGame()
    {
        if (SceneContentManager.Instance != null)
        {
            SceneContentManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

    // 获取节点对应的场景
    public string GetSceneForNode(string nodeName)
    {
        foreach (var mapping in sceneNodeMappings)
        {
            foreach (var node in mapping.nodeNames)
            {
                if (node == nodeName)
                {
                    return mapping.sceneName;
                }
            }
        }

        return currentSceneName;
    }

    #region YarnCommands

    // 保存游戏
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

    // 重置游戏
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

    // 退出游戏
    [YarnCommand("quit_game")]
    public static void QuitGameCommand()
    {
        if (Instance != null)
        {
            Instance.QuitGame();
        }
        else
        {
            Debug.LogError("无法退出游戏：SaveSystem实例不可用");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // 开始游戏
    [YarnCommand("start_game")]
    public static void StartGameCommand()
    {
        if (Instance != null)
        {
            // 检查是否有存档
            if (Instance.DoesSaveExist())
            {
                // 如果有存档，加载存档
                Debug.Log("[StartGameCommand] 发现存档，加载游戏");
                Instance.LoadGame();
            }
            else
            {
                // 如果没有存档，开始新游戏
                Debug.Log("[StartGameCommand] 无存档，开始新游戏");
                Instance.NewGame();
            }
        }
        else
        {
            Debug.LogError("无法开始游戏：SaveSystem实例不可用");
        }
    }

    // 切换场景
    [YarnCommand("change_scene")]
    public static void ChangeSceneCommand(string sceneName, string nodeName)
    {
        if (Instance != null)
        {
            Instance.ChangeSceneAndStartDialogue(sceneName, nodeName);
        }
        else
        {
            Debug.LogError("无法切换场景：SaveSystem实例不可用");
        }
    }

    // 切换到下一个场景
    [YarnCommand("next_scene")]
    public static void NextSceneCommand(string nodeName)
    {
        if (Instance != null && SceneContentManager.Instance != null)
        {
            string currentSceneName = SceneContentManager.Instance.GetCurrentSceneName();
            string[] allScenes = SceneContentManager.Instance.GetAllSceneNames();

            // 查找当前场景在数组中的索引
            int currentIndex = -1;
            for (int i = 0; i < allScenes.Length; i++)
            {
                if (allScenes[i] == currentSceneName)
                {
                    currentIndex = i;
                    break;
                }
            }

            // 切换到下一个场景
            if (currentIndex >= 0 && currentIndex < allScenes.Length - 1)
            {
                string nextSceneName = allScenes[currentIndex + 1];
                ChangeSceneCommand(nextSceneName, nodeName);
            }
            else
            {
                Debug.LogError("没有下一个场景可以切换");
            }
        }
        else
        {
            Debug.LogError("无法切换场景：SaveSystem或SceneContentManager实例不可用");
        }
    }

    // 简化的场景切换命令
    [YarnCommand("goto_scene")]
    public static void GotoSceneCommand(string sceneName, string nodeName = "Start")
    {
        ChangeSceneCommand(sceneName, nodeName);
    }

    #endregion

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
}
