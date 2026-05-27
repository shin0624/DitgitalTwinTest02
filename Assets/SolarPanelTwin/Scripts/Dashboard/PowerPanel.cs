using UnityEngine;
using TMPro;
public class PowerPanel : MonoBehaviour
{
    // 현재 발전량과 누적 발전량을 표시하는 패널 스크립트.

    public DashboardManager dashboard;// 대시보드 매니저 참조. 패널 데이터와 집계값을 읽어와 표시.

    [Header("TMP")]
    public TextMeshProUGUI currentPowerText;// 현재 발전량 텍스트
    public TextMeshProUGUI cumulativePowerText;// 누적 발전량 텍스트
    public TextMeshProUGUI dcPowerText;// DC 전력 텍스트

    void Update()
    {
        if (dashboard == null || dashboard.energyBus == null)
        {
            return;
        }
        
        currentPowerText.text = $"{dashboard.energyBus.totalAcPowerW:F1} W";
        cumulativePowerText.text = $"{dashboard.energyBus.cumulativeAcKWh:F3} kWh";
        dcPowerText.text = $"{dashboard.energyBus.totalDcPowerW:F1} W";

    }
}
