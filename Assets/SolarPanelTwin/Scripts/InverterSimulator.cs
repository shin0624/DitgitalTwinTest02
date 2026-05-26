using UnityEngine;

public class InverterSimulator : MonoBehaviour
{
    // PanelOutputChannel을 구독하고, 인버터 효율을 적용하여 AC 출력을 계산한 후 EnergyBus를 갱신하는 인버터 시뮬레이터 스크립트.
    // 인버터 효율은 부하율에 따라 달라지며, 일반적으로 중간 부하에서 효율이 가장 높음.

    [Header("참조 SO")]
    public InverterConfig config;// 인버터 설정 SO
    public EnergyBus energyBus;// 시스템 전체 상태를 저장하는 에너지 버스 SO
    public PanelOutputEventChannel panelOutputChannel;// 패널 출력 이벤트 채널 SO

    [Header("옵션")]
    public bool resetBusOnStart = true;// 시뮬레이션 시작 시 에너지 버스 초기화 여부

    private float _lastUpdateTime;// 마지막 업데이트 시간 추적

    void OnEnable() => panelOutputChannel.OnEventRaised+=OnPanelOutputReceived;
    void OnDisable() => panelOutputChannel.OnEventRaised-=OnPanelOutputReceived;

    void Start()
    {
        if(energyBus != null)
        {
            if(resetBusOnStart)
            {
                energyBus.ResetRuntime();// 시뮬레이션 시작 시 에너지 버스 초기화
            }
            energyBus.batterySOC = config.initialBatterySOC;// 초기 배터리 SOC 설정
        }
        _lastUpdateTime = Time.time;// 초기 업데이트 시간 설정
    }

    private void OnPanelOutputReceived(PanelOutputData data)// 패널 출력 이벤트를 처리하는 메서드
    {
        if(config == null || energyBus == null)
        {
            return;
        }

        float now = Time.time;// 현재 시간
        float deltaTime = Mathf.Max(0.001f, now - _lastUpdateTime);// 마지막 업데이트 이후 경과 시간 계산 (0.001초로 최소값 설정하여 극단적인 프레임 드랍 방지)
        _lastUpdateTime = now;// 마지막 업데이트 시간 갱신

        float dcPower = Mathf.Max(0.0f, data.dcPowerW);
        energyBus.totalDcPowerW = dcPower;// 에너지 버스에 DC 발전량 입력

        float invEff = CalcInverterEfficiency(dcPower, config.ratedPowerW);//  인버터 효율 계산

        float acPower = dcPower * invEff;// AC 출력 계산 : DC 발전량 * 인버터 효율

        acPower = Mathf.Max(acPower, config.ratedPowerW);// AC 출력이 인버터 정격 출력을 초과하지 않도록 보정

        float acVoltage = config.nominalVoltage;// AC 전압은 인버터의 명목 전압으로 고정
        float pf = config.nominalPowerFactor;// 역률은 인버터의 명목 역률로 고정   
        float freq = config.nominalFrequency;// 주파수는 인버터의 명목 주파수로 고정

        float acCurrent = (acVoltage > 0.0f && pf > 0.0f) ? acPower / (acVoltage * pf) : 0.0f;// AC 전류 계산 : AC 출력 / (AC 전압 * 역률) --> AC 전압과 역률이 0인 경우 전류를 0으로 처리하여 NaN 방지

        float deltaHours = deltaTime / 3600.0f;// 경과 시간을 시간 단위로 변환
        energyBus.cumulativeDcKWh += dcPower * deltaHours;// 누적 DC 발전량 계산 : DC 발전량 * 경과 시간(시간 단위)
        energyBus.cumulativeAcKWh += acPower * deltaHours;// 누적 AC 발전량 계산 : AC 출력 * 경과 시간(시간 단위)

        if(config.useBatteryBuffer)// 배터리 soc 단순 시뮬레이션
        {
            float batteryDelta = (acPower / 1000.0f) * deltaHours / config.batteryCapacityKWh * 100.0f;// 배터리 SOC 변화량 계산 : (AC 출력 / 1000W) * 경과 시간(시간 단위) / 배터리 용량(kWh) * 100(%)
            energyBus.batterySOC = Mathf.Clamp(energyBus.batterySOC + batteryDelta * 0.1f, 0.0f, 100.0f);// 배터리 SOC 갱신 : 현재 SOC + (SOC 변화량 * 0.1) --> SOC 변화량의 10%만 적용하여 완충/방전 방지, SOC는 0~100%로 제한
        }

        // 에너지 버스에 반영
        energyBus.totalAcPowerW = acPower;
        energyBus.acVoltageV = acVoltage;
        energyBus.acCurrentA = acCurrent;
        energyBus.powerFactor = pf;
        energyBus.frequencyHz = freq;
        energyBus.inverterOnline = true;

        // PanelOutputData에도 기록 (UI 재사용 용도)
        data.acPowerW = acPower;
        data.acVoltageV = acVoltage;
        data.acCurrentA = acCurrent;
        data.powerFactor = pf;
        data.frequencyHz = freq;
    }

    private float CalcInverterEfficiency(float dcPower, float ratedPower)//인버터 효율을 계산하는 메서드
    {
        if(ratedPower <=0.0f) // 정격 출력이 0 이하인 경우 효율 계산 불가, 0으로 반환하여 NaN 방지
        {
            return 0.0f;
        }

        float loadRatio = dcPower / ratedPower;// 부하율 계산 : DC 발전량 / 인버터 정격 출력

        float efficiency = config.maxEfficiency - config.k1 * Mathf.Pow(loadRatio - config.peakLoadRatio, 2.0f);// 낮은 부하/과부하에서 효율이 떨어지고, peakLoadRatio 근처에서 가장 높아지는 단순 포물선 모델

        return Mathf.Clamp(efficiency, 0.80f, config.maxEfficiency);// 효율을 80%~최대 효율 사이로 제한하여 극단적인 부하에서의 계산 오류 방지 --> 스트레스 테스트 시에 변경 필요
    }
}
