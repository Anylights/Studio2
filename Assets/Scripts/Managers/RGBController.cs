using UnityEngine;
using Uduino;
using System.Collections;

public class RgbController : MonoBehaviour
{
    public static RgbController Instance { get; private set; }

    [Header("调试选项")]
    [SerializeField] private bool enableDebugLogs = true;

    // 检查Uduino管理器是否可用
    private bool IsUduinoAvailable => UduinoManager.Instance != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        // 延迟一秒再设置颜色，确保Uduino已初始化
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);

        // 确认Uduino可用
        if (!IsUduinoAvailable)
        {
            Debug.LogError("Uduino管理器未找到，无法控制灯带");
            yield break;
        }

        // 先测试将灯带设为低亮度红色
        SetColor(Color.red);

        yield return new WaitForSeconds(1f);

        // 测试设置单个像素
        Debug.Log("测试设置单个像素...");

        // 先将所有灯珠设为黑色（关闭）
        SetColor(Color.black);

        yield return new WaitForSeconds(0.5f);

        // 测试在第一条灯带上设置几个像素为不同颜色
        SetPixelColor(1, 0, Color.red);      // 第1条灯带，第1个像素为红色
        SetPixelColor(1, 10, Color.green);   // 第1条灯带，第11个像素为绿色
        SetPixelColor(1, 8, Color.blue);    // 第1条灯带，第21个像素为蓝色

        yield return new WaitForSeconds(0.5f);

        // 测试在第二条灯带上设置几个像素为不同颜色
        SetPixelColor(2, 0, Color.yellow);   // 第2条灯带，第1个像素为黄色
        SetPixelColor(2, 10, Color.cyan);    // 第2条灯带，第11个像素为青色
        SetPixelColor(2, 11, Color.magenta); // 第2条灯带，第11个像素为品红色

        yield return new WaitForSeconds(2f);

        // 如果测试成功，恢复为白色
        Debug.Log("像素测试完成，恢复为白色...");
        SetWhiteColor();
    }

    // 设置两条灯带为白色
    public void SetWhiteColor()
    {
        SetColor(Color.white);
    }

    // 提供公共方法以供其他脚本调用
    public void SetColor(Color color)
    {
        if (!IsUduinoAvailable) return;

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            // 确保命令名称与Arduino中注册的完全一致
            UduinoManager.Instance.sendCommand("SetColor", r.ToString(), g.ToString(), b.ToString());

            if (enableDebugLogs)
                Debug.Log($"灯带颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置灯带颜色时出错: {e.Message}");
        }
    }

    // 设置特定灯带的颜色
    public void SetStripColor(int stripIndex, Color color)
    {
        if (!IsUduinoAvailable) return;

        // Arduino代码中灯带索引从1开始
        int arduinoStripIndex = stripIndex + 1;

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            UduinoManager.Instance.sendCommand("SetStripColor", arduinoStripIndex.ToString(), r.ToString(), g.ToString(), b.ToString());

            if (enableDebugLogs)
                Debug.Log($"灯带{stripIndex}颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置灯带颜色时出错: {e.Message}");
        }
    }

    // 设置指定灯带指定灯珠的颜色
    public void SetPixelColor(int stripIndex, int pixelIndex, Color color)
    {
        if (!IsUduinoAvailable) return;

        // Arduino代码中灯带索引从1开始
        int arduinoStripIndex = stripIndex + 1;

        // 确保像素索引在有效范围内
        if (pixelIndex < 0 || pixelIndex >= 52)
        {
            Debug.LogError($"无效的像素索引: {pixelIndex}，应为0-51之间");
            return;
        }

        try
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            // 确保命令名称与Arduino中注册的完全一致
            UduinoManager.Instance.sendCommand("SetPixelColor",
                arduinoStripIndex.ToString(),
                pixelIndex.ToString(),
                r.ToString(),
                g.ToString(),
                b.ToString());

            if (enableDebugLogs)
                Debug.Log($"灯带{stripIndex}的第{pixelIndex}个像素颜色设置为: R={r}, G={g}, B={b}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置像素颜色时出错: {e.Message}");
        }
    }

}