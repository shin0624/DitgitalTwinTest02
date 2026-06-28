using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

public class VirtualWeatherController : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private VirtualWeatherDataSO virtualWeather;

    [Header("슬라이더 UI (HeatUI)")]
    [SerializeField] private SliderManager cloudSlider;
    [SerializeField] private SliderManager tempOffsetSlider;

    [Header("계절 탭 버튼 (봄 여름 가을 겨울 순서)")]
    [SerializeField] private Button[]   seasonButtons;      // 4개 Button
    [SerializeField] private Image[]    seasonButtonBgs;    // 각 버튼 배경 Image
    [SerializeField] private TMP_Text[] seasonButtonLabels; // 각 버튼 텍스트

    [Header("레이블 UI")]
    [SerializeField] private TMP_Text cloudLabel;
    [SerializeField] private TMP_Text tempOffsetLabel;

    // 활성/비활성 색상
    private static readonly Color ActiveBg     = new Color(0f,    0.831f, 1f,    0.13f); // rgba(0,212,255,0.13)
    private static readonly Color InactiveBg   = new Color(1f,    1f,    1f,    0.02f);
    private static readonly Color ActiveBorder = new Color(0f,    0.831f, 1f,    0.42f);
    private static readonly Color ActiveText   = new Color(0f,    0.831f, 1f,    1f);    // #00d4ff
    private static readonly Color InactiveText = new Color(0.533f,0.600f, 0.733f,1f);   // #8899bb

    void Start()
    {
        InitCloudSlider();
        InitTempSlider();
        InitSeasonButtons();
        SetSeason(virtualWeather.seasonIndex);
        UpdateLabels();
    }

    private void InitCloudSlider()
    {
        if (cloudSlider?.mainSlider == null) return;
        cloudSlider.mainSlider.minValue = 0f;
        cloudSlider.mainSlider.maxValue = 100f;
        cloudSlider.mainSlider.value    = virtualWeather.cloudCoverPercent;
        cloudSlider.UpdateUI();
        cloudSlider.onValueChanged.AddListener(v =>
        {
            virtualWeather.cloudCoverPercent = v;
            UpdateLabels();
        });
    }

    private void InitTempSlider()
    {
        if (tempOffsetSlider?.mainSlider == null) return;
        tempOffsetSlider.mainSlider.minValue = -20f;
        tempOffsetSlider.mainSlider.maxValue =  20f;
        tempOffsetSlider.mainSlider.value    = virtualWeather.tempOffsetC;
        tempOffsetSlider.UpdateUI();
        tempOffsetSlider.onValueChanged.AddListener(v =>
        {
            virtualWeather.tempOffsetC = v;
            UpdateLabels();
        });
    }

    private void InitSeasonButtons()
    {
        if (seasonButtons == null) return;
        for (int i = 0; i < seasonButtons.Length; i++)
        {
            if (seasonButtons[i] == null) continue;
            int idx = i;
            seasonButtons[i].onClick.AddListener(() => SetSeason(idx));
        }
    }

    private void SetSeason(int index)
    {
        virtualWeather.SetSeason(index);
        RefreshSeasonTabs(index);
        UpdateLabels();
    }

    private void RefreshSeasonTabs(int active)
    {
        for (int i = 0; i < (seasonButtons?.Length ?? 0); i++)
        {
            bool isActive = i == active;

            if (seasonButtonBgs != null && i < seasonButtonBgs.Length && seasonButtonBgs[i])
                seasonButtonBgs[i].color = isActive ? ActiveBg : InactiveBg;

            if (seasonButtonLabels != null && i < seasonButtonLabels.Length && seasonButtonLabels[i])
                seasonButtonLabels[i].color = isActive ? ActiveText : InactiveText;
        }
    }

    private void UpdateLabels()
    {
        if (cloudLabel)
            cloudLabel.text = $"구름량 : {virtualWeather.cloudCoverPercent:F0}%";

        if (tempOffsetLabel)
        {
            float t = virtualWeather.tempOffsetC;
            string sign = t >= 0 ? "+" : "";
            tempOffsetLabel.text = $"온도 편차 : {sign}{t:F1}°C";
        }
    }
}
