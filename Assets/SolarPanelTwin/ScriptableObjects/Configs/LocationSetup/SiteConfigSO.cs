using UnityEngine;

[CreateAssetMenu(fileName = "SiteConfigSO", menuName = "LocationSetUp/SiteConfigSO")]
public class SiteConfigSO : ScriptableObject
{

    [Header("선택 위치(위경도)")]
    public double latitude = 37.5665; // 기본값: 서울시청 위도
    public double longitude = 126.9780; // 기본값: 서울시청 경도
    public double heightMeters = 0.0; // 클릭한 지점의 고도(m)

    [Header("기상청 격자 좌표")]
    public int nx = 60;
    public int ny = 127;

    [Header("ASOS 지점 정보")]
    [Tooltip("지상 관측 지점 번호(서울 = 108, 부산 = 159...)")]
    public string stationId = "108";

    [Header("기상청 API 허브 인증키")]
    [Tooltip("apihub.kma.go.kr 발급 API 키")]
    public string kmaApiKey = "";

    [Header("Node.js 프록시 서버 주소")]
    [Tooltip("예: http://localhost:3000  또는 배포 후 도메인")]
    public string proxyBaseUrl = "http://localhost:3000";

    [Header("패널 파라미터")]
    [Range(0.0f, 90.0f)] public float tiltAngleDeg = 20.0f;// 기울기
    [Range(0.0f, 360.0f)] public float azimuthDeg = 180.0f;// 방위각 (남향 180도)
    public float panelAreaM2 = 1.65f;//단일 패널 면적
    public int panelCount = 1;// 패널 수
    public float panelEfficiency = 0.20f;// 패널 효율

    [Header("씬 전환 완료 플래그")]
    public bool siteConfirmed = false;// 위치 설정이 완료되어 씬 전환이 가능한지 여부를 나타내는 플래그. UI에서 위치 설정이 완료되면 true로 설정하여 다음 단계로 진행할 수 있도록 함


    
}
