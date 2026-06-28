using UnityEngine;

[CreateAssetMenu(fileName = "VirtualWeatherData", menuName = "LocationSetUp/VirtualWeatherDataSO")]
public class VirtualWeatherDataSO : ScriptableObject
{
    [Header("계절 선택 (0=봄 1=여름 2=가을 3=겨울)")]
    [Range(0, 3)] public int seasonIndex = 0;

    [Header("가상 날씨 파라미터")]
    [Range(0.0f, 100.0f)]
    [Tooltip("구름량 0% = 맑음, 100% = 완전히 흐림")]
    public float cloudCoverPercent = 0.0f;

    [Range(-20.0f, 20.0f)]
    [Tooltip("기온 편차(°C) ±20")]
    public float tempOffsetC = 0.0f;

    // SunController 호환용: SetSeason() 호출 시 자동 갱신
    [Tooltip("춘분/추분=0, 하지=23.45, 동지=-23.45")]
    public float declinationDeg = 0.0f;

    // 서울 계절별 평균 일사량 (kWh/m²/day): 봄 여름 가을 겨울
    private static readonly float[] SeasonalIrradiance  = { 4.0f, 5.2f, 3.8f, 2.8f };
    private static readonly float[] SeasonalDeclination = { 0.0f, 23.45f, 0.0f, -23.45f };

    public float GetCloudFactor() => Mathf.Lerp(1.0f, 0.15f, cloudCoverPercent / 100.0f);

    public float GetSeasonIrradiance() => SeasonalIrradiance[Mathf.Clamp(seasonIndex, 0, 3)];

    public void SetSeason(int index)
    {
        seasonIndex    = Mathf.Clamp(index, 0, 3);
        declinationDeg = SeasonalDeclination[seasonIndex];
    }
}
