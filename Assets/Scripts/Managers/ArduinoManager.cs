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
        UduinoManager.Instance.pinMode(2, PinMode.Input_pullup);
        UduinoManager.Instance.pinMode(3, PinMode.Input_pullup);
        UduinoManager.Instance.pinMode(8, PinMode.Output);
        UduinoManager.Instance.pinMode(9, PinMode.Output);
    }

    public float GetHorizontalInput()
    {
        bool redButtonPressed = (UduinoManager.Instance.digitalRead(2) == 0);
        bool greenButtonPressed = (UduinoManager.Instance.digitalRead(3) == 0);

        if (redButtonPressed && greenButtonPressed)
        {
            UduinoManager.Instance.digitalWrite(8, State.LOW);
            UduinoManager.Instance.digitalWrite(9, State.LOW);
            return 0f;
        }
        else if (redButtonPressed)
        {
            UduinoManager.Instance.digitalWrite(8, State.HIGH);
            UduinoManager.Instance.digitalWrite(9, State.LOW);
            return -1f;
        }
        else if (greenButtonPressed)
        {
            UduinoManager.Instance.digitalWrite(8, State.LOW);
            UduinoManager.Instance.digitalWrite(9, State.HIGH);
            return 1f;
        }
        else
        {
            UduinoManager.Instance.digitalWrite(8, State.LOW);
            UduinoManager.Instance.digitalWrite(9, State.LOW);
            return 0f;
        }
    }
}
