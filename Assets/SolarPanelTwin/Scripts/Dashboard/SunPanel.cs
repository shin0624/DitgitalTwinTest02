using TMPro;
using UnityEngine;

public class SunPanel : MonoBehaviour
{
    // 현재 일조량, 셀 온도를 보여주는 패널 스크립트.
    public DashboardManager dashboard;

    public TextMeshProUGUI irradianceText;
    public TextMeshProUGUI cellTempText;

    void Update()
    {
        if (dashboard == null || dashboard.latestPanelData == null) 
        {
            return;
        }
        irradianceText.text = $"{dashboard.latestPanelData.irradiance:F0} W/m²";
        cellTempText.text = $"{dashboard.latestPanelData.cellTempC:F1} °C";
    }
}
