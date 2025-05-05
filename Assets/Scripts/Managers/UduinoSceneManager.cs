using System.Collections;
using UnityEngine;

public class UduinoSceneManager : MonoBehaviour
{
    public static UduinoSceneManager Instance { get; private set; }

    [SerializeField] private float reconnectDelay = 1.5f;
    private bool isReconnecting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 确保只有一个Uduino实例
        CheckUduinoInstances();
    }

    // 检查场景中的Uduino实例
    private void CheckUduinoInstances()
    {
        Uduino.UduinoManager[] managers = FindObjectsOfType<Uduino.UduinoManager>();
        if (managers.Length > 1)
        {
            Debug.LogWarning("检测到多个Uduino实例，将保留第一个实例并销毁其他实例");
            for (int i = 1; i < managers.Length; i++)
            {
                Destroy(managers[i].gameObject);
            }
        }
    }

    // 在场景切换后重新连接Arduino
    public void ReconnectUduino()
    {
        if (isReconnecting) return;

        StartCoroutine(ReconnectAfterDelay());
    }

    private IEnumerator ReconnectAfterDelay()
    {
        isReconnecting = true;

        yield return new WaitForSeconds(reconnectDelay);

        if (Uduino.UduinoManager.Instance != null)
        {
            Debug.Log("重新连接Uduino...");
            Uduino.UduinoManager.Instance.DiscoverPorts();
        }

        isReconnecting = false;
    }
}