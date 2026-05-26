using UnityEngine;

public class FaultDetector : MonoBehaviour
{
    // 규칙 기반 이상 감지 스크립트.
    //일조량 대비 출력 저하, 과열, 전압 이상 룰을 우선 간단히 구현하여 모니터링한다. 향후 머신러닝 모델로 대체 가능.
    
    [Header("참조 SO")]
    public PanelConfig panelConfig;
    public EnergyBus energyBus;
    public PanelOutputEventChannel panelOutputEventChannel;
    public FaultEventChannel faultEventChannel;

    [Header("임계값")]
    public float minIrradianceForCheck = 300.0f;// 일조량이 이보다 낮으면 이상 감지 룰을 적용하지 않음 (야간 등)
    public float lowOutputRatio = 0.7f;// 일조량 대비 출력이 이 비율보다 낮으면 이상으로 간주(기대출력 70%)
    public float overheatTempC = 85.0f;// 패널 온도가 이보다 높으면 과열로 간주
    public float voltageTolerance = 0.2f; // 기대 전압 대비 이 비율 이상 벗어나면 전압 이상으로 간주 (±20%)

    private float _lastFaultTime;
    public float faultCooldown = 5.0f;// 이상 감지 후 다음 이상 감지까지 최소 대기 시간(초) -> 같은 고장 조건이 매우 짧은 시간 동안 반복 발생하는 알림피로를 예방하기 위해 설정.

    void OnEnable() => panelOutputEventChannel.OnEventRaised += EvaluateFaults;
    void OnDisable() => panelOutputEventChannel.OnEventRaised -= EvaluateFaults;

    private void EvaluateFaults(PanelOutputData data)
    {
        if(panelConfig == null || faultEventChannel==null || energyBus == null)
        {
            return;
        }
        energyBus.faultActive = false;// 이상 감지 평가 시작 시 이상 상태 초기화

        float expectedPower = data.irradiance * panelConfig.panelArea * panelConfig.efficiency;// 일조량 기반 기대 출력 계산

        if(data.irradiance > minIrradianceForCheck && data.dcPowerW < expectedPower * lowOutputRatio)// 일조량은 충분한데 출력이 낮을 경우
        {
            RaiseFault("LOW_OUTPUT", "출력 저하 감지", "Warning");
        }

        if(data.cellTempC > overheatTempC)// 패널 온도가 과열 임계값을 초과할 경우
        {
            RaiseFault("OVERHEAT", "패널 과열", "Critical");
        }

        float minV = panelConfig.Vmp * (1.0f - voltageTolerance);// 기대 전압의 하한
        float maxV = panelConfig.Vmp * (1.0f + voltageTolerance);// 기대 전압의 상한
        if(data.voltageV < minV || data.voltageV > maxV)// 출력 전압이 기대 범위를 벗어날 경우
        {
            RaiseFault("VOLTAGE_ABNORMAL", "전압 이상", "Warning");
        }
    }

    private void RaiseFault(string code, string message, string severity)// 이상 이벤트를 발생시키는 메서드
    {
        if(Time.time - _lastFaultTime <faultCooldown)
        {
            return;// 마지막 이상 감지 이후 cooldown 시간보다 짧으면 이상 이벤트를 발생시키지 않음(알림 피로 방지)
        }

        energyBus.faultActive = true;

        FaultData fault = new FaultData
        {
          code = code,
          message = message,
          severity = severity,
          timestamp = Time.time  
        };// 이상 데이터 생성

        faultEventChannel.Raise(fault);// 이상 이벤트 채널을 통해 이상 데이터 전달
        Debug.Log($"[FAULT] {severity} | {code} | {message}");
    }
}
