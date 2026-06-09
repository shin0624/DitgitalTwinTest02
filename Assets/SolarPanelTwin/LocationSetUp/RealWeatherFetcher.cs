using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class RealWeatherFetcher : MonoBehaviour
{
    // 기상청 산업 특화 -> 태양광 API 3종을 호출하여, Node.js 프록시 서버를 통해 실시간 일사량과 예측 데이터를 받아오는 스크립트.
    // 1번 : sun_sfc_day.php 지상관측데이터 일통계(일사, 일조, 구름, 기온, 습도) 조회서비스
    // 3번 : nph_sun_sts_pkg 관측-통계(일사, 일조) 묶음형 조회서비스
    // 4번 : nph_sun_sat_ana_txt 천리안2A호 인공지능 기반 일사량 데이터 지점 조회서비스

    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;
    [SerializeField] private RealWeatherDataSO weatherData;

    [Header("갱신 주기 (초)")]
    [SerializeField] private float realtimeIntervalSec = 1800.0f;  // 30분 (3번)
    [SerializeField] private float forecastIntervalSec = 1800.0f;  // 30분 (4번 - 30분 단위 생산)

    void Start()
    {
        
    }
}
