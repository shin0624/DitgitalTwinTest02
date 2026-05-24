using UnityEngine;


public class IrradianceLogger : MonoBehaviour
{
    // SunController가 제대로 동작하는지 확인하기 위한 임시 리스너

    public FloatEventChannel irradianceChannel;// SunController에서 일조량을 전달받기 위한 이벤트 채널 참조
    void OnEnable() => irradianceChannel.OnEventRaised += Log;
    void OnDisable() => irradianceChannel.OnEventRaised -= Log;

    void Log(float value)
    {
        Debug.Log($"[Irradiance] {value:F1} W/m²");
    }

}
