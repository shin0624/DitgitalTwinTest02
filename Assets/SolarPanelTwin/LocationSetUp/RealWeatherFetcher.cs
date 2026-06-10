using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Networking;


public class RealWeatherFetcher : MonoBehaviour
{
    // 기상청 산업 특화 -> 태양광 API 3종을 호출하여, Node.js 프록시 서버를 통해 실시간 일사량과 예측 데이터를 받아오는 스크립트.
    // 1번 : sun_sfc_day.php 지상관측데이터 일통계(일사, 일조, 구름, 기온, 습도) 조회서비스
    // 3번 : nph_sun_sts_pkg 관측-통계 묶음형 조회서비스
    // 4번 : nph_sun_sat_ana_txt 천리안2A호 인공지능 기반 일사량 데이터 지점 조회서비스

    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;
    [SerializeField] private RealWeatherDataSO weatherData;

    [Header("갱신 주기 (초)")]
    [SerializeField] private float realtimeIntervalSec = 1800.0f;  // 30분 (3번)
    [SerializeField] private float forecastIntervalSec = 1800.0f;  // 30분 (4번 - 30분 단위 생산)

    [Serializable] private class DailyDTO// 1번 API 응답 DTO
    {
        public float sumGsr;// 전천일사 일합계 MJ/m²
        public float sumSs;// 일조시간 hr
        public float taAvg; // 전일 평균 기온 °C (TA_AVG °C)
    }
    [Serializable] private class RealtimeDTO// 3번 API 응답 DTO
    {
        public float gsr; // 일사량 W/m²
        public float ta; // 기온 °C
        public float ws; // 풍속 m/s
    }

    [Serializable] private class ForecastDTO // 4번 API 응답 DTO
    {
        public int baseHourUtc;// 예측 기준 시각 UTC HH
        public float[] values;// 향후 최대 48개(24시간 * 30분) 예측 일사량 W/m²
    }

    void Start()
    {
        StartCoroutine(RealtimeLoop());
        StartCoroutine(ForecastLoop());
        StartCoroutine(DailyStatLoop());
    }

    private IEnumerator RealtimeLoop()// 매 30분마다 3번 API를 호출하여 실시간 일사량 데이터를 갱신하기 위한 코루틴
    {
        while(true)
        {
            yield return FetchRealtime();
            yield return new WaitForSeconds(realtimeIntervalSec);
        }
    }

    private IEnumerator ForecastLoop()// 매 30분마다 4번 API를 호출하여 일사량 예측 데이터를 갱신하기 위한 코루틴
    {
        while (true)
        {
            yield return FetchSatelliteForecast();
            yield return new WaitForSeconds(forecastIntervalSec);
        }
    }

    private IEnumerator DailyStatLoop()// 매일 특정 시점에 1번 API를 호출하여 전일 데이터를 갱신하기 위한 코루틴
    {
        yield return WaitUntilHour(2);  // 매일 02:00 KST 전날 데이터 확정 후 조회
        while (true)
        {
            yield return FetchDailyStat();
            yield return WaitUntilHour(2);
        }
    }

    // 지상관측데이터 일통계 조회서비스 사용을 위한 메서드
    // 프록시 : GET/api/kma/daily
    private IEnumerator FetchDailyStat()// 매일 특정 시점에 1번 API를 호출하여 전일 데이터를 갱신하기 위한 코루틴
    {
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");// API가 전날 데이터를 제공하므로, 어제 날짜를 "yyyyMMdd" 형식으로 생성
        string url = $"{siteConfig.proxyBaseUrl}/api/kma/daily" +
                    $"?stn={siteConfig.stationId}&tm1={yesterday}&tm2={yesterday}";// 프록시 서버의 일통계 엔드포인트 URL 구성, 쿼리 파라미터로 지점 번호(stn)와 날짜(tm1, tm2)를 전달

        using var req = UnityWebRequest.Get(url);// UnityWebRequest를 사용하여 GET 요청 생성

        req.timeout = 10;// 타임아웃 설정 (초)

        yield return req.SendWebRequest();// 요청 전송 및 응답 대기

        if(req.result != UnityWebRequest.Result.Success)// 요청이 성공적으로 완료되었는지 확인
        {
            Debug.LogWarning($"[Daily] 실패: {req.error}");
            yield break;
        }
        ParseDailyStat(req.downloadHandler.text);// 응답에서 필요한 데이터 추출하여 SO에 저장하는 메서드 호출
    }

    private void ParseDailyStat(string json)// 1번 API 응답에서 필요한 데이터 추출하여 SO에 저장하는 메서드
    {   
        // 프록시 정규화를 위한 json 구조는 { "sumGsr": 12.34, "sumSs": 5.67 } 형태로 가정하며, 실제 API 응답에서 필요한 데이터를 추출
        try
        {
            var d  = JsonUtility.FromJson<DailyDTO>(json);// JSON 응답에서 필요한 데이터 추출
            weatherData.dailySumIrradMJm2 = d.sumGsr;// 전천일사 일합계 MJ/m²
            weatherData.dailySunshineHr = d.sumSs;// 일조시간 hr
            weatherData.dailyTaAvgC = d.taAvg;
        }
        catch(Exception e)
        {
            Debug.LogError($"[Daily] 파싱 오류: {e.Message}");
        }
    }

    // 관측-통계 묶음형 사용을 위한 메서드
    // 프록시 : GET/api/kma/realtime

