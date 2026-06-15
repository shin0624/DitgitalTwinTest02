using UnityEngine;
using TMPro;
using Michsky.UI.Heat;

public class VirtualWeatherController : MonoBehaviour
{
    //UI 슬라이더에서 가상 날씨를 실시간 업데이트하는 스크립트
    
    [Header("SO 참조")]
    [SerializeField] private VirtualWeatherDataSO virtualWeather;

    [Header("슬라이더 UI")]
    [SerializeField] private SliderManager cloudSlider;// 구름량 슬라이더
    [SerializeField] private SliderManager tempOffsetSlider;// 기온 오프셋 슬라이더
    [SerializeField] private SliderManager seasonSlider;// 계절 선택 슬라이더

    [Header("레이블 UI")]
    [SerializeField] private TMP_Text cloudLabel;// 구름량 레이블
    [SerializeField] private TMP_Text tempOffsetLabel;// 기온 오프셋 레이블
    [SerializeField] private TMP_Text seasonLabel;// 계절 선택 레이블
    
    
    
    void Start()
    {
        if (cloudSlider == null || tempOffsetSlider == null || seasonSlider == null)
        {
            Debug.LogWarning("[VirtualWeatherController] HeatUI SliderManager 참조가 비어 있습니다.");
            return;
        }

        if (cloudSlider.mainSlider == null || tempOffsetSlider.mainSlider == null || seasonSlider.mainSlider == null)
        {
            Debug.LogWarning("[VirtualWeatherController] SliderManager.mainSlider 참조를 확인하세요.");
            return;
        }

        // 슬라이더 범위 설정
        cloudSlider.mainSlider.minValue      = 0f;   cloudSlider.mainSlider.maxValue      = 100f;
        tempOffsetSlider.mainSlider.minValue = -10f; tempOffsetSlider.mainSlider.maxValue  = 10f;
        seasonSlider.mainSlider.minValue     = -23.45f; seasonSlider.mainSlider.maxValue   = 23.45f;

        // 초기값 SO 기본값으로 설정
        cloudSlider.mainSlider.value      = virtualWeather.cloudCoverPercent;
        tempOffsetSlider.mainSlider.value = virtualWeather.tempOffsetC;
        seasonSlider.mainSlider.value     = virtualWeather.declinationDeg;
        cloudSlider.UpdateUI();
        tempOffsetSlider.UpdateUI();
        seasonSlider.UpdateUI();

        // 이벤트 연결
        cloudSlider.onValueChanged.AddListener(v => {
            virtualWeather.cloudCoverPercent = v;
            UpdateLabels();
        });
        tempOffsetSlider.onValueChanged.AddListener(v => {
            virtualWeather.tempOffsetC = v;
            UpdateLabels();
        });
        seasonSlider.onValueChanged.AddListener(v => {
            virtualWeather.declinationDeg = v;
            UpdateLabels();
        });

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if(cloudLabel)
        {
            cloudLabel.text = $"구름량 : {virtualWeather.cloudCoverPercent:F0}%";
        }
        if(tempOffsetLabel)
        {
            tempOffsetLabel.text  = $"기온 오프셋 : {virtualWeather.tempOffsetC:+0.0;-0.0;0}°C";
        }
        if(seasonLabel)
        {
            string seasonName = virtualWeather.declinationDeg > 10.0f ? "여름"
 : "봄/가을";
            seasonLabel.text = $"계절: {seasonName} ({virtualWeather.declinationDeg:F1}°)";
        }
    }
}
