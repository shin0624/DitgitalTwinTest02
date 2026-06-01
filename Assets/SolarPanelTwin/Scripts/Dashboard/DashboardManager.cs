using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DashboardManager : MonoBehaviour
{
    // UI가 여러 SO와 뒤엉키지 않도록, 공통 데이터 공급용 매니저를 두어 대시보드의 데이터들을 집계.
    // EnergyBus와 마지막 PanelOutputData를 읽어 각 위젯이 참조할 수 있게 한다.

    [Header("참조 SO")]
    public EnergyBus energyBus;
    public PanelOutputEventChannel panelOutputChannel;
    public FaultEventChannel faultEventChannel;
    public PanelConfig panelConfig;
    public InverterConfig inverterConfig;

    [Header("런타임 상태")]
    public PanelOutputData latestPanelData; // 대시보드가 참조할 최신 패널 출력 데이터. 패널에서 이벤트로 업데이트됨
    public FaultData latestFault; // 대시보드가 참조할 최신 고장 데이터. 고장 이벤트로 업데이트됨

    [Header("집계값")]
    public float performanceRatio;// (PR) 패널 출력과 이론적 최대 출력의 비율. 0~1 사이. 패널 데이터 업데이트 시 계산됨.
    public float capacityUtilization; // (CUF) 패널 출력과 패널 용량의 비율. 0~1 사이. 패널 데이터 업데이트 시 계산됨.
    public float inverterEfficiency; // (%) 인버터 효율. 패널 출력과 인버터 용량, 인버터 출력 데이터를 기반으로 계산됨.

    private float _startTime;// 대시보드 시작 시간. 패널 데이터 업데이트 시 경과 시간 계산에 사용됨.
    
    void Start()
    {
        _startTime = Time.time;// 대시보드 시작 시간 기록
    }

    void Update()
    {
        UpdateDerivedMetrics();// 집계값 업데이트. 패널 데이터가 업데이트될 때마다 계산됨.
    }

    private void OnEnable()// 이벤트 구독
    {
        panelOutputChannel.OnEventRaised +=OnPanelData;
        faultEventChannel.OnEventRaised +=OnFault;
    }

    private void OnDisable()// 이벤트 구독 해제
    {
        panelOutputChannel.OnEventRaised -=OnPanelData;
        faultEventChannel.OnEventRaised -=OnFault;
    }

    private void OnPanelData(PanelOutputData data)// 패널 데이터 업데이트 이벤트 핸들러
    {
        latestPanelData = data;// 최신 패널 데이터 업데이트
    }

    private void OnFault(FaultData fault)// 고장 데이터 업데이트 이벤트 핸들러
    {
        latestFault = fault;// 최신 고장 데이터 업데이트
    }

    private void UpdateDerivedMetrics()
    {
        if (latestPanelData == null || energyBus == null || panelConfig == null || inverterConfig == null)
        {
            return;// 필요한 데이터나 참조가 없으면 계산하지 않음
        }

        inverterEfficiency = latestPanelData.dcPowerW > 0.0f ? Mathf.Clamp((energyBus.totalAcPowerW / latestPanelData.dcPowerW) * 100.0f, 0.0f, 100.0f) : 0.0f;// 인버터 효율  = (인버터 출력 AC 전력 / 패널 출력 DC 전력) * 100.0f

        float idealDc = latestPanelData.irradiance * panelConfig.panelArea * panelConfig.efficiency;// 이론적 최대 DC 출력 = 일조량 * 패널 면적 * 패널 효율
        
        performanceRatio = idealDc > 0.0f ? Mathf.Clamp((energyBus.totalAcPowerW / idealDc) * 100.0f, 0.0f, 100.0f) : 0.0f;// 패널 출력과 이론적 최대 출력의 비율 = (인버터 출력 AC 전력 / 이론적 최대 DC 출력) * 100.0f

        float elapsedHours = Mathf.Max((Time.time - _startTime) / 3600.0f, 0.0001f);// 경과 시간(시간 단위) 계산. 0으로 나누는 것을 방지하기 위해 작은 값을 사용
        
        capacityUtilization = Mathf.Clamp((energyBus.cumulativeAcKWh / ((inverterConfig.ratedPowerW / 1000.0f) * elapsedHours)) * 100.0f, 0.0f, 100.0f);// 패널 출력과 패널 용량의 비율 = (누적 AC kWh / (인버터 정격 전력 kW * 경과 시간 h)) * 100.0f



    }



}
