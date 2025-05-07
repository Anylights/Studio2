using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;

public class CharacterAnimationManager : MonoBehaviour
{
    public static CharacterAnimationManager Instance { get; private set; }

    [System.Serializable]
    public class CharacterAnimator
    {
        public string characterName;
        public Animator animator;
    }

    [SerializeField]
    private List<CharacterAnimator> characters = new List<CharacterAnimator>();
    private static Dictionary<string, Animator> characterAnimators = new Dictionary<string, Animator>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化角色动画器字典
        foreach (var character in characters)
        {
            if (!string.IsNullOrEmpty(character.characterName) && character.animator != null)
            {
                characterAnimators[character.characterName] = character.animator;
            }
            else
            {
                Debug.LogWarning($"角色 {character.characterName} 的配置不完整");
            }
        }
    }

    [YarnCommand("play_animation")]
    public static void PlayAnimation(string characterName, string triggerName)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterAnimationManager instance not found!");
            return;
        }

        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(triggerName))
        {
            Debug.LogError("角色名称或触发器名称为空");
            return;
        }

        if (characterAnimators.TryGetValue(characterName, out Animator animator))
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"播放角色 {characterName} 的动画 {triggerName}");
        }
        else
        {
            Debug.LogError($"未找到角色 {characterName} 的动画器");
        }
    }

    [YarnCommand("stop_animation")]
    public static void StopAnimation(string characterName, string triggerName)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterAnimationManager instance not found!");
            return;
        }

        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(triggerName))
        {
            Debug.LogError("角色名称或触发器名称为空");
            return;
        }

        if (characterAnimators.TryGetValue(characterName, out Animator animator))
        {
            animator.ResetTrigger(triggerName);
            Debug.Log($"停止角色 {characterName} 的动画 {triggerName}");
        }
        else
        {
            Debug.LogError($"未找到角色 {characterName} 的动画器");
        }
    }

    [YarnCommand("set_animation_bool")]
    public static void SetAnimationBool(string characterName, string paramName, bool value)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterAnimationManager instance not found!");
            return;
        }

        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("角色名称或参数名称为空");
            return;
        }

        if (characterAnimators.TryGetValue(characterName, out Animator animator))
        {
            animator.SetBool(paramName, value);
            Debug.Log($"设置角色 {characterName} 的动画参数 {paramName} 为 {value}");
        }
        else
        {
            Debug.LogError($"未找到角色 {characterName} 的动画器");
        }
    }

    [YarnCommand("set_animation_int")]
    public static void SetAnimationInt(string characterName, string paramName, int value)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterAnimationManager instance not found!");
            return;
        }

        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("角色名称或参数名称为空");
            return;
        }

        if (characterAnimators.TryGetValue(characterName, out Animator animator))
        {
            animator.SetInteger(paramName, value);
            Debug.Log($"设置角色 {characterName} 的动画参数 {paramName} 为 {value}");
        }
        else
        {
            Debug.LogError($"未找到角色 {characterName} 的动画器");
        }
    }

    [YarnCommand("set_animation_float")]
    public static void SetAnimationFloat(string characterName, string paramName, float value)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterAnimationManager instance not found!");
            return;
        }

        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("角色名称或参数名称为空");
            return;
        }

        if (characterAnimators.TryGetValue(characterName, out Animator animator))
        {
            animator.SetFloat(paramName, value);
            Debug.Log($"设置角色 {characterName} 的动画参数 {paramName} 为 {value}");
        }
        else
        {
            Debug.LogError($"未找到角色 {characterName} 的动画器");
        }
    }
}