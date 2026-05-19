using UnityEngine;

public class TemperatureSensor : MonoBehaviour
{
    public WebGLMqttBridge mqttBridge;

    [Header("센서 설정")]
    public SensorData sensorData;
    public float updateInterval = 1.0f;
    public float baseTemperature = 22.0f;
    public float noiseStdDev = 0.3f;   // 가우시안 노이즈 표준편차
    public float driftRate = 0.01f;    // 시간에 따른 드리프트 비율
    public GaussianNoise _gaussianNoise;

    private float _timer;
    private float _drift; // 누적 드리프트값

    void Start()
    {
        // WebGL MQTT 브리지 연결 시작
        if (mqttBridge != null)
        {
            mqttBridge.Connect();
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;

        // 업데이트 주기 도달 전까지 대기
        if (_timer < updateInterval)
            return;

        _timer = 0.0f;

        // 가우시안 노이즈 생성
        float noise = _gaussianNoise.GenerateGaussianNoise(0.0f, noiseStdDev);

        // 랜덤 드리프트 (실제 센서의 열 팽창 등 시계열 특성 모사)
        _drift += Random.Range(-driftRate, driftRate);

        // 드리프트 상한 제한
        _drift = Mathf.Clamp(_drift, -2.0f, 2.0f);

        // 시뮬레이트된 온도값 계산
        float simulatedTemp = baseTemperature + _drift + noise;

        // 센서 데이터 업데이트
        if (sensorData != null)
        {
            sensorData.SetValue(simulatedTemp);
        }

        // MQTT로 보낼 JSON 문자열 생성
        string json =
            "{"
            + "\"sensor\":\"temp_01\","
            + "\"value\":" + simulatedTemp.ToString("F2") + ","
            + "\"unit\":\"celsius\","
            + "\"ts\":" + Time.time.ToString("F1")
            + "}";

        // WebGL 브라우저의 mqtt.js 브리지로 publish
        if (mqttBridge != null)
        {
            mqttBridge.Publish(json);
        }
    }

    void OnApplicationQuit()
    {
        // 앱 종료 시 MQTT 연결 해제
        if (mqttBridge != null)
        {
            mqttBridge.Disconnect();
        }
    }
}