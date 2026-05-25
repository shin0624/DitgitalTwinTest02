using Unity.Mathematics;
using UnityEngine;

public class SolarPanelSimulator : MonoBehaviour
{
    // IrradianceChannel을 구독하고 발전량을 계산한 뒤, 이를 panelOutputChannel으로 브로드캐스트하는 스크립트.
    //Emission 제어도 이곳에서 처리.

    [Header("참조 SO")]
    public PanelConfig config;// 패널의 물리적 특성과 전기적 스펙을 담는 SO
    public FloatEventChannel irradianceChannel;// 태양 복사량을 전달받는 이벤트 채널
    public PanelOutputEventChannel panelOutputChannel;// 패널 출력 데이터를 전달하는 이벤트 채널

    [Header("Emission 시각화")]
    public Renderer cellRenderer; // CellSurface의 Renderer
    public int cellMaterialIndex = 0;// 머티리얼 슬롯 인덱스
    private Material _cellMat;// CellSurface의 머티리얼 인스턴스

    private PanelOutputData _output = new PanelOutputData();// 패널 출력 데이터를 담는 객체
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");// 머티리얼의 Emission 색상 프로퍼티 ID

    void OnEnable() => irradianceChannel.OnEventRaised+=OnIrradianceReceived;
    void OnDisable() => irradianceChannel.OnEventRaised-=OnIrradianceReceived;


    void Start()
    {
        if(cellRenderer!=null)
        {
            _cellMat = cellRenderer.materials[cellMaterialIndex];// 머티리얼 인스턴스 확보(다른 패널과 공유 방지)
        }
    }

    private void OnIrradianceReceived(float irradiance)// 일조량을 전달받았을 때 발전량 계산 및 이벤트를 발생시키는 메서드
    {
        float cellTemp = config.ambientTemp + (irradiance * config.thermalCoeff); // 셀 온도 계산 : 주변 온도 + (일조량 * 온도 상승 계수)

        // 온도보정 계수 ftemp = 1 + alpha * (T_cell - T_ref) --> 온도 손실 계수는 음수이므로, 25도에서 효율이 최대가 되도록 보정하기 위해 1을 더함
        float fTemp = 1.0f  +config.tempCoeff * (cellTemp - 25.0f);// 온도 보정 계수 계산 : 1 + 온도 손실 계수(=온도 효율 감소 계수) * (셀 온도 - 25도 기준 온도) 
        fTemp = Mathf.Clamp(fTemp, 0.5f, 1.2f);// 온도 보정 계수를 0.5~1.2 사이로 제한하여 극단적인 온도에서의 계산 오류 방지 --> 스트레스 테스트 시에 변경 필요
    
        // 태양광 패널이 생산한 DC(직류) 전력 계산 공식 : P(W) = 일조량(G, W/m²) * 패널 면적(A, m²) * 효율(η) * 온도 보정 계수(fTemp)
        float dcPower = irradiance  *config.panelArea * config.efficiency * fTemp;
        dcPower = Mathf.Max(0.0f, dcPower);// 발전량이 음수가 되는 경우 0으로 보정

        float voc = config.Vmp * (1.0f - config.betaV * (cellTemp - 25.0f));// 온도에 따른 개방 전압(Voc) 계산 : Voc = Vmp * (1 - 온도에 따른 전압 감소 계수 * (셀 온도 - 25도 기준 온도))
        float isc = config.Imp * (irradiance / 1000.0f);// 단락 전류 계산 : Isc = Imp * (일조량 / 1000W/m²) --> 일조량이 1000W/m²일 때 최대 전류가 Imp가 되도록 선형 보정
        voc  = Mathf.Max(0.0f, voc);
        isc = Mathf.Max(0.0f, isc);

        float tempLoss = config.tempCoeff * (cellTemp - 25.0f) * 100.0f;// 온도 손실률(%) 계산 : 온도 손실 계수 * (셀 온도 - 25도 기준 온도) * 100

        float effectiveEff = (irradiance > 0.0f) ? (dcPower / (irradiance * config.panelArea)) : 0.0f; // 실효 효율 계산 : 발전량 / (일조량 * 패널 면적) --> 일조량이 0인 경우 효율을 0으로 처리하여 NaN 방지

        // 계산된 데이터들을 PanelOutputData 객체에 패킹
        _output.irradiance = irradiance;
        _output.dcPowerW = dcPower;
        _output.voltageV = voc;
        _output.currentA = isc;
        _output.cellTempC = cellTemp;
        _output.tempLossPct =  tempLoss;
        _output.efficiency = effectiveEff;

        panelOutputChannel.Raise(_output);// 계산된 패널 출력 데이터를 이벤트 채널을 통해 전달(EMS로 브로드캐스트)

        UpdateEmission(dcPower);// 발전량에 따라 패널의 Emission 색상 업데이트
    }

    private void UpdateEmission(float dcPower)// 발전량에 따라 패널의 Emission 색상을 업데이트하는 메서드
    {
        if(_cellMat == null)
        {
            return;
        }
        float t = Mathf.Clamp01(dcPower / config.ratedPowerW); // 정격 출력 대비 현재 출력의 비율을 0~1 사이로 계산

        Color emission = new Color(0.0f, 0.05f * t, 0.4f * t) * (t * 2.0f);// 발전 비율에 따라 Emission 세기 조절
        _cellMat.SetColor(EmissionColorID, emission);
    }
}
