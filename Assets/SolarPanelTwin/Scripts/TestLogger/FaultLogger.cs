using UnityEngine;

public class FaultLogger : MonoBehaviour
{
    // 패널을 붙이기 전에 먼저 이상 로그를 확인하기 위한 테스트 스크립트.

    public FaultEventChannel faultEventChannel;// 이상 이벤트 채널 참조
    void OnEnable()  => faultEventChannel.OnEventRaised += Log;
    void OnDisable() => faultEventChannel.OnEventRaised -= Log;

    private void Log(FaultData fault)
    {
        Debug.Log($"[ALARM] {fault.severity} | {fault.code} | {fault.message}");
    }

}
