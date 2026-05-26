using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName ="SolarSim/EnergyBus")]
public class EnergyBus : ScriptableObject
{
    // 시스템 전체가 공유하는 런타임 상태 저장소

    [Header("실시간 전력")]
    public float totalDcPowerW;// 태양광 패널에서 생성된 총 DC 전력(W)
    public float totalAcPowerW;// 인버터에서 변환된 총 AC 전력(W)

    [Header("누적 에너지")]
    public float cumulativeDcKWh;// 누적 DC 에너지(kWh)
    public float cumulativeAcKWh;// 누적 AC 에너지(kWh)

    [Header("전기적 정보")]
    public float acVoltageV;// AC 전압(V)
    public float acCurrentA;// AC 전류(A)
    public float powerFactor;// 역률
    public float frequencyHz;// 주파수(Hz)

    [Header("배터리")]
    public float batterySOC;// 배터리 충전 상태(SOC, %)

    [Header("상태")]
    public bool inverterOnline = true;// 인버터 온라인 여부
    public bool faultActive = false;// 고장 상태 여부

    public void ResetRuntime()// 시뮬레이션 시작 시 런타임 상태 초기화
    {
        totalDcPowerW = 0f;
        totalAcPowerW = 0f;
        cumulativeDcKWh = 0f;
        cumulativeAcKWh = 0f;
        acVoltageV = 0f;
        acCurrentA = 0f;
        powerFactor = 0f;
        frequencyHz = 0f;
        faultActive = false;
    }
}
