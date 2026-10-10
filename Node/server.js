const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = 3000;
const DATA_FILE = path.join(__dirname, 'scores.json');
const SECRET_KEY = "OmnibusKey"; // 유니티 NetworkManager.cs와 공유하는 암호화 키

app.use(cors());
app.use(express.json());

// 단순 XOR + Base64 복호화 함수
function decrypt(str) {
    try {
        let text = Buffer.from(str, 'base64').toString('utf8');
        let result = "";
        for (let i = 0; i < text.length; i++) {
            result += String.fromCharCode(text.charCodeAt(i) ^ SECRET_KEY.charCodeAt(i % SECRET_KEY.length));
        }
        return JSON.parse(result);
    } catch (e) {
        return null;
    }
}

// 단순 XOR + Base64 암호화 함수
function encrypt(obj) {
    let text = JSON.stringify(obj);
    let result = "";
    for (let i = 0; i < text.length; i++) {
        result += String.fromCharCode(text.charCodeAt(i) ^ SECRET_KEY.charCodeAt(i % SECRET_KEY.length));
    }
    return Buffer.from(result, 'utf8').toString('base64');
}

// 더미 데이터 (0, 1, 2 인덱스 및 game1, game2, game3 하위 호환)
function getInitialData() {
    return {
        "0": [{ name: "AAA", score: 100 }, { name: "BBB", score: 80 }, { name: "CCC", score: 50 }],
        "1": [{ name: "DDD", score: 150 }, { name: "EEE", score: 120 }, { name: "FFF", score: 90 }],
        "2": [{ name: "GGG", score: 200 }, { name: "HHH", score: 170 }, { name: "III", score: 130 }],
        "game1": [{ name: "AAA", score: 100 }, { name: "BBB", score: 80 }, { name: "CCC", score: 50 }],
        "game2": [{ name: "DDD", score: 150 }, { name: "EEE", score: 120 }, { name: "FFF", score: 90 }],
        "game3": [{ name: "GGG", score: 200 }, { name: "HHH", score: 170 }, { name: "III", score: 130 }]
    };
}

// JSON 읽기
function readScores() {
    if (!fs.existsSync(DATA_FILE)) {
        const initial = getInitialData();
        fs.writeFileSync(DATA_FILE, JSON.stringify(initial, null, 2));
        return initial;
    }
    try {
        return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
    } catch (e) {
        return getInitialData();
    }
}

// 1~3위 데이터 조회 (GET)
app.get('/leaderboard', (req, res) => {
    const scores = readScores();
    res.json({ data: encrypt(scores) });
});

// 신규 점수 등록 (POST)
app.post('/leaderboard', (req, res) => {
    const body = decrypt(req.body.data);
    if (!body || body.gameId === undefined || !body.name || body.score === undefined) {
        return res.status(400).json({ error: "Invalid Data" });
    }

    const { gameId, name, score } = body;
    let scores = readScores();

    if (!scores[gameId]) scores[gameId] = [];

    // 신규 데이터 삽입 후 내림차순 정렬 및 상위 3개 보존
    scores[gameId].push({ name: name.toUpperCase().substring(0, 3), score: parseInt(score) });
    scores[gameId].sort((a, b) => b.score - a.score);
    scores[gameId] = scores[gameId].slice(0, 3);

    fs.writeFileSync(DATA_FILE, JSON.stringify(scores, null, 2));
    res.json({ data: encrypt(scores) });
});

app.listen(PORT, () => {
    console.log(`[Omnibus Server] Running on http://localhost:${PORT}`);
});