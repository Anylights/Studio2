using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Yarn.Unity;

public class TimeLineManager : MonoBehaviour
{
    // 单例模式
    public static TimeLineManager Instance { get; private set; }

    [System.Serializable]
    public class TimelineInfo
    {
        public string id;                         // 时间线的唯一标识符
        public string description;                // 描述（用于在编辑器中更容易识别）
        public PlayableDirector playableDirector; // 对应的PlayableDirector组件
        public TimelineAsset timelineAsset;       // 时间线资产
    }

    [SerializeField] private List<TimelineInfo> timelineList = new List<TimelineInfo>();

    // 当前正在播放的时间线信息
    private TimelineInfo currentPlaying;

    // 对话运行器引用
    private MinimalDialogueRunner dialogueRunner;

    // 角色对话管理器引用
    private CharacterDialogueManager dialogueManager;

    // 等待时间线完成的协程
    private Coroutine waitForTimelineCoroutine;

    // 用于快速查找时间线的字典
    private Dictionary<string, TimelineInfo> timelineDict = new Dictionary<string, TimelineInfo>();

    private void Awake()
    {
        // 单例模式设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // 初始化字典以便快速查找
        foreach (var timeline in timelineList)
        {
            if (!string.IsNullOrEmpty(timeline.id))
            {
                timelineDict[timeline.id.ToLower()] = timeline;
            }
            else
            {
                Debug.LogWarning("找到一个没有ID的时间线配置，将被忽略");
            }
        }
    }

    private void Start()
    {
        // 查找对话运行器和角色对话管理器
        dialogueRunner = FindObjectOfType<MinimalDialogueRunner>();
        dialogueManager = FindObjectOfType<CharacterDialogueManager>();

        if (dialogueRunner == null)
        {
            Debug.LogWarning("场景中没有找到MinimalDialogueRunner，Timeline功能可能不会正常工作");
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning("场景中没有找到CharacterDialogueManager，无法隐藏对话框");
        }
    }

    // 从Yarn脚本调用的命令 - 使用普通Action方法，不返回协程
    [YarnCommand("play_timeline")]
    public void PlayTimelineFromYarn(string timelineId)
    {
        // 隐藏所有对话框
        if (dialogueManager != null)
        {
            dialogueManager.HideAllDialogues();
        }

        // 播放Timeline
        Play(timelineId);

        // 启动等待协程，当Timeline播放完成后继续对话
        if (waitForTimelineCoroutine != null)
        {
            StopCoroutine(waitForTimelineCoroutine);
        }
        waitForTimelineCoroutine = StartCoroutine(WaitForTimelineAndContinue());
    }

    // 等待Timeline播放完成，然后继续对话
    private IEnumerator WaitForTimelineAndContinue()
    {
        // 等待Timeline播放完成
        yield return new WaitUntil(() => !IsAnyPlaying());

        // Timeline播放完成后，继续对话
        if (dialogueRunner != null && dialogueRunner.isRunning)
        {
            // 延迟一帧再继续对话，确保所有内容都准备好
            yield return null;
            Debug.Log("Timeline播放完成，继续对话");
            dialogueRunner.Continue();
        }

        waitForTimelineCoroutine = null;
    }

    // 公共方法，可以从其他脚本调用
    public void Play(string timelineId)
    {
        timelineId = timelineId.ToLower(); // 转为小写以确保不区分大小写

        if (timelineDict.TryGetValue(timelineId, out TimelineInfo timeline))
        {
            PlayTimeline(timeline);
        }
        else
        {
            Debug.LogError($"找不到ID为 '{timelineId}' 的时间线");
        }
    }

    private void PlayTimeline(TimelineInfo timeline)
    {
        // 如果之前有在播放的时间线，先停止
        if (currentPlaying != null && currentPlaying.playableDirector != null)
        {
            currentPlaying.playableDirector.Stop();
        }

        currentPlaying = timeline;

        if (timeline.playableDirector != null && timeline.timelineAsset != null)
        {
            // 设置要播放的时间线资产
            timeline.playableDirector.playableAsset = timeline.timelineAsset;

            // 注册播放完成的回调
            timeline.playableDirector.stopped -= OnTimelineFinished;
            timeline.playableDirector.stopped += OnTimelineFinished;

            // 开始播放
            timeline.playableDirector.Play();

            Debug.Log($"开始播放时间线: {timeline.id} - {timeline.description}");
        }
        else
        {
            Debug.LogError($"时间线 '{timeline.id}' 配置不完整，PlayableDirector或TimelineAsset为空");
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        if (currentPlaying != null && currentPlaying.playableDirector == director)
        {
            Debug.Log($"时间线播放完成: {currentPlaying.id}");
            currentPlaying = null;
        }
    }

    // 公共方法，用于从代码中停止当前正在播放的时间线
    public void StopCurrentTimeline()
    {
        if (currentPlaying != null && currentPlaying.playableDirector != null)
        {
            currentPlaying.playableDirector.Stop();
            currentPlaying = null;
        }
    }

    // 公共方法，检查指定ID的时间线是否正在播放
    public bool IsPlaying(string timelineId)
    {
        timelineId = timelineId.ToLower();

        if (currentPlaying != null &&
            currentPlaying.id.ToLower() == timelineId &&
            currentPlaying.playableDirector != null &&
            currentPlaying.playableDirector.state == PlayState.Playing)
        {
            return true;
        }

        return false;
    }

    // 公共方法，检查是否有任何时间线正在播放
    public bool IsAnyPlaying()
    {
        return currentPlaying != null &&
               currentPlaying.playableDirector != null &&
               currentPlaying.playableDirector.state == PlayState.Playing;
    }
}
