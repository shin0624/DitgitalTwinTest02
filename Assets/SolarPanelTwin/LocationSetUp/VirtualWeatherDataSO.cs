using UnityEditor.EditorTools;
using UnityEngine;


[CreateAssetMenu(fileName = "VirtualWeatherData", menuName = "LocationSetUp/VirtualWeatherDataSO")]
public class VirtualWeatherDataSO : ScriptableObject
{
    [Header("가상 날씨 파라미터")]
    [Range(0.0f, 100.0f)]
    [Tooltip("구름량 0% = 맑음, 100% = 완전히 흐림")]
    public float cloudCoverPercent = 0.0f; // 구름량 (0% ~ 100%)

    [Range(-10.0f, 10.0f)]
    [Tooltip("기온 오프셋(°C)")]
    public float tempOffsetC = 0.0f;// 기온 오프셋 (°C)

    [Header("계절 선택(적위각)")]
    [Tooltip("춘분/추분 = 0, 하지 = 23.45, 동지 = -23.45")]
    public float declinationDeg = 0.0f; // 계절 선택(적위각)

    public float GetCloudFactor() => Mathf.Lerp(1.0f, 0.1f, cloudCoverPercent / 100.0f);// 구름 양으로 irradiance 보정계수 계산 (구름 0% = 1.0, 구름 100% = 0.1)
}
