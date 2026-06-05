using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class BatteryPanel : MonoBehaviour
{
    //배터리 soc를 표시하는 패널 스크립트.
    public DashboardManager dashboard;
    public TextMeshProUGUI socText;
    // public Slider socSlider;
    public ProgressBar socProgressBar;

    void Update()
    {
        if (dashboard == null || dashboard.energyBus == null) 
        {
            return;
        }

        float soc = dashboard.energyBus.batterySOC;
        socText.text = $"{soc:F1} %";
        //if (socSlider != null) socSlider.value = soc / 100f;
        if (socProgressBar != null) socProgressBar.currentValue = soc;
        socProgressBar.UpdateUI();
    }
}
