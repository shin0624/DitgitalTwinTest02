using UnityEngine;
using TMPro;
using System;

public class SunArchUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SunController sunController;
    [SerializeField] private RectTransform archCenter;// 아치의 중심 위치
    [SerializeField] private RectTransform sunDot;// 태양 점의 RectTransform
    [SerializeField] private TMP_Text timeText;// 시간 텍스트 UI

    [Header("Arch Settings")]
    [SerializeField] private float radius = 140.0f; // 아치의 반지름
    [SerializeField] private float startHour = 5.0f; // 시각화 시작 시간
    [SerializeField] private float endHour = 20.0f; // 시각화 종료 시간


    void Update()
    {
        float totalSeconds = sunController.simulatedTime;// SunController에서 시뮬레이션된 시간을 초 단위로 가져옴
        float currentHour = totalSeconds / 3600.0f;//   초 단위를 시간 단위로 변환

        UpdateSunPosition(currentHour);// 태양 점의 위치를 시뮬레이션 시간을 기준으로 업데이트

        if(timeText != null)
        {
            int h = Mathf.FloorToInt(currentHour) % 24;
            int m = Mathf.FloorToInt((currentHour % 1f) * 60f);
            timeText.text = $"{h:00}:{m:00}";
        }
    }

    public void UpdateSunPosition(float hour)// 시간(0~24)을 입력받아 태양 점의 위치를 업데이트하는 함수
    {
        float t = Mathf.InverseLerp(startHour, endHour, hour);// 시간 범위를 0~1로 정규화
        t = Mathf.Clamp01(t);// 정규화된 값을 0~1로 클램프

        float angle = Mathf.Lerp(180.0f, 0.0f, t) * Mathf.Deg2Rad;// 아치의 각도를 라디안으로 변환

        float x = Mathf.Cos(angle) * radius;// 아치의 x 좌표 계산
        float y = Mathf.Sin(angle) * radius;// 아치의 y 좌표 계산

        sunDot.SetParent(archCenter, false);// 태양 점의 부모를 아치 중심으로 설정
        sunDot.anchoredPosition = new Vector2(x,y);// 태양 점의 위치 업데이트

    }
}
