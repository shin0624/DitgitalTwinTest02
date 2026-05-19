using System.Runtime.InteropServices;
using UnityEngine;

public class WebGLMqttBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void MQTT_Connect(string url, string clientId);

    [DllImport("__Internal")]
    private static extern void MQTT_Publish(string topic, string payload);

    [DllImport("__Internal")]
    private static extern void MQTT_Disconnect();
#endif

    [Header("MQTT Settings")]
    public string brokerWebSocketUrl = "ws://127.0.0.1:9001";
    public string topic = "dt/building/floor1/temp";
    public string clientId = "unity_webgl_temp_sensor";

    public void Connect()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MQTT_Connect(brokerWebSocketUrl, clientId);
#else
        Debug.Log($"[MQTT MOCK] Connect → {brokerWebSocketUrl} / {clientId}");
#endif
    }

    public void Publish(string jsonPayload)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MQTT_Publish(topic, jsonPayload);
#else
        Debug.Log($"[MQTT MOCK] Publish → {topic} / {jsonPayload}");
#endif
    }

    public void Disconnect()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MQTT_Disconnect();
#else
        Debug.Log("[MQTT MOCK] Disconnect");
#endif
    }
}