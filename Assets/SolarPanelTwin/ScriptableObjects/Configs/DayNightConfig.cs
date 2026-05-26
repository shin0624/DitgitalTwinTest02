using UnityEngine;

[CreateAssetMenu(menuName ="SolarSim/DayNightConfig")]
public class DayNightConfig : ScriptableObject
{ //태양의 위치와 일조량 계산에 필요한 설정값을 담는 ScriptableObject
    public float latitude = 37.5f;// 위도
    public float timeScale = 6000.0f;//1초 = 실제 1분으로 테스트
    public float dayDurationSec = 86400.0f; //하루의 실제 길이(초 단위, 기본값은 24시간)
    public float solarConstant = 1000.0f;//맑은 날의 최대 태양 복사량(W/m²)
    public float azimuthOIffset = 180.0f; // 남향 기준 태양의 방위각 오프셋(도 단위, 기본값은 180도)
}
