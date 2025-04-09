using UnityEngine;
using Uduino;
using System.Collections;

public class RgbController : MonoBehaviour
{
    public static RgbController Instance { get; private set; }
    private bool isRed = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        SetColor(Color.red);
        StartCoroutine(ToggleColorRoutine());
    }

    private IEnumerator ToggleColorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (isRed)
                SetColor(Color.green);
            else
                SetColor(Color.red);
            isRed = !isRed;
        }
    }

    public void SetColor(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        Debug.Log($"设置RGB灯环颜色: R={r}, G={g}, B={b}");
        UduinoManager.Instance.sendCommand("SetColor", r.ToString(), g.ToString(), b.ToString());
    }
}