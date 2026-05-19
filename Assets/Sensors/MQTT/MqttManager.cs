using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using TMPro;
public class MqttManager : MonoBehaviour
{
    public static MqttManager Instance { get; private set; }

    [Header("브로커 설정")]
    public string brokerHost = "127.0.0.1";

    [Header("클라이언트")]
    public MqttClient Client { get; private set; }
    public bool IsConnected => Client!= null && Client.IsConnected;

    public TextMeshProUGUI portLabel;
    
    [SerializeField] private int tcpPort = 1883;// 에디터는 TCP 1883, WebGL은 WebSocket 9001을 자동 선택
    [SerializeField] private int wsPort = 9001;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        //WebGL인 경우 WebSocket 포트 사용, 그 이외에는 TCP 포트 사용

            int port  = wsPort;
        
            Client = new MqttClient(brokerHost, port, false, null, null, MqttSslProtocols.None); //mqtt 클라이언트 생성(브로커 주소, 포트, SSL 사용 여부, 인증서, 사용자 이름, 패스워드)

            string clientId = "UnityD/T_" + System.Guid.NewGuid().ToString("N").Substring(0, 8); // 클라이언트 ID 생성(고유한 ID를 생성하여 브로커에 연결) -> GUID를 사용하여 고유한 ID 생성, "UnityD/T_" 접두사 추가, 8자리로 자름
            Client.Connect(clientId); // 브로커에 연결

            Debug.Log($"[MqttManager] 연결 상태: {Client.IsConnected} | 포트: {port}");

            portLabel.text = $"Port : {port}";
    }

    void OnDestroy()
    {
        if(IsConnected)
        {
            Client.Disconnect();
        }
    }

}
