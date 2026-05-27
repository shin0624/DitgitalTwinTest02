using UnityEngine;
using TMPro;

public class EfficiencyPanel : MonoBehaviour
{
    // PR, CUF, 인버터 효율, 온도 손실을 표시하는 패널 스크립트
    // 초당 업데이트가 꼭 필요하지는 않으나, 1초 또는 2초 주기로 갱신해도 무방할 듯.
    //자주 바뀌지 않는 값이므로, 캔버스를 별도로 둔다.

    // PR : 패널 출력과 이론적 최대 출력의 비율. 0~1 사이. 패널 데이터 업데이트 시 계산됨.
    // CUF : 패널 출력과 패널 용량의 비율. 0~1 사이. 패널 데이터 업데이트 시 계산됨.

    public DashboardManager dashboard;// 대시보드 매니저 참조. 패널 데이터와 집계값을 읽어와 표시.
    public TextMeshProUGUI prText;// PR 텍스트
    public TextMeshProUGUI cufText;// CUF 텍스트
    public TextMeshProUGUI tempLossText;// 온도 손실 텍스트
    public TextMeshProUGUI inverterEffText;// 인버터 효율 텍스트

    private float _timer;// 패널 데이터 업데이트 간격 타이머
    public float refreshInterval = 1.0f;// 패널 데이터 업데이트 간격

    void Update()
    {
        _timer+=Time.deltaTime;
        if(_timer < refreshInterval)
        {
            return;// 아직 업데이트 간격이 안 됐으면 리턴
        }

        if (dashboard == null || dashboard.latestPanelData == null)
        {
            return;// 대시보드 매니저나 패널 데이터가 없으면 리턴
        }

        prText.text = $"{dashboard.performanceRatio:F1} %";
        cufText.text = $"{dashboard.capacityUtilization:F1} %";
        tempLossText.text = $"{dashboard.latestPanelData.tempLossPct:F1} %";
        inverterEffText.text = $"{dashboard.inverterEfficiency:F1} %";

    }
}
