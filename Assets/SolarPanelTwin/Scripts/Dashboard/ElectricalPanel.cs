using UnityEngine;
using TMPro;

public class ElectricalPanel : MonoBehaviour
{
    // 전압, 전류, 역률, 주파수 표시를 위한 패널 스크립트. 대시보드 매니저에서 EnergyBus의 전기적 정보와 집계값을 읽어와 표시.
    
    public DashboardManager dashboard;// 대시보드 매니저 참조
    public TextMeshProUGUI voltageText;// 전압 텍스트
    public TextMeshProUGUI currentText;// 전류 텍스트
    public TextMeshProUGUI pfText;// 역률 텍스트
    public TextMeshProUGUI freqText;// 주파수 텍스트


    void Update()
    {
        if (dashboard == null || dashboard.energyBus == null) return;

        voltageText.text = $"{dashboard.energyBus.acVoltageV:F1} V";
        currentText.text = $"{dashboard.energyBus.acCurrentA:F2} A";
        pfText.text = $"{dashboard.energyBus.powerFactor:F2}";
        freqText.text = $"{dashboard.energyBus.frequencyHz:F1} Hz";
    }
}
