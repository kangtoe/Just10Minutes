using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 스폰 가능한 시간 범위를 나타내는 직렬화 가능한 구조체
/// </summary>
[System.Serializable]
public class EnemyTimeRange
{
    [Tooltip("적 프리팹")]
    public EnemyShip enemyPrefab;

    [Tooltip("스폰 가능한 최소 시간 (초, 작은 값 = 게임 후반)")]
    public float timeMin;

    [Tooltip("스폰 가능한 최대 시간 (초, 큰 값 = 게임 초반)")]
    public float timeMax;

    public EnemyTimeRange(EnemyShip prefab, float min, float max)
    {
        enemyPrefab = prefab;
        timeMin = min;
        timeMax = max;
    }
}

/// <summary>
/// 각 적의 스폰 가능한 시간 범위를 관리하는 정적 클래스
/// 시간 기반 스폰 시스템에서 사용
/// </summary>
public static class EnemyTimeRangeData
{
    /// <summary>
    /// 적 이름 -> (spawnTimeMin, spawnTimeMax) 매핑
    /// spawnTimeMin: 스폰 가능한 최소 시간 (작은 값, 게임 후반)
    /// spawnTimeMax: 스폰 가능한 최대 시간 (큰 값, 게임 초반)
    /// 카운트다운 방식: 840초(14:00) → 0초(0:00)
    /// </summary>
    private static Dictionary<string, (float min, float max)> timeRanges;

    private static bool isInitialized = false;

    /// <summary>
    /// Inspector에서 설정한 배열로 초기화
    /// </summary>
    public static void Initialize(EnemyTimeRange[] ranges)
    {
        timeRanges = new Dictionary<string, (float, float)>();

        if (ranges == null || ranges.Length == 0)
        {
            Debug.LogError("[EnemyTimeRangeData] No time ranges provided!");
            return;
        }

        foreach (var range in ranges)
        {
            if (range.enemyPrefab == null)
            {
                Debug.LogWarning("[EnemyTimeRangeData] Skipping range with null enemy prefab");
                continue;
            }

            string enemyName = range.enemyPrefab.name;
            timeRanges[enemyName] = (range.timeMin, range.timeMax);
        }

        isInitialized = true;
        Debug.Log($"[EnemyTimeRangeData] Initialized with {timeRanges.Count} enemy time ranges");
    }

    /// <summary>
    /// 특정 시간에 해당 적을 스폰할 수 있는지 확인
    /// </summary>
    /// <param name="enemyName">적 프리팹 이름 (예: "Enemy_light_child")</param>
    /// <param name="timeRemaining">남은 시간 (초 단위, 840 → 0)</param>
    /// <returns>스폰 가능 여부</returns>
    public static bool CanSpawnAtTime(string enemyName, float timeRemaining)
    {
        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return false;
        }

        if (!timeRanges.ContainsKey(enemyName))
        {
            Debug.LogWarning($"[EnemyTimeRangeData] Unknown enemy: {enemyName}");
            return false;
        }

        var (min, max) = timeRanges[enemyName];
        return timeRemaining >= min && timeRemaining <= max;
    }

    /// <summary>
    /// 특정 시간에 스폰 가능한 모든 적 목록 반환
    /// </summary>
    /// <param name="timeRemaining">남은 시간 (초 단위, 840 → 0)</param>
    /// <returns>스폰 가능한 적 이름 목록</returns>
    public static List<string> GetSpawnableEnemiesAtTime(float timeRemaining)
    {
        List<string> result = new List<string>();

        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return result;
        }

        foreach (var kvp in timeRanges)
        {
            string enemyName = kvp.Key;
            var (min, max) = kvp.Value;

            if (timeRemaining >= min && timeRemaining <= max)
            {
                result.Add(enemyName);
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 적의 시간 범위 정보 조회
    /// </summary>
    /// <param name="enemyName">적 프리팹 이름</param>
    /// <param name="min">spawnTimeMin (out)</param>
    /// <param name="max">spawnTimeMax (out)</param>
    /// <returns>시간 범위 정보가 존재하는지 여부</returns>
    public static bool GetTimeRange(string enemyName, out float min, out float max)
    {
        min = 0f;
        max = 0f;

        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return false;
        }

        if (timeRanges.ContainsKey(enemyName))
        {
            (min, max) = timeRanges[enemyName];
            return true;
        }

        return false;
    }

    /// <summary>
    /// 등록된 모든 적 이름 목록 반환
    /// </summary>
    public static List<string> GetAllEnemyNames()
    {
        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return new List<string>();
        }

        return new List<string>(timeRanges.Keys);
    }

    /// <summary>
    /// 디버그용: 특정 시간대의 스폰 가능 적 수 반환
    /// </summary>
    public static int GetSpawnableCount(float timeRemaining)
    {
        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return 0;
        }

        int count = 0;
        foreach (var kvp in timeRanges)
        {
            var (min, max) = kvp.Value;
            if (timeRemaining >= min && timeRemaining <= max)
            {
                count++;
            }
        }
        return count;
    }
}
