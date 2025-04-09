using UnityEngine;
using System.Collections;
using Uduino;

[System.Serializable]
public class NeopixelStripConfig
{
    public int stripID;
    public int pin;
    public int numPixels;
}

public class NeopixelManager : MonoBehaviour
{
    public NeopixelStripConfig[] strips; // 在 Inspector 中配置每个灯环
    public Color TargetColor = Color.red; // 默认颜色
    public int brightness = 50; // 默认亮度

    private bool sequenceStarted = false; // 跟踪序列是否已开始

    void Start()
    {
        // 初始化所有灯环
        foreach (var strip in strips)
        {
            InitializeStrip(strip.stripID, strip.pin, strip.numPixels);
        }

        // 设置初始亮度
        foreach (var strip in strips)
        {
            SetBrightness(strip.stripID, brightness);
        }

        // 设置初始颜色
        foreach (var strip in strips)
        {
            SetColor(strip.stripID, TargetColor, 0.5f);
        }

        // 延迟一小段时间后再启动颜色序列
        // Invoke("BeginColorSequence", 1.0f);
    }

    void BeginColorSequence()
    {
        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartColorSequence(0);
        }
    }

    // 初始化灯环
    public void InitializeStrip(int stripID, int pin, int numPixels)
    {
        Debug.Log($"初始化灯环: ID={stripID}, Pin={pin}, Pixels={numPixels}");
        UduinoManager.Instance.sendCommand("initStrip", stripID.ToString(), pin.ToString(), numPixels.ToString());
    }

    // 设置颜色（指定灯环ID）
    public void SetColor(int stripID, Color color, float duration)
    {
        int r = (int)(color.r * 255);
        int g = (int)(color.g * 255);
        int b = (int)(color.b * 255);
        Debug.Log($"设置颜色: ID={stripID}, R={r}, G={g}, B={b}, Duration={duration}");
        UduinoManager.Instance.sendCommand("setColor", stripID.ToString(), r.ToString(), g.ToString(), b.ToString(), duration.ToString());
    }

    // 设置亮度（指定灯环ID）
    public void SetBrightness(int stripID, int brightness)
    {
        Debug.Log($"设置亮度: ID={stripID}, Brightness={brightness}");
        UduinoManager.Instance.sendCommand("setBrightness", stripID.ToString(), brightness.ToString());
    }

    // 示例：自动颜色序列
    public void StartColorSequence(int stripID)
    {
        StartCoroutine(ColorSequence(stripID));
    }

    IEnumerator ColorSequence(int stripID)
    {
        // 使用纯色进行测试
        SetColor(stripID, Color.red, 2.0f);
        yield return new WaitForSeconds(2.0f);

        SetColor(stripID, Color.green, 2.0f);
        yield return new WaitForSeconds(2.0f);

        SetColor(stripID, Color.blue, 2.0f);
        yield return new WaitForSeconds(2.0f);

        // 循环调用自身以持续颜色变化
        StartColorSequence(stripID);
    }
}