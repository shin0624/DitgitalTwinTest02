using UnityEngine;

[CreateAssetMenu(menuName = "SolarSim/InverterConfig")]
public class InverterConfig : ScriptableObject
{
    // 에너지 관리 시스템(EMS) 중 인버터의 정격과 효율, 배터리 및 에너지 버스 관련 정보를 담는 SO
    // 일반적인 인버터는 40~80% 부하에서 최고 효율에 도달하며, 94~98%의 효율 범위를 갖는다.
    // 가상 인버터에서는 부하율에 따른 포물선 효율 곡선을 근사식으로 구현할 것.
    // 태양광 설비의 여러 패널들은 인버터와 데이터 버스로 연결될 것.
    [Header("인버터 정격")]
    public float ratedPowerW = 500.0f;// 정격 AC 출력(W)
    public float maxEfficiency = 0.97f;// 인버터 최고 효율
    public float peakLoadRatio = 0.6f;// 최고 효율이 나는 부하율 (예 : 60% 부하에서 최고 효율)
    public float k1 = 0.25f;// 효율 곡선의 포물선 계수 (부하율이 낮을 때 효율 감소 정도)

    [Header("출력 전기 정보")]
    public float nominalVoltage = 220.0f;// 명목 AC 전압(V)
    public float nominalFrequency = 60.0f;// 명목 AC 주파수(Hz)
    public float nominalPowerFactor = 0.98f;// 명목 역률(역률이란, 실제 전력과 겉보기 전력의 비율로, 1에 가까울수록 효율적)

    [Header("배터리 및 버스")]
    public bool useBatteryBuffer = true;// 배터리 버퍼 사용 여부 (태양광 발전이 일시적으로 감소할 때 버퍼링 역할)
    public float batteryCapacityKWh = 2.0f;// 배터리 용량(kWh)
    public float initialBatterySOC = 80.0f;// 초기 배터리 충전 상태(SOC, %)


}
