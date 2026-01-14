using System;
using UnityEngine;

/// <summary>
/// 웨이브 타입
/// </summary>
public enum WaveType
{
    Manual,      // 수작업 (튜토리얼, 보스)
    Procedural   // 절차적 생성
}

/// <summary>
/// 웨이브 설정 정보
/// </summary>
[Serializable]
public class WaveConfig
{
    public int waveNumber;
    public WaveType waveType;
    public int budget;              // 절차적 생성 시 사용
    public WavePreset preset;       // 수작업 웨이브 시 사용

    public WaveConfig(int waveNumber, WaveType type)
    {
        this.waveNumber = waveNumber;
        this.waveType = type;
    }
}

/// <summary>
/// 생성된 웨이브 데이터 (절차적 생성 결과)
/// </summary>
[Serializable]
public class GeneratedWaveData
{
    public int waveNumber;
    public int budgetUsed;
    public int budgetTotal;
    public string patternName;
    public SpawnInfo[] spawnInfos;

    public GeneratedWaveData(int waveNumber, int budgetUsed, int budgetTotal, string patternName, SpawnInfo[] spawnInfos)
    {
        this.waveNumber = waveNumber;
        this.budgetUsed = budgetUsed;
        this.budgetTotal = budgetTotal;
        this.patternName = patternName;
        this.spawnInfos = spawnInfos;
    }
}

/// <summary>
/// 웨이브 예산 계산 유틸리티
/// </summary>
public static class WaveBudgetCalculator
{
    // 기본 예산 공식: BasePoints + (WaveNumber * GrowthRate)
    private const int BasePoints = 100;
    private const int GrowthRate = 50;

    /// <summary>
    /// 웨이브 번호로 예산 계산
    /// </summary>
    public static int CalculateBudget(int waveNumber)
    {
        return BasePoints + (waveNumber * GrowthRate);
    }

    /// <summary>
    /// 난이도 배율을 적용한 예산 계산 (향후 2-3단계에서 사용)
    /// </summary>
    public static int CalculateBudget(int waveNumber, float difficultyMultiplier)
    {
        int baseBudget = CalculateBudget(waveNumber);
        return Mathf.RoundToInt(baseBudget * difficultyMultiplier);
    }

    /// <summary>
    /// 웨이브 타입 결정
    /// </summary>
    public static WaveType GetWaveType(int waveNumber)
    {
        // 웨이브 10, 20, 30: 보스 (수작업)
        if (waveNumber == 10 || waveNumber == 20 || waveNumber == 30)
            return WaveType.Manual;

        // 나머지: 절차적 생성
        return WaveType.Procedural;
    }

    /// <summary>
    /// 웨이브 예산 정보 디버그 출력
    /// </summary>
    public static void DebugPrintBudgetTable(int maxWave = 30)
    {
        Debug.Log("=== Wave Budget Table ===");
        for (int i = 1; i <= maxWave; i++)
        {
            int budget = CalculateBudget(i);
            WaveType type = GetWaveType(i);
            Debug.Log($"Wave {i}: {budget} points ({type})");
        }
    }
}
