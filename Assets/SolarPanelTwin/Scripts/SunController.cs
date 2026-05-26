using System;
using UnityEngine;

public class SunController : MonoBehaviour
{
    // Directional Light를 태양으로 사용하고, 지구 자전을 모방하여 X축 중심의 일정 속도 회전을 구현하는 스크립트.
    // Unity에서 Directional Light는 위치가 아닌 방향이 중요하므로, 실제 태양 고도각 공식, 위도/경도 파라미터가 저장된 SO를 사용하여 시뮬레이션 시간을 실제 시간의 n배로 가속한다.
    // 태양 고도각 : sin(태양 고도각) = sin(적위)sin(위도) + cos(적위)cos(위도)cos(시각)

    [Header("참조 SO")]
    public DayNightConfig config;// 태양 위치와 일조량 계산에 필요한 설정값을 담는 ScriptableObject
    public FloatEventChannel irradianceChannel;// 태양 복사량을 전달하기 위한 이벤트 채널

    private float simulatedTime = 21600.0f;//  오전 6시에서 시작(초단위)
    private const float Deg2Rad = Mathf.PI / 180.0f;// 각도-라디안 변환 상수


    void Update()
    {
        simulatedTime += Time.deltaTime * config.timeScale;// 시뮬레이션 시간 업데이트

        if(simulatedTime >=config.dayDurationSec)//하루가 끝나면
        {
            simulatedTime -= config.dayDurationSec;// 시뮬레이션 시간 초기화
        }

        float hourAngle = ((simulatedTime / config.dayDurationSec) * 360.0f) - 180.0f;// 시각에 따른 시각각 계산(남향 기준, 정오 = 0도)

        // 태양 고도각 계산
        float declinationRad = 0.0f;// 춘 추분 근사(적위 0)
        float latRad = config.latitude * Deg2Rad;// 태양 고도각 계산을 위한 위도 라디안 변환
        float haRad = hourAngle * Deg2Rad;// 시각각 라디안 변환

        float sinAlt = Mathf.Sin(latRad) * MathF.Sin(declinationRad) + Mathf.Cos(latRad) * Mathf.Cos(declinationRad) * Mathf.Cos(haRad);// 태양 고도각의 사인값 계산
        
        float altitudeDeg = Mathf.Asin(sinAlt) / Deg2Rad;// 태양 고도각 계산(도 단위)

        transform.rotation = Quaternion.Euler(altitudeDeg, config.azimuthOIffset, 0.0f); // Directional Light의 회전(X축 : 고도각, Y축  : 방위각 오프셋)

        float irradiance = Mathf.Max(0.0f, config.solarConstant * Mathf.Sin(altitudeDeg * Deg2Rad));// 일조량 계산(지평선 아래이면 0)
        
        irradianceChannel.Raise(irradiance);// 계산된 일조량을 이벤트 채널을 통해 전달
    }
}
