using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class ComparisonEngine : MonoBehaviour
{
    // RealWeatherDataSO, VirtualWeatherDataSO, SiteConfigSO, PanelConfig를 구독하여 실제 날씨-가상 날씨 간 예상 발전량, 수익 비교 지표를 계산하고 UI에 출력하는 스크립트.

    [Header("SO 입력")]
    [SerializeField] private RealWeatherDataSO realWeather;
    [SerializeField] private VirtualWeatherDataSO virtualWeather;
    [SerializeField] private SiteConfigSO siteConfig;
    [SerializeField] private PanelConfig panelConfig;

    [Header("UI 출력 - Real 패널(좌)")]
    [SerializeField] private TMP_Text realIrradianceText; // 실제 일사량
    [SerializeField] private TMP_Text realPowerText;// 예상 발전량
    [SerializeField] private TMP_Text realDailyText; // 예상 일 발전량
    [SerializeField] private TMP_Text realAnnualRevenueText; // 연 수익(만원 단위)

    [Header("UI 출력 - Virtual 패널(우)")]
    [SerializeField] private TMP_Text virtualIrradianceText; // 가상 일사량
    [SerializeField] private TMP_Text virtualPowerText; // 예상 발전량
    [SerializeField] private TMP_Text virtualDailyText; // 예상 일 발전량
    [SerializeField] private TMP_Text virtualAnnualRevenueText; // 연 수익(만원 단위)

    [Header("UI 출력 - 차이(중앙)")]
    [SerializeField] private TMP_Text deltaPowerText;  // Δ발전량
    [SerializeField] private TMP_Text deltaAnnualText;  // Δ연 수익

    [Header("전력 단가")]
    [SerializeField] private float kwhPriceKRW = 120.0f;// 원/kWh(기본값, 수정가능)
    [SerializeField] private float peakHoursPerDay = 3.5f;//발전량 환산용 유효시간(Peak Sun Hours(PSH)) -> ui 비교에서 일/연간 환산치를 빠르게 보여주기 위한 단순화 계수로, 추후 실제 한국 평균 일조시간인 6.0, 또는 이 값을 제거하고 일통계 기반으로 직접 일발전량을 계산하는 것이 좋을 듯


    void Update()
    {
        CalculateAndDisplay();// 매 프레임 계산(API 갱신 주기가 30분이므로, 성능 부담 적음)
    }

    private void CalculateAndDisplay()
    {
        // 실제 값 계산
        float realIrrad = realWeather.GetEffectiveIrradiance();// 실제 일사량
        float realTempC = realWeather.temperatureC;// 실제 온도
        float realPowerW = CalcPanelPowerW(realIrrad, realTempC); // 실제 패널 발전량 (W)
        float realDailyKWh = realPowerW * peakHoursPerDay / 1000.0f * siteConfig.panelCount;// 실제 일 발전량 (kWh)
        float realAnnualRevenue = realDailyKWh * 365.0f * kwhPriceKRW / 10000.0f;// 실제 연 수익 (만원)

        // 가상 값 계산
        float virtualIrrad = realIrrad * virtualWeather.GetCloudFactor();// 가상 일사량 (구름량 보정)
        float virtualTempC = realTempC + virtualWeather.tempOffsetC;// 가상 온도 (오프셋 보정)

        // 계절(적위각)에 따른 일사량 보정 : sin(90도 - |lat - declination|)비율 적용 -> 실제로는 일사량이 적위각에 따라 선형적으로 변하지 않지만, 간단한 비교 지표로서 계절 효과를 반영하기 위해 추가
        float latRad = (float)(siteConfig.latitude * Mathf.Deg2Rad);// 위도 라디안
        float declRad = virtualWeather.declinationDeg * Mathf.Deg2Rad;// 적위각 라디안
        float seasonFactor = Mathf.Max(0.1f, 
                             Mathf.Sin(latRad) * Mathf.Sin(declRad) + 
                             Mathf.Cos(latRad) * Mathf.Cos(declRad));// 계절 보정계수 (최소 0.1로 제한하여 극단적 계절에도 일사량이 완전히 0이 되지 않도록)

        virtualIrrad *= seasonFactor;// 계절 보정 적용

        float virtualPowerW = CalcPanelPowerW(virtualIrrad, virtualTempC);// 가상 패널 발전량 (W)
        float virtualDailyKWh = virtualPowerW * peakHoursPerDay / 1000.0f * siteConfig.panelCount;// 가상 일 발전량 (kWh)
        float virtualAnnualRevenue = virtualDailyKWh * 365.0f * kwhPriceKRW / 10000.0f;// 가상 연 수익 (만원)

        // 실제 vs 가상 값 비교 계산
        float deltaPower = virtualPowerW - realPowerW;// 발전량 차이
        float deltaAnnual = virtualAnnualRevenue - realAnnualRevenue;// 연 수익 차이

        //UI 업데이트
        SetText(realIrradianceText, $"{realIrrad:F1} W/m²");
        SetText(realPowerText, $"{realPowerW:F1} W");
        SetText(realDailyText, $"{realDailyKWh:F2} kWh/일");
        SetText(realAnnualRevenueText, $"{realAnnualRevenue:F1} 만원/년");

        SetText(virtualIrradianceText, $"{virtualIrrad:F1} W/m²");
        SetText(virtualPowerText, $"{virtualPowerW:F1} W");
        SetText(virtualDailyText,  $"{virtualDailyKWh:F2} kWh/일");
        SetText(virtualAnnualRevenueText, $"{virtualAnnualRevenue:F1} 만원/년");

        string sign = deltaPower >= 0 ? "+" : "";
        SetText(deltaPowerText,  $"Δ발전량 {sign}{deltaPower:F1} W");
        SetText(deltaAnnualText, $"Δ수익 {sign}{deltaAnnual:F1} 만원/년");
    
    }
    
    private float CalcPanelPowerW(float irradianceWm2, float tempC)// 단일 패널의 DC 전력을 계산하는 메서드(PanelConfig의 온도 계수를 반영)
    {
        if(irradianceWm2 <= 0.0f)
        {
            return 0.0f;// 일사량이 0 이하이면 발전량도 0
        }

        float cellTemp = tempC + irradianceWm2 * panelConfig.thermalCoeff;// 셀 온도 = 주변 온도 + (복사량 * 열계수)

        float effCorr = panelConfig.efficiency + panelConfig.tempCoeff * (cellTemp - 25.0f);// 온도에 따른 효율 보정(25°C 기준)
        effCorr = Mathf.Max(0.0f, effCorr);// 효율이 음수가 되지 않도록 보정

        return irradianceWm2 * panelConfig.panelArea * effCorr;// 발전량 = 일사량 * 패널 면적 * 효율
    }


    private void SetText(TMP_Text label, string value)// null 체크 후 텍스트를 업데이트하는 메서드
    {
        if(label != null)
        {
            label.text = value;
        }
    }



}
