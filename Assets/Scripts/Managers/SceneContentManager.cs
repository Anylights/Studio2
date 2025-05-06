using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景内容管理器 - 单场景方案
/// 负责管理所有子场景内容的显示/隐藏，替代传统场景加载
/// </summary>
public class SceneContentManager : MonoBehaviour
{
    public static SceneContentManager Instance { get; private set; }

    [System.Serializable]
    public class SceneContent
    {
        public string sceneName;          // 场景名称
        public GameObject contentRoot;    // 场景内容的根物体
        public bool initialState = false; // 初始状态
    }

    [Header("场景内容")]
    [SerializeField] private List<SceneContent> sceneContents = new List<SceneContent>();
    [SerializeField] private string initialSceneName = "Start";

    [Header("过渡设置")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private GameObject transitionCanvas; // 过渡画面
    [SerializeField] private bool enableDebugLog = true;

    // 场景变化事件
    public delegate void SceneChangeHandler(string previousScene, string newScene);
    public event SceneChangeHandler OnSceneChanged;

    private Dictionary<string, GameObject> sceneMap = new Dictionary<string, GameObject>();
    private string currentSceneName;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化场景映射
        InitializeSceneMap();
    }

    private void InitializeSceneMap()
    {
        sceneMap.Clear();
        foreach (SceneContent content in sceneContents)
        {
            if (content.contentRoot != null)
            {
                sceneMap.Add(content.sceneName, content.contentRoot);
                content.contentRoot.SetActive(content.initialState);
            }
            else
            {
                Debug.LogError($"场景 {content.sceneName} 没有指定内容根物体！");
            }
        }
    }

    private void Start()
    {
        // 激活初始场景
        ChangeScene(initialSceneName);
    }

    /// <summary>
    /// 切换到指定场景内容
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void ChangeScene(string sceneName)
    {
        if (isTransitioning)
            return;

        if (sceneName == currentSceneName)
            return;

        if (!sceneMap.ContainsKey(sceneName))
        {
            Debug.LogError($"场景 '{sceneName}' 不存在!");
            return;
        }

        StartCoroutine(TransitionToScene(sceneName));
    }

    /// <summary>
    /// 场景过渡协程
    /// </summary>
    private IEnumerator TransitionToScene(string newSceneName)
    {
        isTransitioning = true;
        string previousScene = currentSceneName;

        // 显示过渡画面
        if (transitionCanvas != null)
            transitionCanvas.SetActive(true);

        yield return new WaitForSeconds(transitionDuration * 0.5f);

        // 隐藏当前场景
        if (!string.IsNullOrEmpty(currentSceneName) && sceneMap.ContainsKey(currentSceneName))
            sceneMap[currentSceneName].SetActive(false);

        // 显示新场景
        sceneMap[newSceneName].SetActive(true);
        currentSceneName = newSceneName;

        // 触发场景内容初始化
        InitializeSceneContent(newSceneName);

        yield return new WaitForSeconds(transitionDuration * 0.5f);

        // 隐藏过渡画面
        if (transitionCanvas != null)
            transitionCanvas.SetActive(false);

        // 触发场景变化事件
        OnSceneChanged?.Invoke(previousScene, newSceneName);

        isTransitioning = false;
    }

    /// <summary>
    /// 初始化新激活的场景内容
    /// </summary>
    private void InitializeSceneContent(string sceneName)
    {
        if (sceneMap.TryGetValue(sceneName, out GameObject sceneRoot))
        {
            // 查找SceneContentInitializer组件
            SceneContentInitializer[] initializers = sceneRoot.GetComponentsInChildren<SceneContentInitializer>(true);
            foreach (var initializer in initializers)
            {
                if (initializer != null)
                    initializer.OnSceneActivated();
            }
        }
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }

    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    public bool SceneExists(string sceneName)
    {
        return sceneMap.ContainsKey(sceneName);
    }

    /// <summary>
    /// 获取所有场景名称
    /// </summary>
    public string[] GetAllSceneNames()
    {
        string[] sceneNames = new string[sceneMap.Count];
        sceneMap.Keys.CopyTo(sceneNames, 0);
        return sceneNames;
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneContentManager] {message}");
        }
    }
}