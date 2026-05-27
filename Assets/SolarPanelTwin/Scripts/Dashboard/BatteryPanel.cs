using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BatteryPanel : MonoBehaviour
{
    //배터리 soc를 표시하는 패널 스크립트.
    public DashboardManager dashboard;
    public TextMeshProUGUI socText;
    public Slider socSlider;

    void Update()
    {
        if (dashboard == null || dashboard.energyBus == null) 
        {
            return;
        }

        float soc = dashboard.energyBus.batterySOC;
        socText.text = $"{soc:F1} %";
        if (socSlider != null) socSlider.value = soc / 100f;
    }
}
