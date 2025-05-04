using UnityEngine;
using Uduino;

// 文件名是 ArduinoManager.cs，但类名是 ArduinoController
// 最好统一一下，这里暂时保持类名为 ArduinoController
public class ArduinoController : MonoBehaviour
{
    public static ArduinoController Instance { get; private set; }

    // 记录按钮上一帧的状态
    private int previousRedState = 1; // 假设初始未按下 (pull-up)
    private int previousGreenState = 1; // 假设初始未按下 (pull-up)

    // 记录当前帧按钮状态
    private int currentRedState = 1;
    private int currentGreenState = 1;

    // 公开属性，用于判断按钮是否在本帧被按下
    public bool RedButtonDown { get; private set; }
    public bool GreenButtonDown { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 如果需要跨场景保留，可以取消注释
        }
    }

    void Start()
    {
        UduinoManager.Instance.pinMode(2, PinMode.Input_pullup);
        UduinoManager.Instance.pinMode(3, PinMode.Input_pullup);
        Debug.Log("按钮引脚初始化完成");
    }

    // 在 Update 中读取输入并检测状态变化
    void Update()
    {
        // 读取当前状态 (0 表示按下)
        currentRedState = UduinoManager.Instance.digitalRead(2);
        currentGreenState = UduinoManager.Instance.digitalRead(3);

        // 检测是否从"未按下"变为"按下"
        RedButtonDown = (previousRedState != 0 && currentRedState == 0);
        GreenButtonDown = (previousGreenState != 0 && currentGreenState == 0);

        // 添加调试日志
        if (RedButtonDown || GreenButtonDown)
        {
            Debug.Log($"按钮状态: Red={currentRedState}, Green={currentGreenState}");
            Debug.Log($"按钮按下: Red={RedButtonDown}, Green={GreenButtonDown}");
        }

        // 更新上一帧状态
        previousRedState = currentRedState;
        previousGreenState = currentGreenState;

    }


    // 这个方法现在只负责返回方向，不处理 LED
    public float GetHorizontalInput()
    {
        bool redButtonPressed = (currentRedState == 0);
        bool greenButtonPressed = (currentGreenState == 0);

        if (redButtonPressed && !greenButtonPressed)
        {
            return -1f;
        }
        else if (!redButtonPressed && greenButtonPressed)
        {
            return 1f;
        }
        else
        {
            // 同时按下或都没按
            return 0f;
        }
    }
}