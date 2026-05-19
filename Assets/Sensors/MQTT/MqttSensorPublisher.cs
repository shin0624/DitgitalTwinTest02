using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
public class MqttSensorPublisher : MonoBehaviour
{
    [Header("발행 설정")]
    public SensorData sensorData;
    public string topic = "dt/building/floor1/temp";//발행할 MQTT 토픽

    void OnEnable()
    {
        sensorData.OnValueChanged += Publish;// 센서 데이터가 변경될 때마다 Publish 메서드 호출
    }

    void OnDisable()
    {
        sensorData.OnValueChanged -= Publish;
    }

    private void Publish(float value)
    {
        if(MqttManager.Instance == null || !MqttManager.Instance.IsConnected)// mqttmanager 싱글톤이 없거나 연결이 안된 경우 스킵
        {
            return;
        }

        string json = $"{{\"sensor\":\"temp_01\",\"value\":{value:F2}," + $"\"unit\":\"celsius\",\"ts\":{Time.time:F1}}}";// JSON 형식으로 센서 데이터 포맷팅 (소수점 2자리로 값 표현, 타임스탬프는 소수점 1자리로 표현)
        byte[] payload = Encoding.UTF8.GetBytes(json);// JSON 문자열을 UTF-8 바이트 배열로 인코딩

        MqttManager.Instance.Client.Publish(
            topic, payload, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, retain : false
        );// MQTT 브로커에 메시지 발행 (토픽, 페이로드, QoS 레벨, retain 플래그)
        
    }
}
