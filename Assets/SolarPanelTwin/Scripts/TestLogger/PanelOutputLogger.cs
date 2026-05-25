using UnityEngine;

public class PanelOutputLogger : MonoBehaviour
{
    public PanelOutputEventChannel channel;
    void OnEnable() => channel.OnEventRaised+=Log;
    void OnDisable() => channel.OnEventRaised-=Log;


    void Log(PanelOutputData d)
    {
        Debug.Log($"[Panel] {d.dcPowerW:F1}W | {d.voltageV:F1}V | {d.currentA:F2}A" +
                  $" | 셀온도 {d.cellTempC:F1}°C | 손실 {d.tempLossPct:F1}% | 효율 {d.efficiency:P1}");
    }
}
