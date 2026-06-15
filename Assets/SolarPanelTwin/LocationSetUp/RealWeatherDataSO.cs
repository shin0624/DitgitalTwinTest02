using UnityEngine;
using System;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "RealWeatherData", menuName = "LocationSetUp/RealWeatherDataSO")]
public class RealWeatherDataSO : ScriptableObject
{
    //3번 API : 관측-통계 묶음형 (일사량, mode=si)
    [Header("실시간 일사 (3번 API - nph-sun_sfc_sts_pkg, mode=si)")]
    [Tooltip("전천일사 W/m² — 3번 API 응답값 (프록시에서 단위 확인 후 변환)")]
    public float irradianceWm2  = 0f;

    [Tooltip("기온 °C — 3번 API 응답에 포함된 경우")]
    public float temperatureC   = 25f;

    [Tooltip("풍속 m/s — 3번 API 응답에 포함된 경우")]
    public float windSpeedMs    = 0f;

    // 4번 API : 천리안2A 위성 AI (30분 간격, UTC 기준) 
    [Header("발전량 예측 (4번 API - nph_sun_sat_ana_txt)")]
    [Tooltip("향후 최대 48개(24시간 * 30분) 예측 일사량 W/m²")]
    public float[] forecastIrradianceWm2 = new float[48];

    [Tooltip("예측 기준 시각 UTC HH")]
    public int forecastBaseHourUtc = 0;

    // 1번 API : 일통계 (전날 확정값)
    [Header("전일 실적 (1번 API - sun_sfc_day.php)")]
    [Tooltip("전천일사 일합계 MJ/m²")]
    public float dailySumIrradMJm2 = 0f;

    [Tooltip("일조시간 hr")]
    public float dailySunshineHr   = 0f;
    [Tooltip("전일 평균 기온 °C")]
    public float dailyTaAvgC       = 25f;  // TA_AVG °C

    // 메타 
    [Header("상태")]
    public string lastUpdatedAt = "";
    public bool   isDataFresh   = false;

    public float GetEffectiveIrradiance() => irradianceWm2 > 0f ? irradianceWm2 : 0f;
}

