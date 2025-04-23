using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public class SaveSystemUI : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] private GameObject savePanel;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI saveInfoText;

    [Header("设置")]
    [SerializeField] private bool showSaveInfoOnStart = true;

    private SaveSystem saveSystem;

    private void Start()
    {
        saveSystem = FindObjectOfType<SaveSystem>();

        if (saveSystem == null)
        {
            Debug.LogWarning("未找到SaveSystem，UI将无法正常工作");
            return;
        }

        // 初始化按钮事件
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);

        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        // 更新存档信息显示
        UpdateSaveInfo();

        // 根据是否有存档决定加载按钮是否可用
        if (loadButton != null)
            loadButton.interactable = saveSystem.HasSaveData();
    }

    private void OnSaveButtonClicked()
    {
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
            UpdateSaveInfo();

            // 确保加载按钮可用
            if (loadButton != null)
                loadButton.interactable = true;
        }
    }

    private void OnLoadButtonClicked()
    {
        if (saveSystem != null && saveSystem.HasSaveData())
        {
            saveSystem.LoadGame();
        }
    }

    private void OnResetButtonClicked()
    {
        if (saveSystem != null)
        {
            // 弹出确认对话框（可选）
            saveSystem.ResetGame();
            UpdateSaveInfo();

            // 重置后禁用加载按钮
            if (loadButton != null)
                loadButton.interactable = false;
        }
    }

    private void UpdateSaveInfo()
    {
        if (saveInfoText != null && saveSystem != null)
        {
            if (saveSystem.HasSaveData())
            {
                saveInfoText.text = $"上次存档时间: {saveSystem.GetSaveTime()}";
            }
            else
            {
                saveInfoText.text = "无存档数据";
            }
        }
    }

    // Yarn命令，用于显示/隐藏存档面板
    [YarnCommand("show_save_panel")]
    public void ShowSavePanel()
    {
        if (savePanel != null)
        {
            savePanel.SetActive(true);

            // 更新存档信息
            UpdateSaveInfo();
        }
    }

    [YarnCommand("hide_save_panel")]
    public void HideSavePanel()
    {
        if (savePanel != null)
        {
            savePanel.SetActive(false);
        }
    }
}