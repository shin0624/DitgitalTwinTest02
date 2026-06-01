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

    public float simulatedTime {get; private set;} = 21600.0f;//  오전 6시에서 시작(초단위)
    private const float Deg2Rad = Mathf.PI / 180.0f;// 각도-라디안 변환 상수
    public WeatherState currentWeather {get; private set;} = WeatherState.Clear;// 현재 날씨 상태 

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

        float irradiance = Mathf.Max(0.0f, config.solarConstant * Mathf.Sin(altitudeDeg * Deg2Rad));// 일조량 계산(지평선 아래이면 0) -> Sin함수 기반의 태양 고도각에 따른 대기 감쇠 반영
        
        irradianceChannel.Raise(irradiance);// 계산된 일조량을 이벤트 채널을 통해 전달

        currentWeather = DetermineWeather(irradiance, altitudeDeg);// 현재 날씨 상태 업데이트
        
    }

    private WeatherState DetermineWeather(float irradiance, float altitudeDeg)// 일조량과 태양 고도각을 기반으로 현재 날씨 상태를 결정하는 메서드
    {
        if(altitudeDeg <=0 || irradiance <=5.0f)// 태양이 지평선 아래이거나 일조량이 매우 낮으면 밤으로 간주
        {
            return WeatherState.Night;
        }

        float angleRad = altitudeDeg * Mathf.Deg2Rad;// 태양 고도각을 라디안으로 변환
        float clearSkyValue = config.solarConstant * Mathf.Sin(angleRad);// 맑은 날의 이론적 일조량 계산

        if(clearSkyValue < 5.0f)
        {
            clearSkyValue = 5.0f;// 극단적으로 낮은 일조량에서 계산 오류 방지
        }

        float kc = irradiance / clearSkyValue;// 청천지수(kc, 실제 지표면에 도달하는 태양복사 에너지양 / 구름 등이 없는 맑은 하늘일 때 예상되는 이론적 태양복사 에너지양)

        if(kc >=0.85f)
        {
            Debug.Log($"맑은 날: kc={kc:F2}, 일조량={irradiance:F1} W/m², 고도각={altitudeDeg:F1}°");
            return WeatherState.Clear;//맑은 날(kc 85% 이상)
        }
        else if (kc >=0.50f)// 구름 조금(kc 50% 이상 85% 미만)
        {
            Debug.Log($"구름 조금: kc={kc:F2}, 일조량={irradiance:F1} W/m², 고도각={altitudeDeg:F1}°");
            return WeatherState.PartiallyCloudy;
        }
        else if(kc >=0.15f)
        {
            Debug.Log($"구름 많음: kc={kc:F2}, 일조량={irradiance:F1} W/m², 고도각={altitudeDeg:F1}°");
            return WeatherState.Overcast;// 구름 많음(kc 15% 이상 50% 미만)
        }
        else
        {
            Debug.Log($"비 또는 짙은 안개: kc={kc:F2}, 일조량={irradiance:F1} W/m², 고도각={altitudeDeg:F1}°");
            return WeatherState.RainyOrHeavyFog;// 비 또는 짙은 안개(kc 15% 미만)
        }
        
    }



    
}
