using System;
using UnityEngine;

public static class GridCoordConverter
{
    //  기상청 공식 격자 변환 공식을 사용하여 위경도 <-> 격자 좌표 변환을 수행하는 유틸리티 클래스.
    //기상청 공식 LCC 파라미터
    const double RE = 6371.00877; // 지구 반경 (km)
    const double GRID = 5.0;// 격자 간격 (km)
    const double SLAT1 = 30.0;// 투영 위도 1 (degree)
    const double SLAT2 = 60.0;// 투영 위도 2 (degree)
    const double OLON = 126.0;// 기준점 경도 (degree)
    const double OLAT = 38.0;// 기준점 위도 (degree)
    const double XO = 43.0;// 기준점 X 좌표 (격자 좌표)
    const double YO = 136.0;// 기준점 Y 좌표 (격자 좌표)
    const double DEGRAD = Mathf.PI / 180.0;// degree -> radian 변환 상수

    public static (int nx, int ny) LatLonToGrid(double lat, double lon)
    {
        double sn = Mathf.Log((float) (Mathf.Cos((float)(SLAT1 * DEGRAD)) / 
                    Mathf.Cos((float)(SLAT2 * DEGRAD)))) / 
                    Mathf.Log((float)(Mathf.Tan((float)((90.0 + SLAT1) * 0.5 * DEGRAD)) / 
                    Mathf.Tan((float)((90.0 + SLAT2) * 0.5 * DEGRAD)))); // 투영 위도 1과 2로부터 투영 상수 sn 계산
            
        double sf = Mathf.Pow((float)Mathf.Tan((float)((90.0 + SLAT1) * 0.5 * DEGRAD)), (float)sn) *
                    Mathf.Cos((float)(SLAT1 * DEGRAD)) / (float)sn; // 투영 위도 1로부터 투영 상수 sf 계산
        
        double ro = RE / GRID * sf /
                    Mathf.Pow((float)Mathf.Tan((float)((90.0 + OLAT) * 0.5 * DEGRAD)), (float)sn); // 기준점 위도로부터 기준점과의 거리 ro 계산

        double ra = RE / GRID * sf /
                    Mathf.Pow((float)Mathf.Tan((float)((90.0 + lat) * 0.5 * DEGRAD)), (float)sn); // 입력 위도로부터 기준점과의 거리 ra 계산

        double theta = (lon - OLON) * DEGRAD * sn; // 입력 경도에서 기준점 경도를 빼고 투영 상수 sn을 곱하여 theta 계산

        int nx = (int)(ra * Mathf.Sin((float)theta) + XO + 1.5); // ra에 theta의 사인값을 곱하고 기준점 X 좌표를 더하여 격자 X 좌표 nx 계산 (1.5는 반올림 보정)
        int ny = (int)(ro - ra * Mathf.Cos((float)theta) + YO + 1.5); // 기준점과의 거리 ro에서 ra에 theta의 코사인값을 곱한 값을 빼고 기준점 Y 좌표를 더하여 격자 Y 좌표 ny 계산 (1.5는 반올림 보정)

        return (nx, ny); // 계산된 격자 좌표 (nx, ny) 반환

        /*
        사용 예시
        var (nx, ny) = GridCoordConverter.LatLonToGrid(37.5665, 126.9780);
        결과 : nx=60, ny=127 (서울)
        siteConfig.nx = nx;
        siteConfig.ny = ny;
        
        */
    }

}
