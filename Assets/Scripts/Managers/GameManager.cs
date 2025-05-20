using System.Collections;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 游戏管理器
/// 负责协调场景切换、对话系统和游戏流程
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例实现
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    Debug.LogError("场景中找不到GameManager实例！");
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("系统引用")]
    [SerializeField] private SceneContentManager sceneManager;
    [SerializeField] private MinimalDialogueRunner dialogueRunner;
    [SerializeField] private SaveSystem saveSystem;

    [Header("初始设置")]
    [SerializeField] private string startSceneName = "Start_Scene";
    [SerializeField] private string startNodeName = "Start";
    [SerializeField] private string defaultGameSceneName = "Level1_lab";
    [SerializeField] private string defaultGameNodeName = "lab";
    [SerializeField] private bool enableDebugLog = true;

    private bool isGameStarted = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 查找关键组件
        if (sceneManager == null) sceneManager = FindObjectOfType<SceneContentManager>();
        if (dialogueRunner == null) dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
        if (saveSystem == null) saveSystem = FindObjectOfType<SaveSystem>();

        // 检查关键组件是否存在
        if (sceneManager == null || dialogueRunner == null || saveSystem == null)
        {
            Debug.LogError("GameManager无法找到关键组件，游戏可能无法正常运行！");
        }
    }

    private void Start()
    {
        // 订阅场景变化事件
        if (sceneManager != null)
        {
            sceneManager.OnSceneChanged += HandleSceneChanged;
        }

        // 订阅存档加载事件
        if (saveSystem != null)
        {
            saveSystem.OnSaveLoaded += HandleSaveLoaded;
        }

        // 订阅选项选择事件
        EventCenter.Instance.Subscribe<int>("optionSelected", HandleOptionSelected);

        // 确保使用正确的初始场景
        if (sceneManager != null)
        {
            string initialScene = startSceneName;
            if (string.IsNullOrEmpty(initialScene))
            {
                initialScene = sceneManager.GetInitialSceneName();
            }

            if (enableDebugLog) Debug.Log("显示初始场景: " + initialScene);
            sceneManager.ChangeScene(initialScene);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (sceneManager != null)
        {
            sceneManager.OnSceneChanged -= HandleSceneChanged;
        }

        if (saveSystem != null)
        {
            saveSystem.OnSaveLoaded -= HandleSaveLoaded;
        }

        EventCenter.Instance.Unsubscribe<int>("optionSelected", HandleOptionSelected);
    }

    private void Update()
    {
        // 检测ESC键退出游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        // 检测Arduino按钮输入（仅在开始场景时）
        if (!isGameStarted && sceneManager != null &&
            sceneManager.GetCurrentSceneName() == startSceneName &&
            ArduinoController.Instance != null)
        {
            // 检测按钮按下
            if (((ArduinoController.Instance.RedButtonDown || ArduinoController.Instance.GreenButtonDown)
            && !(ArduinoController.Instance.RedButtonDown && ArduinoController.Instance.GreenButtonDown)) ||
            Input.GetKeyDown(KeyCode.Space))
            {
                if (enableDebugLog) Debug.Log("按钮按下，准备开始游戏");
                StartStartScreenDialogue();
            }
        }
    }

    // 开始初始界面对话
    private void StartStartScreenDialogue()
    {
        isGameStarted = true;
        if (dialogueRunner != null)
        {
            if (enableDebugLog) Debug.Log("开始初始对话节点: " + startNodeName);
            dialogueRunner.StartDialogue(startNodeName);
        }
    }

    // 处理选项选择事件
    private void HandleOptionSelected(int optionIndex)
    {
        if (enableDebugLog) Debug.Log($"GameManager接收到选项选择事件: {optionIndex}");

        // 如果对话视图组件存在，检查其状态
        MinimalOptionsView optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null && optionsView.IsSelectionInProgress())
        {
            if (enableDebugLog) Debug.Log("选项选择正在进行中，延迟处理Yarn命令");

            // 如果选项选择正在进行中，等待一小段时间再继续处理Yarn命令
            // 注意：这部分由YarnCommand和对话系统自身处理，我们只需确保不会过早开始场景切换
            StartCoroutine(DelayYarnCommandProcessing());
        }
    }

    // 延迟Yarn命令处理，确保选项选择完成
    private IEnumerator DelayYarnCommandProcessing()
    {
        yield return new WaitForSeconds(0.1f);

        // 等待任何可能的选项选择完成
        MinimalOptionsView optionsView = FindObjectOfType<MinimalOptionsView>();
        float waitTime = 0f;
        float maxWaitTime = 2f; // 最长等待2秒

        while (optionsView != null && optionsView.IsSelectionInProgress() && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;

            if (enableDebugLog && waitTime % 0.5f < 0.1f) // 每0.5秒输出一次日志
            {
                Debug.Log($"正在等待选项选择完成... ({waitTime:F1}s)");
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("选项选择等待完成");
        }
    }

    // 处理场景变化事件
    private void HandleSceneChanged(string previousScene, string newScene)
    {
        if (enableDebugLog) Debug.Log($"场景已切换: {previousScene} -> {newScene}");
    }

    // 处理存档加载事件
    private void HandleSaveLoaded(SaveSystem.SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("加载的存档数据为空");
            return;
        }

        if (enableDebugLog) Debug.Log($"正在处理存档数据: 场景={saveData.currentSceneName}, 节点={saveData.currentNodeName}");

        // 先应用变量
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            saveSystem.ApplyVariables(
                saveData.floatVariables,
                saveData.stringVariables,
                saveData.boolVariables
            );
        }

        // 切换场景并启动对话
        ChangeSceneAndStartDialogue(saveData.currentSceneName, saveData.currentNodeName);
    }

    #region 公共方法

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        if (enableDebugLog) Debug.Log("开始新游戏");

        // 清空变量存储
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            dialogueRunner.VariableStorage.Clear();
        }

        // 切换到第一个游戏场景并开始默认对话
        ChangeSceneAndStartDialogue(defaultGameSceneName, defaultGameNodeName);
    }

    /// <summary>
    /// 加载保存的游戏
    /// </summary>
    public void LoadGame()
    {
        if (saveSystem != null)
        {
            if (enableDebugLog) Debug.Log("尝试加载游戏存档");
            saveSystem.LoadGame();
        }
        else
        {
            Debug.LogError("无法加载游戏：SaveSystem不可用");
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        if (enableDebugLog) Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 切换场景并开始特定对话节点
    /// </summary>
    public void ChangeSceneAndStartDialogue(string sceneName, string nodeName)
    {
        StartCoroutine(StopDialogueWithDelay(0.6f, sceneName, nodeName));

    }

    private IEnumerator StopDialogueWithDelay(float delay, string sceneName, string nodeName)
    {
        yield return new WaitForSeconds(delay);
        // 停止当前对话
        if (dialogueRunner.isRunning)
        {
            dialogueRunner.StopDialogue();
        }

        if (enableDebugLog) Debug.Log($"准备切换场景并开始对话: 场景={sceneName}, 节点={nodeName}");

        // 保存目标节点，用于场景切换完成后开始对话
        string targetNodeName = nodeName;

        // 订阅一次性场景切换事件，用于在场景切换完成后开始对话
        sceneManager.OnSceneChanged += OnSceneChangedForDialogue;

        // 切换场景
        sceneManager.ChangeScene(sceneName);

        // 一次性事件处理
        void OnSceneChangedForDialogue(string prevScene, string newScene)
        {
            // 取消订阅，确保这个处理只执行一次
            sceneManager.OnSceneChanged -= OnSceneChangedForDialogue;

            if (newScene == sceneName)
            {
                StartCoroutine(StartDialogueAfterDelay(targetNodeName, 0.5f));
            }
        }
    }

    // 在延迟后开始对话
    private IEnumerator StartDialogueAfterDelay(string nodeName, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (dialogueRunner != null)
        {
            if (enableDebugLog) Debug.Log($"开始对话节点: {nodeName}");

            // 确保对话不在运行，这是为了防止可能的冲突
            if (dialogueRunner.isRunning)
            {
                dialogueRunner.StopDialogue();
                yield return null; // 等待一帧，确保停止完成
            }

            // 检查节点是否存在
            if (dialogueRunner.NodeExists(nodeName))
            {
                dialogueRunner.StartDialogue(nodeName);
            }
            else
            {
                Debug.LogError($"无法开始对话：节点 {nodeName} 不存在");
            }
        }
        else
        {
            Debug.LogError("无法开始对话：MinimalDialogueRunner不可用");
        }
    }

    /// <summary>
    /// 判断是开始新游戏还是加载旧游戏
    /// </summary>
    public void StartGame()
    {
        if (saveSystem != null && saveSystem.DoesSaveExist())
        {
            // 存在存档，加载游戏
            if (enableDebugLog) Debug.Log("发现存档，加载游戏");
            LoadGame();
        }
        else
        {
            // 不存在存档，开始新游戏
            if (enableDebugLog) Debug.Log("无存档，开始新游戏");
            StartNewGame();
        }
    }

    #endregion

    #region Yarn命令

    [YarnCommand("start_game")]
    public static void StartGameCommand()
    {
        if (Instance != null)
        {
            Instance.StartGame();
        }
        else
        {
            Debug.LogError("无法开始游戏：GameManager实例不可用");
        }
    }

    [YarnCommand("quit_game")]
    public static void QuitGameCommand()
    {
        if (Instance != null)
        {
            Instance.QuitGame();
        }
        else
        {
            Debug.LogError("无法退出游戏：GameManager实例不可用");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    [YarnCommand("change_scene")]
    public static void ChangeSceneCommand(string sceneName, string nodeName)
    {
        if (Instance != null)
        {
            Instance.ChangeSceneAndStartDialogue(sceneName, nodeName);
        }
        else
        {
            Debug.LogError("无法切换场景：GameManager实例不可用");
        }
    }

    [YarnCommand("goto_scene")]
    public static void GotoSceneCommand(string sceneName, string nodeName = "Start")
    {
        ChangeSceneCommand(sceneName, nodeName);
    }

    #endregion
}