using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SensorUI : MonoBehaviour
{
    [Header("데이터 소스")]
    public SensorData sensorData;

    [Header("UI 요소")]
    public TextMeshProUGUI valueLabel;      // 온도 수치 텍스트
    public TextMeshProUGUI statusLabel;     // 상태 텍스트 (NORMAL / WARNING)
    public Image statusIndicator;           // 색상 인디케이터 Image

    [Header("경보 설정")]
    public float warningThreshold = 30f;   // 경보 임계값
    public Color normalColor = new Color(0.2f, 0.8f, 0.4f);   // 초록
    public Color warningColor = new Color(0.9f, 0.3f, 0.2f);  // 빨강

    void OnEnable()  { sensorData.OnValueChanged += UpdateUI; }
    void OnDisable() { sensorData.OnValueChanged -= UpdateUI; }

    private void UpdateUI(float temp)
    {
        bool isWarning = temp > warningThreshold;

        // 수치 텍스트 갱신
        valueLabel.text = $"{temp:F1} °C";

        // 상태 텍스트 및 색상 전환
        statusLabel.text   = isWarning ? "⚠ WARNING" : "● NORMAL";
        statusLabel.color  = isWarning ? warningColor : normalColor;
        statusIndicator.color = isWarning ? warningColor : normalColor;
    }
}


