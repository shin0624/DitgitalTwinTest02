using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VirtualWeatherController : MonoBehaviour
{
    //UI 슬라이더에서 가상 날씨를 실시간 업데이트하는 스크립트
    
    [Header("SO 참조")]
    [SerializeField] private VirtualWeatherDataSO virtualWeather;

    [Header("슬라이더 UI")]
    [SerializeField] private Slider cloudSlider;// 구름량 슬라이더
    [SerializeField] private Slider tempOffsetSlider;// 기온 오프셋 슬라이더
    [SerializeField] private Slider seasonSlider;// 계절 선택 슬라이더

    [Header("레이블 UI")]
    [SerializeField] private TMP_Text cloudLabel;// 구름량 레이블
    [SerializeField] private TMP_Text tempOffsetLabel;// 기온 오프셋 레이블
    [SerializeField] private TMP_Text seasonLabel;// 계절 선택 레이블
    
    
    
    void Start()
    {
        // 슬라이더 범위 설정
        cloudSlider.minValue      = 0f;   cloudSlider.maxValue      = 100f;
        tempOffsetSlider.minValue = -10f; tempOffsetSlider.maxValue  = 10f;
        seasonSlider.minValue     = -23.45f; seasonSlider.maxValue   = 23.45f;

        // 초기값 SO 기본값으로 설정
        cloudSlider.value      = virtualWeather.cloudCoverPercent;
        tempOffsetSlider.value = virtualWeather.tempOffsetC;
        seasonSlider.value     = virtualWeather.declinationDeg;

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
