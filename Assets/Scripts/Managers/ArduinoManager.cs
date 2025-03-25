using UnityEngine;
using Uduino;

public class ArduinoController : MonoBehaviour
{
    public static ArduinoController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        UduinoManager.Instance.pinMode(AnalogPin.A2, PinMode.Input);
        UduinoManager.Instance.pinMode(AnalogPin.A3, PinMode.Input);
    }

    public float GetHorizontalInput()
    {
        int a2Value = UduinoManager.Instance.analogRead(AnalogPin.A2);
        int a3Value = UduinoManager.Instance.analogRead(AnalogPin.A3);

        bool redButtonPressed = (a2Value > 250);
        bool greenButtonPressed = (a3Value > 250);

        if (redButtonPressed && greenButtonPressed) return 0f;
        else if (redButtonPressed) return -1f;
        else if (greenButtonPressed) return 1f;
        else return 0f;
    }
}
