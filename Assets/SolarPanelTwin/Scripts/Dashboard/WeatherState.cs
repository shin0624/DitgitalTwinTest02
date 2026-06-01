using UnityEngine;

public enum WeatherState
{
    Night, // 야간(태양이 지평선 아래에 있거나 광량이 없음)
    RainyOrHeavyFog, // 비 또는 짙은 안개(이론값 대비 광량이 극도로 낮음)
    Overcast, // 구름 많음(대기 산란광만 존재하는 상태)
    PartiallyCloudy,// 구름 조금(구름에 의해 태양이 간헐적으로 가려짐)
    Clear // 맑음(현재 태양 고도에서 나올 수 있는 최적의 광량)
}

