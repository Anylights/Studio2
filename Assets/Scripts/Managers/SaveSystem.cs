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

public class SaveSystem : MonoBehaviour
{
    public int happy;
    [Header("对话运行器引用")]
    [SerializeField] private MinimalDialogueRunner dialogueRunner;

    [Header("存档设置")]
    [SerializeField] private string saveFileName = "game_save.json";

    // 场景与节点关系映射（场景名称->节点名称集合）
    [SerializeField] private SceneNodeMapping[] sceneNodeMappings;

    // 单例模式
    public static SaveSystem Instance { get; private set; }

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
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 记录当前场景
        currentSceneName = SceneManager.GetActiveScene().name;
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

        // ResetGame();
        // 开始游戏
        // StartGame();
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
        Debug.Log("开始新游戏");

        // 确保变量存储是空的
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.Clear();
        }

        // 从Start节点开始对话
        if (dialogueRunner != null)
        {
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
            Debug.Log($"游戏已保存至：{path}");
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

            // 加载场景（如果需要）
            if (saveData.currentSceneName != currentSceneName)
            {
                // 需要加载不同的场景
                StartCoroutine(LoadSceneAndContinue(saveData));
            }
            else
            {
                // 在当前场景继续
                ContinueFromSaveData(saveData);
            }
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
        // 处理Uduino以避免场景切换问题
        if (Uduino.UduinoManager.Instance != null)
        {
            Uduino.UduinoManager.Instance.isApplicationQuiting = true;
            Uduino.UduinoManager.Instance.FullReset();
        }

        // 加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(saveData.currentSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 更新当前场景名称
        currentSceneName = saveData.currentSceneName;

        // 重置Uduino的退出标志
        if (Uduino.UduinoManager.Instance != null)
        {
            Uduino.UduinoManager.Instance.isApplicationQuiting = false;
        }

        // 场景加载完成后，需要重新查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        // 重新连接Uduino
        if (UduinoSceneManager.Instance != null)
        {
            UduinoSceneManager.Instance.ReconnectUduino();
        }

        if (dialogueRunner != null)
        {
            // 继续游戏
            ContinueFromSaveData(saveData);
        }
        else
        {
            Debug.LogError($"加载场景后无法找到MinimalDialogueRunner");
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

        Debug.Log($"从节点 '{saveData.currentNodeName}' 继续游戏");

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

        // 重新加载初始场景
        // SceneManager.LoadScene(0); // 加载构建设置中的第一个场景
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
                Debug.Log("存档已删除");
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

    // 切换到指定场景和节点的YarnCommand
    [YarnCommand("change_scene")]
    public static void ChangeSceneCommand(string sceneName, string nodeName)
    {
        if (Instance != null)
        {
            Instance.ChangeScene(sceneName, nodeName);
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
        if (Instance != null)
        {
            // 获取当前场景索引
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            // 计算下一个场景索引
            int nextSceneIndex = currentSceneIndex + 1;

            // 确保下一个场景索引有效
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                string nextSceneName;

#if UNITY_EDITOR
                // 在编辑器中使用EditorSceneManager获取场景路径
                string nextScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByBuildIndex(nextSceneIndex).path;
                nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);
#else
                // 在运行时使用另一种方式
                string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
                nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);
#endif

                // 切换到下一个场景
                Instance.ChangeScene(nextSceneName, nodeName);
            }
            else
            {
                Debug.LogError($"没有下一个场景可以切换：当前场景索引 {currentSceneIndex}，场景总数 {SceneManager.sceneCountInBuildSettings}");
            }
        }
        else
        {
            Debug.LogError("无法切换场景：SaveSystem实例不可用");
        }
    }

    #endregion

    // 切换场景并开始指定节点的对话
    public void ChangeScene(string sceneName, string nodeName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称不能为空");
            return;
        }

        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogError("节点名称不能为空");
            return;
        }

        // 创建临时数据以便在新场景中使用
        SaveData tempData = new SaveData
        {
            currentNodeName = nodeName,
            currentSceneName = sceneName,
            saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 获取当前所有变量
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            var (floats, strings, bools) = dialogueRunner.VariableStorage.GetAllVariables();
            tempData.floatVariables = floats;
            tempData.stringVariables = strings;
            tempData.boolVariables = bools;
        }

        // 开始加载新场景
        StartCoroutine(LoadSceneAndStartDialogue(tempData));
    }

    // 加载场景并自动开始对话
    private IEnumerator LoadSceneAndStartDialogue(SaveData data)
    {
        yield return new WaitForSeconds(2f);

        dialogueRunner.StopDialogue();

        Debug.Log($"开始加载场景 {data.currentSceneName}，将在加载后3秒开始节点 {data.currentNodeName}");

        // 处理Uduino以避免场景切换问题
        if (Uduino.UduinoManager.Instance != null)
        {
            Uduino.UduinoManager.Instance.isApplicationQuiting = true;
            Uduino.UduinoManager.Instance.FullReset();
        }

        // 加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.currentSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 更新当前场景名称
        currentSceneName = data.currentSceneName;

        // 重置Uduino的退出标志
        if (Uduino.UduinoManager.Instance != null)
        {
            Uduino.UduinoManager.Instance.isApplicationQuiting = false;
        }

        // 场景加载完成后，需要重新查找对话运行器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();

        // 重新连接Uduino
        if (UduinoSceneManager.Instance != null)
        {
            UduinoSceneManager.Instance.ReconnectUduino();
        }

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

            // 等待3秒
            Debug.Log("场景加载完成，等待1秒后开始对话");

            yield return new WaitForSeconds(1f);

            SaveGame();

            // 开始对话
            Debug.Log($"开始节点 {data.currentNodeName}");
            dialogueRunner.StartDialogue(data.currentNodeName);
        }
        else
        {
            Debug.LogError($"加载场景后无法找到MinimalDialogueRunner");
        }
    }
}
