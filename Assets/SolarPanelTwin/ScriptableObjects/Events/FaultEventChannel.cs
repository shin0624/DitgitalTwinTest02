using UnityEngine;

[CreateAssetMenu(menuName ="Events/FaultEventChannel")]
public class FaultEventChannel : ScriptableObject
{
    // 고장 이벤트를 전달하는 채널 역할을 하는 SO
    public event System.Action<FaultData> OnEventRaised;// 고장 이벤트가 발생했을 때 구독자들에게 전달하는 델리게이트
    public void Raise(FaultData data) => OnEventRaised?.Invoke(data);// 고장 이벤트 발생 시 구독자들에게 데이터 전달
}
