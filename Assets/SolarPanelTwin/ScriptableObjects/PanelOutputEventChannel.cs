using UnityEngine;


[CreateAssetMenu(menuName ="Events/PanelOutputEventChannel")]
public class PanelOutputEventChannel : ScriptableObject
{
    public event System.Action<PanelOutputData> OnEventRaised;// 패널 출력 데이터를 전달하는 이벤트 채널
    public void Raise(PanelOutputData data) => OnEventRaised?.Invoke(data);// 이벤트 발생 시 구독자들에게 데이터 전달
}
