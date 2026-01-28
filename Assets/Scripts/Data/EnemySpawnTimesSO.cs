using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 시간 정보를 저장하는 단일 항목
/// </summary>
[System.Serializable]
public class EnemySpawnTimeData
{
    [Tooltip("적 프리팹")]
    public EnemyShip enemyPrefab;

    [Tooltip("스폰 시작 시간 (초)")]
    [Min(0f)]
    public float spawnTime = 0f;

    [Tooltip("스폰 종료 시간 (초, -1 = 무제한)")]
    public float despawnTime = -1f;

    /// <summary>
    /// EnemyTimeRange로 변환
    /// </summary>
    public EnemyTimeRange ToTimeRange()
    {
        float endTime = despawnTime == -1f ? float.MaxValue : despawnTime;
        return new EnemyTimeRange(enemyPrefab, spawnTime, endTime);
    }
}

/// <summary>
/// 모든 적의 스폰 시간 정보를 관리하는 ScriptableObject
/// CSV 대신 Inspector에서 직접 편집 가능
/// </summary>
[CreateAssetMenu(fileName = "EnemySpawnTimes", menuName = "Game/Enemy Spawn Times", order = 1)]
public class EnemySpawnTimesSO : ScriptableObject
{
    [Header("=== Enemy Spawn Time Settings ===")]
    [Tooltip("모든 적의 스폰 시간 정보 (Inspector에서 직접 편집)")]
    public List<EnemySpawnTimeData> spawnTimes = new List<EnemySpawnTimeData>();

    /// <summary>
    /// EnemyTimeRange 배열로 변환
    /// </summary>
    public EnemyTimeRange[] ToTimeRanges()
    {
        if (spawnTimes == null || spawnTimes.Count == 0)
        {
            Debug.LogWarning("[EnemySpawnTimesSO] No spawn time data available!");
            return new EnemyTimeRange[0];
        }

        List<EnemyTimeRange> ranges = new List<EnemyTimeRange>();

        foreach (var data in spawnTimes)
        {
            if (data == null || data.enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawnTimesSO] Skipping null entry");
                continue;
            }

            ranges.Add(data.ToTimeRange());
        }

        Debug.Log($"[EnemySpawnTimesSO] Loaded {ranges.Count} spawn time entries");
        return ranges.ToArray();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: 스폰 시간 순서로 정렬
    /// </summary>
    [ContextMenu("Sort by Spawn Time")]
    private void SortBySpawnTime()
    {
        spawnTimes.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[EnemySpawnTimesSO] Sorted {spawnTimes.Count} entries by spawn time");
    }

    /// <summary>
    /// 에디터 전용: 적 이름 순서로 정렬
    /// </summary>
    [ContextMenu("Sort by Enemy Name")]
    private void SortByEnemyName()
    {
        spawnTimes.Sort((a, b) =>
        {
            if (a.enemyPrefab == null || b.enemyPrefab == null) return 0;
            return a.enemyPrefab.name.CompareTo(b.enemyPrefab.name);
        });
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[EnemySpawnTimesSO] Sorted {spawnTimes.Count} entries by enemy name");
    }

    /// <summary>
    /// 에디터 전용: 검증
    /// </summary>
    private void OnValidate()
    {
        // 중복된 프리팹 체크
        HashSet<EnemyShip> seen = new HashSet<EnemyShip>();
        foreach (var data in spawnTimes)
        {
            if (data != null && data.enemyPrefab != null)
            {
                if (seen.Contains(data.enemyPrefab))
                {
                    Debug.LogWarning($"[EnemySpawnTimesSO] Duplicate enemy prefab detected: {data.enemyPrefab.name}", this);
                }
                seen.Add(data.enemyPrefab);
            }
        }
    }
#endif
}
