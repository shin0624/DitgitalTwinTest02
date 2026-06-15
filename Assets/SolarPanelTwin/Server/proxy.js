// server/proxy.js
import express from 'express';
import fetch from 'node-fetch';
import dotenv from 'dotenv';
import { KMA_API_KEY as LOCAL_KMA_API_KEY } from './apiKey.local.js';
dotenv.config();

const app = express();
const PORT = process.env.PORT || 3000;
const APIKEY = process.env.KMA_API_KEY || LOCAL_KMA_API_KEY || '';

if (!APIKEY) {
    console.warn('[KMA] API KEY가 설정되지 않았습니다. .env 또는 apiKey.local.js를 확인하세요.');
}

app.use((req, res, next) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET,OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    if (req.method === 'OPTIONS') return res.sendStatus(200);
    next();
});

// ─────────────────────────────────────────────────────────────────────
// 공통 유틸: 기상청 XML <Error> 응답 감지
// 기상청은 오류 시에도 HTTP 200을 반환하고 본문에 <Error>를 담는 경우가 있음
// ─────────────────────────────────────────────────────────────────────
function extractKmaError(raw) {
    if (!raw.includes('<Error>')) return null;
    const match = raw.match(/<Error>([\s\S]*?)<\/Error>/);
    return match?.[1]?.trim() || '기상청 오류 응답';
}

// ─────────────────────────────────────────────────────────────────────
// 공통 유틸: "yyyyMMddHHmm" 문자열 → Date (UTC)
// ─────────────────────────────────────────────────────────────────────
function parseTm(tm) {
    return new Date(Date.UTC(
        parseInt(tm.substring(0, 4)),
        parseInt(tm.substring(4, 6)) - 1,
        parseInt(tm.substring(6, 8)),
        parseInt(tm.substring(8, 10)),
        parseInt(tm.substring(10, 12))
    ));
}

// ─────────────────────────────────────────────────────────────────────
// 공통 유틸: Date (UTC) → "yyyyMMddHHmm" 문자열
// ─────────────────────────────────────────────────────────────────────
function fmtTm(dt) {
    const pad = n => String(n).padStart(2, '0');
    return `${dt.getUTCFullYear()}${pad(dt.getUTCMonth() + 1)}${pad(dt.getUTCDate())}` +
           `${pad(dt.getUTCHours())}${pad(dt.getUTCMinutes())}`;
}

