using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class TestCommander : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [YarnCommand("fade_camera")]
    public static void FadeCamera()
    {
        Debug.Log("45678974564891564891564187");
    }
}
