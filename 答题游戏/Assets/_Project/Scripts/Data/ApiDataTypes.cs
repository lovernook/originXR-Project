using System;
using System.Collections.Generic;

namespace OriginXR.Data
{
    // ===================== API 响应 =====================

    [Serializable]
    public class ApiResponse<T>
    {
        public bool success = true;
        public string message;
        public T data;
        public long timestamp;

        // 兼容旧版
        public int code { get => success ? 0 : 1; set => success = value == 0; }
    }

    // ===================== 认证 =====================

    [Serializable]
    public class LoginData
    {
        public string accessToken;
        public string refreshToken;
        public UserData user;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
        public string nickname;
    }

    // ===================== 游戏 =====================

    [Serializable]
    public class BattleStartData
    {
        public string sessionId;
        public string stageId;
        public string stageName;
        public string bossName;
        public int bossHp;
        public int maxLives;
        public int totalQuestions;
        public int currentIndex;
        public List<QuestionData> questions;
        public QuestionData currentQuestion;    // 后端返回单题时用这个
    }

    [Serializable]
    public class AnswerResultData
    {
        public bool isCorrect;
        public string correctAnswer;
        public string explanation;
        public int scoreGained;
        public int combo;
        public int comboBonus;
        public int lives;
        public int score;              // 累计总分（后端计算）
        public int currentIndex;
        public bool isSuspicious;
        public bool finished;
        public AnswerSummary summary;
    }

    [Serializable]
    public class AnswerSummary
    {
        public bool passed;
        public int score;
        public float accuracy;
        public int correctCount;
        public int totalQuestions;
        public int maxCombo;
        public int stars;
        public int expGained;
        public int goldGained;
        public int timeSpent;
    }

    [Serializable]
    public class SubmitAnswerBody
    {
        public string questionId;
        public string selectedOption;
        public float usedTime;
        public long timestampMs;
    }

    [Serializable]
    public class DailyChallengeData
    {
        public string rule;
        public int remainingAttempts;
        public List<QuestionData> questions;
    }

    [Serializable]
    public class TowerStartData
    {
        public int floor;
        public List<QuestionData> questions;
    }

    [Serializable]
    public class RankEntryData
    {
        public int rank;
        public string playerId;
        public string username;
        public int level;
        public long score;
    }

    [Serializable]
    public class UserProgressData
    {
        public string knowledgePointId;
        public string knowledgePointName;
        public float masteryLevel;
        public int totalAnswered;
        public int correctAnswered;
    }
}
