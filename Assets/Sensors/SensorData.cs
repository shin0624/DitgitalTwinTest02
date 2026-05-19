using UnityEngine;
using System;

[CreateAssetMenu(menuName = "DT/SensorData")]
public class SensorData : ScriptableObject 
{
    [Tooltip("현재 센서 값")]
    public float value;
    [Tooltip("센서 값이 업데이트된 시간")]
    public float timestamp; 
    [Tooltip("값 변경 이벤트")]
    public event Action<float> OnValueChanged; 

    public void SetValue(float newValue)
    {
        value = newValue;
        timestamp = Time.time;
        OnValueChanged?.Invoke(value);
    }
}
