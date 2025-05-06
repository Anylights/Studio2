using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景内容管理器 - 单场景方案
/// 负责管理所有子场景内容的显示/隐藏，替代传统场景加载
/// </summary>
public class SceneContentManager : MonoBehaviour
{
    #region 单例实现
    private static SceneContentManager _instance;
    public static SceneContentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneContentManager>();
                if (_instance == null)
                {
                    Debug.LogError("场景中找不到SceneContentManager实例！");
                }
            }
            return _instance;
        }
    }
    #endregion

    [System.Serializable]
    public class SceneContent
    {
        public string sceneName;          // 场景名称
        public GameObject contentRoot;    // 场景内容的根物体
        public bool initialState = false; // 初始状态
    }

    [Header("场景内容")]
    [SerializeField] private List<SceneContent> sceneContents = new List<SceneContent>();
    [SerializeField] private string initialSceneName = "Start_Scene";

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
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSceneMap();
    }

    private void Start()
    {
        // 在这里不再自动激活初始场景，改为由GameManager控制初始场景
    }

    private void InitializeSceneMap()
    {
        sceneMap.Clear();

        if (enableDebugLog)
        {
            Debug.Log("=== 初始化场景内容映射 ===");
        }

        foreach (SceneContent content in sceneContents)
        {
            if (content.contentRoot != null)
            {
                sceneMap.Add(content.sceneName, content.contentRoot);
                content.contentRoot.SetActive(content.initialState);

                if (enableDebugLog)
                {
                    Debug.Log($"已注册场景: {content.sceneName} -> {content.contentRoot.name}");
                }
            }
            else
            {
                Debug.LogError($"错误: 场景 {content.sceneName} 没有指定内容根物体！");
            }
        }
    }

    /// <summary>
    /// 切换到指定场景内容
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        if (isTransitioning)
        {
            if (enableDebugLog) Debug.LogWarning($"无法切换场景: 当前正在进行另一个场景切换");
            return;
        }

        if (sceneName == currentSceneName)
        {
            if (enableDebugLog) Debug.LogWarning($"无法切换场景: 已经在场景 {sceneName} 中");
            return;
        }

        if (!sceneMap.ContainsKey(sceneName))
        {
            Debug.LogError($"无法切换场景: 场景 {sceneName} 不存在! 可用场景: {string.Join(", ", GetAllSceneNames())}");
            return;
        }

        if (enableDebugLog) Debug.Log($"开始切换场景: {currentSceneName} -> {sceneName}");
        StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string newSceneName)
    {
        isTransitioning = true;
        string previousScene = currentSceneName;

        // 显示过渡画面
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }

        yield return new WaitForSeconds(transitionDuration * 0.5f);

        // 隐藏当前场景
        if (!string.IsNullOrEmpty(currentSceneName) && sceneMap.ContainsKey(currentSceneName))
        {
            if (enableDebugLog) Debug.Log($"隐藏场景: {currentSceneName}");
            sceneMap[currentSceneName].SetActive(false);
        }

        // 显示新场景
        if (sceneMap.ContainsKey(newSceneName))
        {
            if (enableDebugLog) Debug.Log($"显示场景: {newSceneName}");
            sceneMap[newSceneName].SetActive(true);
        }
        else
        {
            Debug.LogError($"严重错误: 场景 {newSceneName} 不在场景映射中");
            isTransitioning = false;
            yield break;
        }

        currentSceneName = newSceneName;

        // 初始化新场景内容
        InitializeSceneContent(newSceneName);

        yield return new WaitForSeconds(transitionDuration * 0.5f);

        // 隐藏过渡画面
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }

        // 触发场景变化事件
        OnSceneChanged?.Invoke(previousScene, newSceneName);

        isTransitioning = false;
        if (enableDebugLog) Debug.Log($"场景切换完成: 当前场景为 {currentSceneName}");
    }

    private void InitializeSceneContent(string sceneName)
    {
        if (sceneMap.TryGetValue(sceneName, out GameObject sceneRoot))
        {
            SceneContentInitializer[] initializers = sceneRoot.GetComponentsInChildren<SceneContentInitializer>(true);
            foreach (var initializer in initializers)
            {
                if (initializer != null)
                {
                    initializer.OnSceneActivated();
                }
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
    /// 获取初始场景名称
    /// </summary>
    public string GetInitialSceneName()
    {
        return initialSceneName;
    }
}