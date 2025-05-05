using UnityEngine;
using System.Collections;

public class OptionEffectsManager : MonoBehaviour
{
    [SerializeField] private GameObject[] effects; // 拖入你的4个特效
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    // 可选：如果特效有粒子系统
    private ParticleSystem[][] particleSystems;
    private MinimalOptionsView optionsView;

    void Awake()
    {
        // 初始化粒子系统数组
        particleSystems = new ParticleSystem[effects.Length][];
        for (int i = 0; i < effects.Length; i++)
        {
            particleSystems[i] = effects[i].GetComponentsInChildren<ParticleSystem>();
        }

        // 初始时停用所有特效
        DeactivateAllEffectsImmediate();
    }

    void Start()
    {
        // 查找同一场景中的MinimalOptionsView
        optionsView = FindObjectOfType<MinimalOptionsView>();
        if (optionsView != null)
        {
            optionsView.OnOptionsShown += HandleOptionsShown;
            optionsView.OnOptionsHidden += HandleOptionsHidden;
            Debug.Log("已订阅对话选项显示/隐藏事件");
        }
        else
        {
            Debug.LogWarning("未找到MinimalOptionsView，特效将不会自动激活/停用");
        }
    }

    void OnDestroy()
    {
        // 取消事件订阅，防止内存泄漏
        if (optionsView != null)
        {
            optionsView.OnOptionsShown -= HandleOptionsShown;
            optionsView.OnOptionsHidden -= HandleOptionsHidden;
        }
    }

    // 处理选项显示事件
    private void HandleOptionsShown()
    {
        Debug.Log("选项显示，激活特效");
        ActivateEffects();
    }

    // 处理选项隐藏事件
    private void HandleOptionsHidden()
    {
        Debug.Log("选项隐藏，停用特效");
        DeactivateEffects();
    }

    // 立即激活所有特效
    public void ActivateAllEffectsImmediate()
    {
        foreach (var effect in effects)
        {
            if (effect != null)
                effect.SetActive(true);
        }
    }

    // 立即停用所有特效
    public void DeactivateAllEffectsImmediate()
    {
        foreach (var effect in effects)
        {
            if (effect != null)
                effect.SetActive(false);
        }
    }

    // 激活特效并带有淡入效果
    public void ActivateEffects()
    {
        StopAllCoroutines();
        StartCoroutine(ActivateEffectsCoroutine());
    }

    // 停用特效并带有淡出效果
    public void DeactivateEffects()
    {
        StopAllCoroutines();
        StartCoroutine(DeactivateEffectsCoroutine());
    }

    private IEnumerator ActivateEffectsCoroutine()
    {
        // 先激活所有特效
        foreach (var effect in effects)
        {
            if (effect != null && !effect.activeSelf)
                effect.SetActive(true);
        }

        // 如果有粒子系统，可以平滑启动
        for (int i = 0; i < effects.Length; i++)
        {
            if (particleSystems[i] != null && particleSystems[i].Length > 0)
            {
                foreach (var ps in particleSystems[i])
                {
                    // 重置并启动粒子系统
                    ps.Clear();
                    ps.Play();
                }
            }
        }

        yield return new WaitForSeconds(fadeInDuration);
    }

    private IEnumerator DeactivateEffectsCoroutine()
    {
        // 如果有粒子系统，先停止发射
        for (int i = 0; i < effects.Length; i++)
        {
            if (particleSystems[i] != null && particleSystems[i].Length > 0)
            {
                foreach (var ps in particleSystems[i])
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        // 等待粒子消失
        yield return new WaitForSeconds(fadeOutDuration);

        // 停用所有特效
        foreach (var effect in effects)
        {
            if (effect != null)
                effect.SetActive(false);
        }
    }

    // 激活单个特效
    public void ActivateEffect(int index)
    {
        if (index >= 0 && index < effects.Length && effects[index] != null)
        {
            effects[index].SetActive(true);

            // 如果有粒子系统，重置并启动
            if (particleSystems[index] != null && particleSystems[index].Length > 0)
            {
                foreach (var ps in particleSystems[index])
                {
                    ps.Clear();
                    ps.Play();
                }
            }
        }
    }

    // 停用单个特效
    public void DeactivateEffect(int index)
    {
        if (index >= 0 && index < effects.Length && effects[index] != null)
        {
            // 如果有粒子系统，先停止发射
            if (particleSystems[index] != null && particleSystems[index].Length > 0)
            {
                foreach (var ps in particleSystems[index])
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                // 为了给粒子消失的时间，可以使用协程
                StartCoroutine(DeactivateEffectDelayed(index));
            }
            else
            {
                // 如果没有粒子系统，直接停用
                effects[index].SetActive(false);
            }
        }
    }

    private IEnumerator DeactivateEffectDelayed(int index)
    {
        yield return new WaitForSeconds(fadeOutDuration);
        if (index >= 0 && index < effects.Length && effects[index] != null)
        {
            effects[index].SetActive(false);
        }
    }
}