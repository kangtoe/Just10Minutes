using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemySpawnTimes.csv 파일을 로드하여 EnemyTimeRange 배열로 변환
/// </summary>
public static class EnemySpawnTimesLoader
{
    private const string LOADER_NAME = "EnemySpawnTimesLoader";
    private static readonly string[] REQUIRED_COLUMNS = { "Name", "SpawnTime", "DespawnTime" };

    /// <summary>
    /// CSV 파일에서 EnemyTimeRange 배열 로드
    /// </summary>
    /// <param name="csvAsset">EnemySpawnTimes.csv TextAsset</param>
    /// <returns>EnemyTimeRange 배열</returns>
    public static EnemyTimeRange[] LoadFromCsv(TextAsset csvAsset)
    {
        List<Dictionary<string, string>> rows = CsvReader.LoadCsvRows(csvAsset, LOADER_NAME);
        if (rows.Count == 0)
        {
            return new EnemyTimeRange[0];
        }

        List<EnemyTimeRange> ranges = new List<EnemyTimeRange>();

        foreach (var row in rows)
        {
            // 필수 컬럼 확인
            if (!CsvReader.ValidateRequiredColumns(row, REQUIRED_COLUMNS, LOADER_NAME))
            {
                continue;
            }

            string enemyName = row["Name"];

            // SpawnTime 파싱
            if (!CsvReader.TryParseFloat(row["SpawnTime"], "SpawnTime", LOADER_NAME, out float spawnTime))
            {
                continue;
            }

            // DespawnTime 파싱 (-1이면 무한대)
            float despawnTime;
            if (row["DespawnTime"] == "-1")
            {
                despawnTime = float.MaxValue;
            }
            else if (!CsvReader.TryParseFloat(row["DespawnTime"], "DespawnTime", LOADER_NAME, out despawnTime))
            {
                continue;
            }

            // 프리팹 로드
            EnemyShip prefab = LoadEnemyPrefab(enemyName);
            if (prefab == null)
            {
                Debug.LogWarning($"[{LOADER_NAME}] Could not load prefab for {enemyName}");
                continue;
            }

            // EnemyTimeRange 생성
            EnemyTimeRange range = new EnemyTimeRange(prefab, spawnTime, despawnTime);
            ranges.Add(range);

            Debug.Log($"[{LOADER_NAME}] Loaded {enemyName}: {spawnTime}s ~ {despawnTime}s");
        }

        CsvReader.LogLoadComplete(ranges.Count, "enemy time ranges", LOADER_NAME);
        return ranges.ToArray();
    }

    /// <summary>
    /// 적 프리팹 로드 (Resources 폴더 또는 AssetDatabase 사용)
    /// </summary>
    private static EnemyShip LoadEnemyPrefab(string enemyName)
    {
        // 1. Resources 폴더에서 로드 시도
        EnemyShip prefab = Resources.Load<EnemyShip>($"Prefabs/Enemys/Enemy_{enemyName}");
        if (prefab != null)
            return prefab;

        // 2. AssetDatabase 사용 (에디터 전용)
#if UNITY_EDITOR
        string path = $"Assets/Prefabs/Enemys/Enemy_{enemyName}.prefab";
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyShip>(path);
        if (prefab != null)
            return prefab;
#endif

        return null;
    }
}
