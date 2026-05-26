using UnityEngine;

[CreateAssetMenu (menuName = "SolarSim/PanelConfig")]
public class PanelConfig : ScriptableObject
{
    // 일조량에서 DC 전력을 계산하고, 패널 오브젝트의 CellSurface의 Emission을 제어하기 위한 정보를 담는 SO

    [Header("패널 물리 스펙")] // 아래 변수들은 패널의 물리적 특성을 정의하는 변수들로, 일조량에서 실제 발전량을 계산하는 데 사용됨
    //--> 가정 값을 Default로 설정하며, SO이기 때문에 테스트베드 환경에 맞추어 언제든지 조정 가능
    public float panelArea = 1.7f; // 제곱미터(1.0 * 1.65)
    public float efficiency = 0.20f;// 태양 복사량에서 DC 전력으로 변환하는 효율(20% 가정)
    public float tempCoeff = -0.004f;// 온도에 따른 효율 감소 계수(1도 상승 시 효율 0.4% 감소 가정)
    public float betaV = 0.003f; // 온도에 따른 전압 감소 계수(1도 상승 시 전압 0.3% 감소 가정)
    public float thermalCoeff = 0.03f;// 태양 복사량에서 패널 온도 상승을 계산하는 계수(복사량 1000W/m^2에서 30도 상승 가정)

    [Header("전기 스펙(STC 기준)")]
    public float Vmp = 40.0f;// 최대 전력점에서의 전압(V)
    public float Imp = 8.5f;// 최대 전력점에서의 전류(A)
    public float ratedPowerW = 340.0f;// 정격 출력(W) = Vmp * Imp

    [Header("환경")]
    public float ambientTemp = 25.0f;// 주변 온도(°C)

}