// ─────────────────────────────────────────────────────────────────────
// [1번 API] sun_sfc_day.php — 일통계
// 응답 컬럼: 0:YYMMDD 1:STN 2:TA_AVG 3:TA_MAX 4:TA_MAX_TM 5:TA_MIN
// 6:TA_MIN_TM 7:HM_AVG 8:HM_MIN 9:HM_MIN_TM 10:CA_TOT
// 10:SS_DAY(일조량hr) 11:SS_DUR 12:SI_DAY(전천일사합 MJ/m²) 13:SI_60M_MAX
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/daily', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    if (!stn || !tm1 || !tm2)
        return res.status(400).json({ error: 'stn, tm1, tm2 필수' });

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/url/sun_sfc_day.php` +
        `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&help=0&disp=0&authKey=${APIKEY}`;

    try {
        const raw = await fetch(kmaUrl).then(r => r.text());
        console.log('[daily raw]', raw.substring(0, 200));

        const kmaErr = extractKmaError(raw);
        if (kmaErr) {
            console.error('[daily] 기상청 에러:', kmaErr);
            return res.status(400).json({ error: kmaErr });
        }

        res.json(parseDaily(raw));
    } catch (e) {
        console.error('[daily]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseDaily(text) {
    const lines = text.split('\n').filter(l => l.trim() && !l.startsWith('#'));
    if (!lines.length) throw new Error('빈 응답');

    const cols = lines[lines.length - 1].trim().split(/\s+/);
    return {
        sumSs:  parseFloat(cols[10]) || 0,  // 일조량 hr
        sumGsr: parseFloat(cols[12]) || 0,  // 전천일사합 MJ/m²
        taAvg:  parseFloat(cols[2])  || 0,  // 기온 °C
    };
}

// ─────────────────────────────────────────────────────────────────────
// [3번 API] nph-sun_sfc_sts_pkg — 실시간 일사량 (mode=si)
// 응답 컬럼: 0:YYMMDDHHMI 1:STN 2:SI_MI 3:SI_HR(MJ/m²/hr)
// 4:SI_DAY 5:SI_MON 6:SI_YEAR
// SI_HR (MJ/m²/hr) × 277.78 = W/m²
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/realtime', async (req, res) => {
    const { stn, tm1, tm2 } = req.query;
    if (!stn || !tm1 || !tm2)
        return res.status(400).json({ error: 'stn, tm1, tm2 필수' });

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph-sun_sfc_sts_pkg` +
        `?stn=${stn}&tm1=${tm1}&tm2=${tm2}&mode=si&help=0&disp=0&authKey=${APIKEY}`;

    try {
        const raw = await fetch(kmaUrl).then(r => r.text());
        console.log('[realtime raw]', raw.substring(0, 200));

        const kmaErr = extractKmaError(raw);
        if (kmaErr) {
            console.error('[realtime] 기상청 에러:', kmaErr);
            return res.status(400).json({ error: kmaErr });
        }

        res.json(parseRealtime(raw));
    } catch (e) {
        console.error('[realtime]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseRealtime(text) {
    const lines = text.split('\n').filter(l => l.trim() && !l.startsWith('#'));
    if (!lines.length) throw new Error('빈 응답');

    const cols = lines[lines.length - 1].trim().split(/\s+/);
    const siHr = parseFloat(cols[3]) || 0;
    const gsrW = parseFloat((siHr * 277.78).toFixed(2));

    return {
        gsr:   gsrW,
        siMi:  parseFloat(cols[2]) || 0,
        siDay: parseFloat(cols[4]) || 0,
        ta:    25,
        ws:    0,
    };
}

// ─────────────────────────────────────────────────────────────────────
// [4번 API] nph_sun_sat_ana_txt — 위성 AI 예측
//
// ★ 핵심 수정:
//   - tm2는 클라이언트 값을 완전히 무시하고 프록시에서 강제 계산
//   - 기상청 제한: 최대 출력 24개
//   - 30분×24=12h 이지만 양 끝 포함 계산으로 25개가 되는 경우가 있음
//   - 안전하게 tm2 = tm1 + 11h (22개) 로 강제 설정
// ─────────────────────────────────────────────────────────────────────
app.get('/api/kma/forecast', async (req, res) => {
    const { lat, lon, tm1 } = req.query;  // ★ tm2는 클라이언트에서 받지 않음
    if (!lat || !lon || !tm1)
        return res.status(400).json({ error: 'lat, lon, tm1 필수' });

    // ★ tm2 강제 계산: tm1 + 11시간 (22개 타임스텝, 24개 제한 안전 통과)
    const tm1Date  = parseTm(tm1);
    const tm2Date  = new Date(tm1Date.getTime() + 11 * 60 * 60 * 1000);
    const tm2Forced = fmtTm(tm2Date);

    console.log(`[forecast] tm1=${tm1}  tm2(강제)=${tm2Forced}`);

    const kmaUrl =
        `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph_sun_sat_ana_txt` +
        `?tm1=${tm1}&tm2=${tm2Forced}&int=30&lat=${lat}&lon=${lon}&authKey=${APIKEY}`;

    console.log('[forecast url]', kmaUrl);

    try {
        const raw = await fetch(kmaUrl).then(r => r.text());
        console.log('[forecast raw]', raw.substring(0, 400)); 

        const kmaErr = extractKmaError(raw);
        if (kmaErr) {
            console.error('[forecast] 기상청 에러:', kmaErr);
            return res.status(400).json({ error: kmaErr });
        }

        res.json(parseForecast(raw));
    } catch (e) {
        console.error('[forecast]', e.message);
        res.status(502).json({ error: e.message });
    }
});

function parseForecast(text) {
    const lines = text.split('\n')
        .map(l => l.trim())
        .filter(l => l.startsWith('|') && l.endsWith('|'));

    if (lines.length < 2) {
        console.warn('[forecast] 파이프 테이블 없음 — 데이터 미생산 구간');
        return { baseHourUtc: 0, values: [] };
    }

    let baseHourUtc = 0;
    const values = [];

    for (let i = 1; i < lines.length; i++) {
        const cols = lines[i].split('|').map(c => c.trim()).filter(c => c);
        if (cols.length < 5) continue;
        if (!cols[1].includes('AI-DSR')) continue;

        const tmfc = cols[0].replace(/\s/g, '');
        if (tmfc.length >= 10) {
            baseHourUtc = parseInt(tmfc.substring(8, 10)) || 0;
        }

        for (let v = 4; v < cols.length; v++) {
            const raw = cols[v].trim().toLowerCase();
            // ★ 수정: 'nan' 문자열과 NaN 모두 명시적으로 걸러냄
            if (raw === 'nan' || raw === '') continue;
            const val = parseFloat(raw);
            if (!isNaN(val)) values.push(val);
        }
        break;
    }

    console.log(`[forecast] 파싱 완료 — baseHourUtc=${baseHourUtc}, 유효값 ${values.length}개:`, values);

    return {
        baseHourUtc,
        values: values.slice(0, 22),
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
    const { lat, lon, tm1 } = req.query;
    // raw 라우트도 동일하게 tm2 강제 계산
    const tm2Forced = fmtTm(new Date(parseTm(tm1).getTime() + 11 * 60 * 60 * 1000));
    const url = `https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph_sun_sat_ana_txt` +
        `?tm1=${tm1}&tm2=${tm2Forced}&int=30&lat=${lat}&lon=${lon}&authKey=${APIKEY}`;
    const raw = await fetch(url).then(r => r.text());
    res.type('text/plain').send(raw);
});

app.listen(PORT, () => console.log(`KMA Proxy on port ${PORT}`));