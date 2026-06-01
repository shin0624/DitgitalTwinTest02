using TMPro;
using UnityEngine;

public class SunPanel : MonoBehaviour
{
    // 현재 일조량, 셀 온도를 보여주는 패널 스크립트.
    public DashboardManager dashboard;

    public TextMeshProUGUI irradianceText;
    public TextMeshProUGUI cellTempText;
    public TextMeshProUGUI statusText;
    public SunController sunController;

    void Update()
    {
        if (dashboard == null || dashboard.latestPanelData == null) 
        {
            return;
        }
        irradianceText.text = $"{dashboard.latestPanelData.irradiance:F0} W/m²";
        cellTempText.text = $"{dashboard.latestPanelData.cellTempC:F1} °C";
        IrradianceStatus(dashboard.latestPanelData.irradiance);
    }

    private void IrradianceStatus(float irradiance)
    {
       if(sunController.currentWeather == WeatherState.Night)
        {
            statusText.text = "NIGHT - NO SUN";
        }
        else if(sunController.currentWeather == WeatherState.PartiallyCloudy)
        {
            statusText.text = "DAY - CLOUDY";
        }
        else if(sunController.currentWeather == WeatherState.Overcast)
        {
            statusText.text = "DAY - OVERCAST";
        }
        else if(sunController.currentWeather == WeatherState.RainyOrHeavyFog)
        {
            statusText.text = "DAY - RAINY/FOG";
        }
        else
        {
            statusText.text = "DAY - CLEAR";
        }
    }
}
