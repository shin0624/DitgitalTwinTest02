using UnityEngine;

[System.Serializable]
public class PanelOutputData
{
    public float irradiance;    // W/m²
    public float dcPowerW;      // DC 출력 (W)
    public float voltageV;      // 전압 (V)
    public float currentA;      // 전류 (A)
    public float cellTempC;     // 셀 온도 (°C)
    public float tempLossPct;   // 온도 손실률 (%)
    public float efficiency;    // 현재 실효 효율

    //--대시보드에서 재사용하기 편한 ac 관련 필드 정의
    public float acPowerW;      // AC 출력 (W)
    public float acVoltageV;    // AC 전압 (V)
    public float acCurrentA;    // AC 전류 (A)
    public float powerFactor;   // 역률
    public float frequencyHz;   // 주파수 (Hz)
}
