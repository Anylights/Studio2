using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LinePathEffect : MonoBehaviour
{
    public static LinePathEffect Instance { get; private set; }

    public LineRenderer leftLine;
    public LineRenderer rightLine;
    public float duration = 2f; // 动画持续时间（秒）
    public float offset = 1f;   // 线条在屏幕外的偏移量（世界坐标）
    public float lineZ = 0f;    // 线条所在z平面
    public float afterDrawWait = 1f; // 线绘制完成后等待消失的时间
    public Volume postProcessVolume; // 拖入或自动查找
    public float bloomMaxIntensity = 10f;

    private Vector3[] leftPath;
    private Vector3[] rightPath;
    private Coroutine drawCoroutine;
    private float timer = 0f;
    private Bloom bloom;

    void Awake()
    {
        // 单例赋值
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // 预先计算路径点
        Camera cam = Camera.main;
        float screenZ = cam.WorldToScreenPoint(new Vector3(0, 0, lineZ)).z;

        Vector3 bottomCenter = cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, 0, screenZ));
        Vector3 leftBottom = cam.ScreenToWorldPoint(new Vector3(0, 0, screenZ));
        Vector3 leftTop = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, screenZ));
        Vector3 topCenter = cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height, screenZ));
        Vector3 rightBottom = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, screenZ));
        Vector3 rightTop = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, screenZ));

        Vector3 leftOffset = new Vector3(-offset, 0, 0);
        Vector3 rightOffset = new Vector3(offset, 0, 0);
        Vector3 upOffset = new Vector3(0, offset, 0);
        Vector3 downOffset = new Vector3(0, -offset, 0);

        Vector3 bottomCenterOut = bottomCenter + downOffset;
        Vector3 leftBottomOut = leftBottom + leftOffset + downOffset;
        Vector3 leftTopOut = leftTop + leftOffset + upOffset;
        Vector3 topCenterOut = topCenter + upOffset;
        Vector3 rightBottomOut = rightBottom + rightOffset + downOffset;
        Vector3 rightTopOut = rightTop + rightOffset + upOffset;

        bottomCenterOut.z = lineZ;
        leftBottomOut.z = lineZ;
        leftTopOut.z = lineZ;
        topCenterOut.z = lineZ;
        rightBottomOut.z = lineZ;
        rightTopOut.z = lineZ;

        leftPath = new Vector3[] { bottomCenterOut, leftBottomOut, leftTopOut, topCenterOut };
        rightPath = new Vector3[] { bottomCenterOut, rightBottomOut, rightTopOut, topCenterOut };
    }

    void Start()
    {
        // 自动查找Volume
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<Volume>();
        }
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out bloom);
            if (bloom != null)
            {
                bloom.intensity.value = 0f;
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 3f)
        {
            LinePathEffect.Instance.DrawLines(0.5f, Color.red);
            timer = 0f;
        }
    }

    // 新增：只传duration和单色
    public void DrawLines(float customDuration, Color color)
    {
        DrawLines(customDuration, color, color);
    }

    // 新增：外部调用此方法开始绘制线条（只传duration）
    public void DrawLines(float customDuration)
    {
        DrawLines(customDuration, null, null);
    }

    // 新增：外部调用此方法开始绘制线条（可选颜色渐变）
    public void DrawLines(float customDuration, Color? startColor, Color? endColor)
    {
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
        }
        drawCoroutine = StartCoroutine(DrawLinesRoutine(customDuration, startColor, endColor));
    }

    // 兼容老接口（无参数）
    public void DrawLines()
    {
        DrawLines(duration, null, null);
    }

    private IEnumerator DrawLinesRoutine(float customDuration, Color? startColor, Color? endColor)
    {
        leftLine.positionCount = 1;
        leftLine.SetPosition(0, leftPath[0]);
        rightLine.positionCount = 1;
        rightLine.SetPosition(0, rightPath[0]);

        // 动画开始时先设置Emission为起始色
        Color emissionStart = startColor ?? Color.white;
        Color emissionEnd = endColor ?? Color.white;
        if (leftLine.material.HasProperty("_EmissionColor"))
            leftLine.material.SetColor("_EmissionColor", emissionStart);
        if (rightLine.material.HasProperty("_EmissionColor"))
            rightLine.material.SetColor("_EmissionColor", emissionStart);
        leftLine.startColor = emissionStart;
        leftLine.endColor = emissionStart;
        rightLine.startColor = emissionStart;
        rightLine.endColor = emissionStart;

        // Bloom前10%渐变上升
        float bloomUpTime = customDuration * 0.1f;
        float bloomDownTime = afterDrawWait;
        if (bloom != null)
        {
            bloom.intensity.value = 0f;
            float t = 0f;
            while (t < bloomUpTime)
            {
                bloom.intensity.value = Mathf.Lerp(0f, bloomMaxIntensity, t / bloomUpTime);
                t += Time.deltaTime;
                yield return null;
            }
            bloom.intensity.value = bloomMaxIntensity;
        }

        // 动画主过程
        Coroutine left = StartCoroutine(AnimateLine(leftLine, leftPath, customDuration, startColor, endColor));
        Coroutine right = StartCoroutine(AnimateLine(rightLine, rightPath, customDuration, startColor, endColor));
        yield return left;
        yield return right;

        // 动画完成后等待消失，Bloom强度逐渐降为0
        float elapsed = 0f;
        float bloomStartIntensity = bloom != null ? bloom.intensity.value : 0f;
        while (elapsed < afterDrawWait)
        {
            if (bloom != null)
            {
                bloom.intensity.value = Mathf.Lerp(bloomStartIntensity, 0f, elapsed / afterDrawWait);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (bloom != null)
            bloom.intensity.value = 0f;

        // 清除线条
        leftLine.positionCount = 0;
        rightLine.positionCount = 0;
        // 清除后Emission设为终止色或白色
        if (leftLine.material.HasProperty("_EmissionColor"))
            leftLine.material.SetColor("_EmissionColor", emissionEnd);
        if (rightLine.material.HasProperty("_EmissionColor"))
            rightLine.material.SetColor("_EmissionColor", emissionEnd);
        leftLine.startColor = emissionEnd;
        leftLine.endColor = emissionEnd;
        rightLine.startColor = emissionEnd;
        rightLine.endColor = emissionEnd;
    }

    IEnumerator AnimateLine(LineRenderer line, Vector3[] path, float customDuration, Color? startColor, Color? endColor)
    {
        float t = 0f;
        int segmentCount = path.Length - 1;
        float[] segmentLengths = new float[segmentCount];
        float totalLength = 0f;
        for (int i = 0; i < segmentCount; i++)
        {
            segmentLengths[i] = Vector3.Distance(path[i], path[i + 1]);
            totalLength += segmentLengths[i];
        }

        int currentSegment = 0;
        float currentLength = 0f;
        line.positionCount = 1;
        line.SetPosition(0, path[0]);

        Color emissionStart = startColor ?? Color.white;
        Color emissionEnd = endColor ?? Color.white;

        while (t < customDuration)
        {
            float progress = t / customDuration * totalLength;
            currentLength = 0f;
            currentSegment = 0;
            while (currentSegment < segmentCount && currentLength + segmentLengths[currentSegment] < progress)
            {
                currentLength += segmentLengths[currentSegment];
                currentSegment++;
            }
            line.positionCount = currentSegment + 2;
            for (int i = 0; i <= currentSegment; i++)
            {
                line.SetPosition(i, path[i]);
            }
            if (currentSegment < segmentCount)
            {
                float segProgress = (progress - currentLength) / segmentLengths[currentSegment];
                Vector3 interp = Vector3.Lerp(path[currentSegment], path[currentSegment + 1], segProgress);
                interp.z = path[0].z; // 保证z一致
                line.SetPosition(currentSegment + 1, interp);
            }
            else
            {
                line.SetPosition(currentSegment + 1, path[path.Length - 1]);
            }
            // 动态设置Emission和线条颜色
            if (line.material.HasProperty("_EmissionColor"))
            {
                Color lerped = Color.Lerp(emissionStart, emissionEnd, t / customDuration);
                line.material.SetColor("_EmissionColor", lerped);
                line.startColor = lerped;
                line.endColor = lerped;
            }
            t += Time.deltaTime;
            yield return null;
        }
        // 结束时设为终止色
        if (line.material.HasProperty("_EmissionColor"))
            line.material.SetColor("_EmissionColor", emissionEnd);
        line.startColor = emissionEnd;
        line.endColor = emissionEnd;
        line.positionCount = path.Length;
        for (int i = 0; i < path.Length; i++)
        {
            line.SetPosition(i, path[i]);
        }
    }

    // 兼容老的AnimateLine接口
    IEnumerator AnimateLine(LineRenderer line, Vector3[] path)
    {
        return AnimateLine(line, path, duration, null, null);
    }
}