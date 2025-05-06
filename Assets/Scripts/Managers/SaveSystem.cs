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
    [Header("对话运行器引用")]
    [SerializeField] private MinimalDialogueRunner dialogueRunner;

    [Header("存档设置")]
    [SerializeField] private string saveFileName = "game_save.json";

    // 场景与节点关系映射（场景名称->节点名称集合）
    [SerializeField] private SceneNodeMapping[] sceneNodeMappings;

    // 单例模式
    public static SaveSystem Instance { get; private set; }

    // 重要数据
    [SerializeField] private string currentSceneName;

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
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 记录当前场景
        currentSceneName = SceneContentManager.Instance.GetCurrentSceneName();
    }

    private void Start()
    {
        // 查找对话运行器
        if (dialogueRunner == null)
        {
            dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
            if (dialogueRunner == null)
            {
                Debug.LogError("无法找到MinimalDialogueRunner，保存系统可能无法正常工作！");
                return;
            }
        }
    }

    // 1. 开始游戏
    public void StartGame()
    {
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
        // 确保变量存储是空的
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.Clear();
        }

        // 从Start节点开始对话
        if (dialogueRunner != null)
        {
            Debug.Log("开始新游戏，切换到实验室场景");
            ChangeSceneCommand("Level1_lab", "lab");
        }
        else
        {
            Debug.LogError("无法开始新游戏：对话运行器不可用");
        }
    }

    // 2. 保存游戏
    public void SaveGame()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogError("无法保存游戏：对话运行器或变量存储不可用");
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
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏失败：{e.Message}");
        }
    }

    // 加载游戏
    public void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("没有找到存档，无法加载游戏");
            NewGame();
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // 从序列化恢复Dictionary
            saveData.RestoreFromSerialization();

            // // 加载场景（如果需要）
            // if (saveData.currentSceneName != currentSceneName)
            // {
            // 需要加载不同的场景
            StartCoroutine(LoadSceneAndContinue(saveData));
            Debug.Log("正在从存档加载场景");
            // }
            // else
            // {
            //     // 在当前场景继续
            //     ContinueFromSaveData(saveData);
            // }

        }
        catch (Exception e)
        {
            Debug.LogError($"加载游戏失败：{e.Message}");
            NewGame();
        }
    }

    // 加载场景并继续对话
    private IEnumerator LoadSceneAndContinue(SaveData saveData)
    {
        // 确保停止当前对话
        if (dialogueRunner != null)
        {
            dialogueRunner.StopDialogue();
        }

        // 检查SceneContentManager是否存在
        if (SceneContentManager.Instance == null)
        {
            Debug.LogError("SceneContentManager不存在，无法切换场景内容");
            yield break;
        }

        // 记录当前应用的场景名称
        string targetSceneName = saveData.currentSceneName;
        string currentSceneName = SceneContentManager.Instance.GetCurrentSceneName();

        Debug.Log($"[场景切换] 当前场景: {currentSceneName}, 目标场景: {targetSceneName}, 节点: {saveData.currentNodeName}");

        if (SceneContentManager.Instance.SceneExists(targetSceneName))
        {
            Debug.Log($"[场景切换] 场景 {targetSceneName} 存在，准备切换");
        }
        else
        {
            Debug.LogError($"[场景切换] 错误: 场景 {targetSceneName} 不存在!");
            // 输出所有可用场景
            string[] allScenes = SceneContentManager.Instance.GetAllSceneNames();
            Debug.Log("[场景切换] 可用场景列表:");
            foreach (var scene in allScenes)
            {
                Debug.Log($"- {scene}");
            }
            yield break;
        }

        // 使用SceneContentManager切换场景内容
        Debug.Log($"[场景切换] 正在调用SceneContentManager.ChangeScene({targetSceneName})");
        SceneContentManager.Instance.ChangeScene(targetSceneName);

        // 等待场景内容切换完成
        yield return new WaitForSeconds(1.5f);

        // 检查场景是否成功切换
        string newCurrentSceneName = SceneContentManager.Instance.GetCurrentSceneName();
        if (newCurrentSceneName == targetSceneName)
        {
            Debug.Log($"[场景切换] 成功: 当前场景已变为 {newCurrentSceneName}");
        }
        else
        {
            Debug.LogError($"[场景切换] 失败: 当前场景仍为 {newCurrentSceneName}，未能切换到 {targetSceneName}");
            // 尝试强制再次切换
            SceneContentManager.Instance.ChangeScene(targetSceneName);
            yield return new WaitForSeconds(1.5f);
        }

        // 更新当前场景名称记录
        this.currentSceneName = targetSceneName;

        // 查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        if (dialogueRunner != null)
        {
            // 继续游戏
            Debug.Log($"[场景切换] 准备在场景 {targetSceneName} 中继续对话节点: {saveData.currentNodeName}");
            ContinueFromSaveData(saveData);
        }
        else
        {
            Debug.LogError($"[场景切换] 错误: 场景切换后无法找到MinimalDialogueRunner");
        }
    }

    // 从存档数据继续游戏
    private void ContinueFromSaveData(SaveData saveData)
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogError("对话运行器或变量存储不可用，无法继续游戏");
            return;
        }

        // 加载所有变量
        dialogueRunner.VariableStorage.SetAllVariables(
            saveData.floatVariables,
            saveData.stringVariables,
            saveData.boolVariables
        );

        // 先停止当前正在运行的对话
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        // 从保存的节点开始对话
        dialogueRunner.StartDialogue(saveData.currentNodeName);
    }

    // 3. 重置游戏
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

    // 4. 退出游戏
    public void QuitGame()
    {
#if UNITY_EDITOR
        // 在编辑器中退出播放模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建后的应用中退出
        Application.Quit();
#endif
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
            }
            catch (Exception e)
            {
                Debug.LogError($"删除存档失败：{e.Message}");
            }
        }
    }

    // 当场景改变时更新当前场景名称
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

        // 如果找不到映射关系，默认返回当前场景
        return currentSceneName;
    }

    #region YarnCommands

    // 保存游戏的YarnCommand
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

    // 重置游戏的YarnCommand
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

    // 退出游戏的YarnCommand
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

    // 加载游戏的YarnCommand
    [YarnCommand("start_game")]
    public static void StartGameCommand()
    {
        if (Instance != null)
        {
            Instance.StartGame();
        }
        else
        {
            Debug.LogError("无法加载游戏：SaveSystem实例不可用");
        }
    }

    // 单场景版：切换到指定场景内容并跳转到指定节点的YarnCommand
    [YarnCommand("change_scene")]
    public static void ChangeSceneCommand(string sceneName, string nodeName)
    {
        if (Instance != null)
        {
            // 创建临时数据以便在新场景内容中使用
            SaveData tempData = new SaveData
            {
                currentNodeName = nodeName,
                currentSceneName = sceneName,
                saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 获取当前所有变量
            if (Instance.dialogueRunner != null && Instance.dialogueRunner.VariableStorage != null)
            {
                var (floats, strings, bools) = Instance.dialogueRunner.VariableStorage.GetAllVariables();
                tempData.floatVariables = floats;
                tempData.stringVariables = strings;
                tempData.boolVariables = bools;
            }

            // 开始切换场景内容
            Instance.StartCoroutine(Instance.LoadSceneAndStartDialogue(tempData));
        }
        else
        {
            Debug.LogError("无法切换场景：SaveSystem实例不可用");
        }
    }

    // 切换到下一个场景的YarnCommand
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

            // 如果找到当前场景，尝试切换到下一个场景
            if (currentIndex >= 0 && currentIndex < allScenes.Length - 1)
            {
                string nextSceneName = allScenes[currentIndex + 1];
                ChangeSceneCommand(nextSceneName, nodeName);
            }
            else
            {
                Debug.LogError($"没有下一个场景内容可以切换");
            }
        }
        else
        {
            Debug.LogError("无法切换场景：SaveSystem或SceneContentManager实例不可用");
        }
    }

    // 简化版场景切换命令，更方便在Yarn中使用
    [YarnCommand("goto_scene")]
    public static void GotoSceneCommand(string sceneName, string nodeName = "Start")
    {
        ChangeSceneCommand(sceneName, nodeName);
    }

    // 调试方法：打印所有场景信息
    [YarnCommand("debug_scenes")]
    public static void DebugScenesCommand()
    {
        if (Instance != null && SceneContentManager.Instance != null)
        {
            string[] allScenes = SceneContentManager.Instance.GetAllSceneNames();
            string currentScene = SceneContentManager.Instance.GetCurrentSceneName();

            Debug.Log("===== 场景调试信息 =====");
            Debug.Log($"当前场景: {currentScene}");
            Debug.Log("所有可用场景:");
            foreach (var scene in allScenes)
            {
                Debug.Log($"- {scene}");
            }
            Debug.Log("=======================");
        }
        else
        {
            Debug.LogError("无法打印场景信息：SaveSystem或SceneContentManager实例不可用");
        }
    }

    #endregion

    // 加载场景并自动开始对话
    private IEnumerator LoadSceneAndStartDialogue(SaveData data)
    {
        yield return new WaitForSeconds(1f);

        // 确保停止当前对话
        if (dialogueRunner != null)
        {
            dialogueRunner.StopDialogue();
        }

        // 检查SceneContentManager是否存在
        if (SceneContentManager.Instance == null)
        {
            Debug.LogError("SceneContentManager不存在，无法切换场景内容");
            yield break;
        }

        // 记录当前应用的场景名称
        string targetSceneName = data.currentSceneName;
        Debug.Log($"开始切换到场景: {targetSceneName}, 节点: {data.currentNodeName}");

        // 使用SceneContentManager切换场景内容
        SceneContentManager.Instance.ChangeScene(targetSceneName);

        // 等待场景内容切换完成
        yield return new WaitForSeconds(1.5f);

        // 更新当前场景名称
        currentSceneName = targetSceneName;

        // 查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        if (dialogueRunner != null)
        {
            // 设置所有变量
            if (dialogueRunner.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetAllVariables(
                    data.floatVariables,
                    data.stringVariables,
                    data.boolVariables
                );
            }

            yield return new WaitForSeconds(1f);

            // 再次确保没有对话在运行
            if (dialogueRunner.isRunning)
            {
                dialogueRunner.StopDialogue();
            }

            // 开始对话
            Debug.Log($"在场景 {targetSceneName} 中开始对话节点: {data.currentNodeName}");
            dialogueRunner.StartDialogue(data.currentNodeName);

            // 等待一小段时间，确保对话开始后再保存
            yield return new WaitForSeconds(0.5f);

            // 对话开始后再保存游戏状态
            SaveGame();
        }
        else
        {
            Debug.LogError($"场景切换后无法找到MinimalDialogueRunner");
        }
    }
}
