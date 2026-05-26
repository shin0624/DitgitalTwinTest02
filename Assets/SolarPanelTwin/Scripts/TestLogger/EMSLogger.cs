using UnityEngine;

public class EMSLogger : MonoBehaviour
{
    // 패널을 붙이기 전에 먼저 ems 로그를 확인하기 위한 테스트 스크립트.
    public EnergyBus energyBus;
    public float logInterval = 1.0f;
    private float _timer;

    void Update()
    {
        if (energyBus == null)
        {
            return;
        }

        _timer +=Time.deltaTime;
        if(_timer < logInterval)
        {
            return;
        }
        _timer = 0.0f;


        /*
        정상 로그 상태 
        - 낮에 energyBus.totalAcPower가 0보다 커야 함
        - AC < DC 관계가 유지되어야 함(인버터 효율이 100%를 초과할 수 없으므로)
        - cumulativeAcKWh가 시간이 지날수록 증가해야 함(발전량이 누적되어야 하므로)
        - acVoltageV ≈ 220, frequencyHz ≈ 60, powerFactor ≈ 0.98 정도가 나와야 함
        - 배터리 SOC는 config.useBatteryBuffer가 true인 경우에만 변화하며, 0~100% 사이에서 변화해야 함
        - 과열/출력저하 조건을 강제로 주면 Fault 로그가 발생함
        
        */
         Debug.Log(
            $"[EMS] DC {energyBus.totalDcPowerW:F1}W | " +
            $"AC {energyBus.totalAcPowerW:F1}W | " +
            $"누적 {energyBus.cumulativeAcKWh:F4}kWh | " +
            $"V {energyBus.acVoltageV:F1} | I {energyBus.acCurrentA:F2} | " +
            $"PF {energyBus.powerFactor:F2} | Hz {energyBus.frequencyHz:F1} | " +
            $"SOC {energyBus.batterySOC:F1}%"
        );

    }
}
