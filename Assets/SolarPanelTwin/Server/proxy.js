// server/proxy.js
import express from 'express';
import fetch   from 'node-fetch';
import dotenv  from 'dotenv';
import { KMA_API_KEY as LOCAL_KMA_API_KEY } from './apiKey.local.js';
dotenv.config();

const app    = express();
const PORT   = process.env.PORT        || 3000;
const APIKEY = process.env.KMA_API_KEY || LOCAL_KMA_API_KEY || '';

if (!APIKEY) {
    console.warn('[KMA] API KEY가 설정되지 않았습니다. .env 또는 apiKey.local.js를 확인하세요.');
}

app.use((req, res, next) => {
    res.setHeader('Access-Control-Allow-Origin',  '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET,OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    if (req.method === 'OPTIONS') return res.sendStatus(200);
    next();
});

// ─────────────────────────────────────────────────────────────────────
// [1번 API] sun_sfc_day.php — 일통계
// 응답 컬럼: 0:YYMMDD 1:STN 2:TA_AVG 3:TA_MAX 4:TA_MAX_TM 5:TA_MIN
//           6:TA_MIN_TM 7:HM_AVG 8:HM_MIN 9:HM_MIN_TM 10:CA_TOT
//           11:SS_DAY(일조량hr) 12:SS_DUR 13:SI_DAY(MJ/m²) 14:SI_60M_MAX
// ※ 실제 데이터 기준: cols[10]=SS_DAY, cols[12]=SI_DAY
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/daily', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    if (!stn || !tm1 || !tm2)
        return res.status(400).json({ error: 'stn, tm1, tm2 필수' });

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/url/sun_sfc_day.php` +
        `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&help=0&disp=0&authKey=${APIKEY}`;

    try {
        const raw  = await fetch(kmaUrl).then(r => r.text());
        console.log('[daily raw]', raw.substring(0, 200));
        const data = parseDaily(raw);
        res.json(data);
    } catch (e) {
        console.error('[daily]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseDaily(text) {
    // #으로 시작하거나 빈 줄 제거, 마지막 데이터 행 사용
    const lines = text.split('\n')
        .filter(l => l.trim() && !l.startsWith('#'));
    if (!lines.length) throw new Error('빈 응답');

    const cols = lines[lines.length - 1].trim().split(/\s+/);
    // 인덱스 (실수신 확인 완료 기준):
    // 0:YYMMDD  1:STN  2:TA_AVG  3:TA_MAX  4:TA_MAX_TM  5:TA_MIN  6:TA_MIN_TM
    // 7:HM_AVG  8:HM_MIN  9:HM_MIN_TM  10:CA_TOT
    // 10:SS_DAY(일조량hr)  11:SS_DUR  12:SI_DAY(전천일사합 MJ/m²)  13:SI_60M_MAX
    // ※ 헤더 주석 기준: SS_DAY=index 10, SI_DAY=index 12
    return {
        sumSs:  parseFloat(cols[10]) || 0,  // 일조량 hr
        sumGsr: parseFloat(cols[12]) || 0,  // 전천일사합 MJ/m²
        taAvg:  parseFloat(cols[2])  || 0,  // 기온 °C (보너스)
    };
}

// ─────────────────────────────────────────────────────────────────────
// [3번 API] nph-sun_sfc_sts_pkg — 실시간 일사량 (mode=si)
// 응답 컬럼: 0:YYMMDDHHMI  1:STN  2:SI_MI(매분누적 MJ/m²)
//           3:SI_HR(시간일사량 MJ/m²)  4:SI_DAY(일합계)
//           5:SI_MON(월합계)  6:SI_YEAR(연합계)
//
// 실시간 W/m² 변환:
//   SI_HR (MJ/m²/hr) × 277.78 = W/m²
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/realtime', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    if (!stn || !tm1 || !tm2)
        return res.status(400).json({ error: 'stn, tm1, tm2 필수' });

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph-sun_sfc_sts_pkg` +
        `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&mode=si&help=0&disp=0&authKey=${APIKEY}`;

    try {
        const raw  = await fetch(kmaUrl).then(r => r.text());
        console.log('[realtime raw]', raw.substring(0, 200));
        const data = parseRealtime(raw);
        res.json(data);
    } catch (e) {
        console.error('[realtime]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseRealtime(text) {
    const lines = text.split('\n')
        .filter(l => l.trim() && !l.startsWith('#'));
    if (!lines.length) throw new Error('빈 응답');

    // 가장 마지막 행 (최신 데이터)
    const cols = lines[lines.length - 1].trim().split(/\s+/);
    // 0:YYMMDDHHMI  1:STN  2:SI_MI  3:SI_HR(MJ/m²/hr)
    // 4:SI_DAY  5:SI_MON  6:SI_YEAR

    const siHr = parseFloat(cols[3]) || 0;  // MJ/m²/hr
    // MJ/m²/hr → W/m²: 1 MJ = 1,000,000 J, 1hr = 3600s → W = J/s
    // W/m² = (MJ × 1,000,000) / 3600 = MJ × 277.78
    const gsrW = parseFloat((siHr * 277.78).toFixed(2));

    return {
        gsr:     gsrW,                          // 전천일사 W/m² (변환값)
        siMi:    parseFloat(cols[2]) || 0,      // 매분 누적 MJ/m² (원시값, 참고용)
        siDay:   parseFloat(cols[4]) || 0,      // 당일 일합계 MJ/m²
        ta:      25,                            // 3번 API에 기온 없음 → 기본값 유지
        ws:      0,                             // 3번 API에 풍속 없음 → 기본값 유지
    };
}

// ─────────────────────────────────────────────────────────────────────
// [4번 API] nph_sun_sat_ana_txt — 위성 AI 예측
// 응답 형식: 파이프(|) 구분 테이블, 시간이 열(컬럼)으로 배치됨
// 헤더 행: | TMFC | VAR | LON | LAT | 시각1 | 시각2 | ...
// 데이터 행: | 기준시각 | AI-DSR | 경도 | 위도 | 값1 | 값2 | ...
// 단위: W/m² (직달일사, AI-DSR)
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/forecast', async (req, res) => {
    const { lat, lon, tm1, tm2, int: interval } = req.query;
    if (!lat || !lon || !tm1 || !tm2)
        return res.status(400).json({ error: 'lat, lon, tm1, tm2 필수' });

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph_sun_sat_ana_txt` +
        `?tm1=${tm1}&tm2=${tm2}&int=${interval || 30}&lat=${lat}&lon=${lon}&authKey=${APIKEY}`;

    try {
        const raw  = await fetch(kmaUrl).then(r => r.text());
        console.log('[forecast raw]', raw.substring(0, 400));
        const data = parseForecast(raw);
        res.json(data);
    } catch (e) {
        console.error('[forecast]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseForecast(text) {
    // 파이프(|) 구분 테이블 파싱
    // 헤더: | TMFC | VAR | LON | LAT | 202606090000 | 202606090030 | ...
    // 데이터: | 202606090000 | AI-DSR | 126.9167 | 37.4938 | 2.0 | 2.3 | ...

    const lines = text.split('\n')
        .map(l => l.trim())
        .filter(l => l.startsWith('|') && l.endsWith('|'));

    if (lines.length < 2) throw new Error('파이프 테이블 파싱 실패');

    // 헤더 행에서 시각 컬럼 추출 (index 4 이후)
    const headerCols = lines[0].split('|').map(c => c.trim()).filter(c => c);
    // headerCols: ['TMFC','VAR','LON','LAT','202606090000','202606090030',...]

    let baseHourUtc = 0;
    const values = [];

    // AI-DSR 행 찾기
    for (let i = 1; i < lines.length; i++) {
        const cols = lines[i].split('|').map(c => c.trim()).filter(c => c);
        if (cols.length < 5) continue;
        if (!cols[1].includes('AI-DSR')) continue;

        // 기준 시각 (UTC yyyymmddhhmm → hour 추출)
        const tmfc = cols[0].replace(/\s/g, '');
        if (tmfc.length >= 10) {
            baseHourUtc = parseInt(tmfc.substring(8, 10)) || 0;
        }

        // index 4 이후가 시각별 예측값 (W/m²)
        for (let v = 4; v < cols.length; v++) {
            const val = parseFloat(cols[v]);
            if (!isNaN(val)) values.push(val);
        }
        break;  // AI-DSR 행 1개만 처리
    }

    return {
        baseHourUtc,
        values: values.slice(0, 48),  // 최대 48개 (30분 × 48 = 24시간)
    };
}

// ─────────────────────────────────────────────────────────────────────
// 디버그용 원문 라우트
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/daily-raw', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    const url = `https://apihub.kma.go.kr/api/typ01/url/sun_sfc_day.php` +
                `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&help=1&disp=0&authKey=${APIKEY}`;
    const raw = await fetch(url).then(r => r.text());
    res.type('text/plain').send(raw);
});

app.get('/api/kma/realtime-raw', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    const url = `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph-sun_sfc_sts_pkg` +
                `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&mode=si&help=1&disp=0&authKey=${APIKEY}`;
    const raw = await fetch(url).then(r => r.text());
    res.type('text/plain').send(raw);
});

app.get('/api/kma/forecast-raw', async (req, res) => {
    const { lat, lon, tm1, tm2 } = req.query;
    const url = `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph_sun_sat_ana_txt` +
                `?tm1=${tm1}&tm2=${tm2}&int=30&lat=${lat}&lon=${lon}&authKey=${APIKEY}`;
    const raw = await fetch(url).then(r => r.text());
    res.type('text/plain').send(raw);
});

app.listen(PORT, () => console.log(`KMA Proxy on port ${PORT}`));