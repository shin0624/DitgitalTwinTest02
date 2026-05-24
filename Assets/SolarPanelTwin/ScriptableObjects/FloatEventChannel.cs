using UnityEngine;

[CreateAssetMenu (menuName = "Events/FloatEventChannel")]
public class FloatEventChannel : ScriptableObject
{
    public event System.Action<float> OnEventRaised; // 이벤트 구독자를 위한 이벤트
    public void Raise(float value) => OnEventRaised?.Invoke(value);// 이벤트 발생 시 구독자에게 알림
}
