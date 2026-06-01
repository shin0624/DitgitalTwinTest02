using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlarmPanel : MonoBehaviour
{
    // FaultData의 고장 상태를 읽어와 고장 알람을 표시하는 패널 스크립트. 고장 데이터가 업데이트될 때마다 알람 표시 여부를 갱신.
    public DashboardManager dashboard;// 대시보드 매니저 참조. 고장 데이터와 집계값을 읽어와 표시.

    public TextMeshProUGUI codeText;// 고장 코드 텍스트
    public TextMeshProUGUI messageText;// 고장 메시지 텍스트
    public TextMeshProUGUI severityText;// 고장 심각도 텍스트
    public Image alarmBackground;// 알람 배경 이미지. 고장 심각도에 따라 색상 변경

    public Color normalColor = new Color(0.15f, 0.2f, 0.2f, 0.8f);
    public Color warningColor = new Color(0.8f, 0.55f, 0.1f, 0.9f);
    public Color criticalColor = new Color(0.8f, 0.2f, 0.2f, 0.95f);

    void Update()
    {
        if (dashboard == null)
        {
            return;
        }

        if(dashboard.latestFault == null || string.IsNullOrEmpty(dashboard.latestFault.code))
        {
            codeText.text = "NORMAL";
            messageText.text = "No Faults Detected";
            severityText.text = "Info";
            alarmBackground.color = normalColor;
            return;
        }

        codeText.text = dashboard.latestFault.code;
        messageText.text = dashboard.latestFault.message;
        severityText.text = dashboard.latestFault.severity;

        if(dashboard.latestFault.severity == "Critical")
        {
            alarmBackground.color = criticalColor;
        }
        else if(dashboard.latestFault.severity == "Warning")
        {
            alarmBackground.color = warningColor;
        }
        else
        {
            alarmBackground.color = normalColor;
        }
    }
}
