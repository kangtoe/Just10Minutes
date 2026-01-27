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

    [Tooltip("스폰 가능한 최소 시간 (초, elapsed time 기준, 작은 값 = 게임 초반)")]
    public float timeMin;

    [Tooltip("스폰 가능한 최대 시간 (초, elapsed time 기준, 큰 값 = 게임 후반)")]
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
    /// 적 프리팹 -> (spawnTimeMin, spawnTimeMax) 매핑
    /// spawnTimeMin: 스폰 가능한 최소 시간 (작은 값, 게임 초반)
    /// spawnTimeMax: 스폰 가능한 최대 시간 (큰 값, 게임 후반)
    /// Elapsed time 방식: 0초(0:00) → 600초(10:00)
    /// </summary>
    private static Dictionary<EnemyShip, (float min, float max)> timeRanges;

    private static bool isInitialized = false;

    /// <summary>
    /// Inspector에서 설정한 배열로 초기화
    /// </summary>
    public static void Initialize(EnemyTimeRange[] ranges)
    {
        timeRanges = new Dictionary<EnemyShip, (float, float)>();

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

            timeRanges[range.enemyPrefab] = (range.timeMin, range.timeMax);
        }

        isInitialized = true;
        Debug.Log($"[EnemyTimeRangeData] Initialized with {timeRanges.Count} enemy time ranges");
    }

    /// <summary>
    /// 특정 시간에 해당 적을 스폰할 수 있는지 확인
    /// </summary>
    /// <param name="enemyPrefab">적 프리팹</param>
    /// <param name="elapsedTime">경과 시간 (초 단위, 0 → 600)</param>
    /// <returns>스폰 가능 여부</returns>
    public static bool CanSpawnAtTime(EnemyShip enemyPrefab, float elapsedTime)
    {
        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return false;
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemyTimeRangeData] Null enemy prefab");
            return false;
        }

        if (!timeRanges.ContainsKey(enemyPrefab))
        {
            Debug.LogWarning($"[EnemyTimeRangeData] Unknown enemy: {enemyPrefab.name}");
            return false;
        }

        var (min, max) = timeRanges[enemyPrefab];
        return elapsedTime >= min && elapsedTime <= max;
    }

    /// <summary>
    /// 특정 시간에 스폰 가능한 모든 적 목록 반환
    /// </summary>
    /// <param name="elapsedTime">경과 시간 (초 단위, 0 → 600)</param>
    /// <returns>스폰 가능한 적 프리팹 목록</returns>
    public static List<EnemyShip> GetSpawnableEnemiesAtTime(float elapsedTime)
    {
        List<EnemyShip> result = new List<EnemyShip>();

        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return result;
        }

        foreach (var kvp in timeRanges)
        {
            EnemyShip enemyPrefab = kvp.Key;
            var (min, max) = kvp.Value;

            if (elapsedTime >= min && elapsedTime <= max)
            {
                result.Add(enemyPrefab);
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 적의 시간 범위 정보 조회
    /// </summary>
    /// <param name="enemyPrefab">적 프리팹</param>
    /// <param name="min">spawnTimeMin (out)</param>
    /// <param name="max">spawnTimeMax (out)</param>
    /// <returns>시간 범위 정보가 존재하는지 여부</returns>
    public static bool GetTimeRange(EnemyShip enemyPrefab, out float min, out float max)
    {
        min = 0f;
        max = 0f;

        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return false;
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemyTimeRangeData] Null enemy prefab");
            return false;
        }

        if (timeRanges.ContainsKey(enemyPrefab))
        {
            (min, max) = timeRanges[enemyPrefab];
            return true;
        }

        return false;
    }

    /// <summary>
    /// 등록된 모든 적 프리팹 목록 반환
    /// </summary>
    public static List<EnemyShip> GetAllEnemyPrefabs()
    {
        if (!isInitialized || timeRanges == null)
        {
            Debug.LogError("[EnemyTimeRangeData] Not initialized!");
            return new List<EnemyShip>();
        }

        return new List<EnemyShip>(timeRanges.Keys);
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