    private IEnumerator FetchRealtime()
    {
        DateTime nowKst = DateTime.Now;// 유니티는 로컬 시간 =kst 환경을 가정

        int minFloor = (nowKst.Minute < 30) ? 0 : 30;

        DateTime tm2Kst = new DateTime(nowKst.Year, nowKst.Month, nowKst.Day, nowKst.Hour, minFloor, 0);// API가 30분 단위로 생산되므로, 현재 시각에서 분을 0 또는 30으로 설정하여 tm2 생성
        DateTime tm1Kst = tm2Kst.AddMinutes(-30);

        string tm1 = tm1Kst.ToString("yyyyMMddHHmm");
        string tm2 = tm2Kst.ToString("yyyyMMddHHmm");

        string url = $"{siteConfig.proxyBaseUrl}/api/kma/realtime" +
                     $"?stn={siteConfig.stationId}&tm1={tm1}&tm2={tm2}";

        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Realtime] 실패: {req.error}");
            yield break;
        }

        ParseRealtime(req.downloadHandler.text);
    }

    private void ParseRealtime(string json)
    {
        // 프록시 정규화 JSON 구조는 {"tgsr": 123.4, "ta": 25.6, "ws": 3.2} 형태로 가정하며, 실제 API 응답에서 일사량(tgsr), 기온(ta), 풍속(ws) 데이터를 추출

        try
        {
            var d = JsonUtility.FromJson<RealtimeDTO>(json);// JSON 응답에서 필요한 데이터 추출
            weatherData.irradianceWm2  = d.gsr; // 일사량 W/m²
            weatherData.temperatureC   = d.ta; // 기온 °C
            weatherData.windSpeedMs    = d.ws; // 풍속 m/s
            weatherData.lastUpdatedAt  = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            weatherData.isDataFresh    = true;  // 데이터가 성공적으로 갱신되었음을 나타내는 플래그 설정
        }
        catch(Exception e)
        {
            Debug.LogError($"[Realtime] 파싱 오류: {e.Message}\n{json}");
        }
    }

    // 천리안2A호 인공지능 기반 일사량 데이터 지점 조회 사용을 위한 메서드
    // 프록시 : GET/api/kma/forecast

    // 4번 API는 UTC 기준이므로, KST에서 9시간 차감 필요
    private IEnumerator FetchSatelliteForecast()
    {
        DateTime nowUtc = DateTime.UtcNow;

        int minFloor = (nowUtc.Minute < 30) ? 0 : 30; // 30분 단위로 내림 처리하여 API의 생산 시점과 맞춤

        DateTime tm1Utc = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, minFloor, 0);// API가 30분 단위로 생산되므로, 현재 시각에서 분을 0 또는 30으로 설정하여 tm1 생성
        DateTime tm2Utec = tm1Utc.AddHours(24);// API가 최대 48개(24시간 * 30분) 예측을 제공하므로, tm2는 tm1에서 24시간 후로 설정

        string tm1 = tm1Utc.ToString("yyyyMMddHHmm");// tm1을 "yyyyMMddHHmm" 형식으로 생성
        string tm2 = tm2Utec.ToString("yyyyMMddHHmm");// tm2

        string url = $"{siteConfig.proxyBaseUrl}/api/kma/forecast" +
                     $"?lat={siteConfig.latitude:F6}&lon={siteConfig.longitude:F6}" +
                     $"&tm1={tm1}&tm2={tm2}&int=30"; // 프록시 서버의 천리안2A호 예측 엔드포인트 URL 구성, 쿼리 파라미터로 위도(lat), 경도(lon), 예측 시점(tm1, tm2), 간격(int=30분)을 전달
        
        using var req = UnityWebRequest.Get(url);// UnityWebRequest를 사용하여 GET 요청 생성
        req.timeout = 10;// 타임아웃 설정 (초)
        yield return req.SendWebRequest();// 요청 전송 및 응답 대기

        if(req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Forecast] 실패: {req.error}");
            yield break;
        }

        ParseForecast(req.downloadHandler.text);// 응답에서 필요한 데이터 추출하여 SO에 저장하는 메서드 호출
    }

    private void ParseForecast(string json)// 4번 API 응답에서 필요한 데이터 추출하여 SO에 저장하는 메서드
    {
        // 프록시 정규화 JSON 구조는 {"baseHourUtc":5, "values":[520.0,480.0...]} 형태로 가정하며, 실제 API 응답에서 예측 기준 시각과 일사량 예측 배열을 추출
        try
        {
            var d = JsonUtility.FromJson<ForecastDTO>(json);// JSON 응답에서 필요한 데이터 추출
            weatherData.forecastBaseHourUtc = d.baseHourUtc;// 예측 기준 시각 UTC HH

            int len = Mathf.Min(d.values.Length, 48);// 예측 일사량 배열의 길이가 48을 초과할 수 있으므로, 안전하게 최소값으로 설정
            for (int i = 0; i <len; i++)
            {
                weatherData.forecastIrradianceWm2[i] = d.values[i];// 향후 최대 48개(24시간 * 30분) 예측 일사량 W/m²
            }
        }
        catch(Exception e)    
        {
            Debug.LogError($"[Forecast] 파싱 오류: {e.Message}");
        }
        
    }

    //유틸리티 코루틴
    private IEnumerator WaitUntilHour(int targetHour)// 매 시간마다 특정 시점에 일통계 데이터를 갱신하기 위해 매 시간마다 현재 시각이 targetHour인지 체크하는 코루틴
    {
        while(DateTime.Now.Hour != targetHour)//  매 시간마다 현재 시각이 targetHour인지 체크
        {
            yield return new WaitForSeconds(60.0f);//   1분마다 체크
        }
    }


}
