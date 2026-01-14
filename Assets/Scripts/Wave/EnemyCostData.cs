using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 등급 분류
/// </summary>
public enum EnemyTier
{
    Light,      // 20-30
    LightPlus,  // 40
    Mid,        // 80-150
    MidPlus,    // 150-200
    Heavy,      // 300-400
    Boss        // 1500
}

/// <summary>
/// 적 비용 테이블 - 절차적 웨이브 생성을 위한 정적 데이터
/// </summary>
public static class EnemyCostData
{
    // 적 프리팹 이름 -> 비용 매핑
    private static Dictionary<string, int> enemyCosts = new Dictionary<string, int>()
    {
        // Light 등급 (20-30)
        { "Enemy_light_child", 20 },      // point: 50
        { "Enemy_light_kido", 20 },       // point: 60
        { "Enemy_light_thunder", 25 },    // point: 80

        // Light+ 등급 (40)
        { "Enemy_light_shield", 40 },     // point: 100, 높은 내구도

        // Mid 등급 (80-150)
        { "Enemy_mid_Ghost", 80 },        // point: 250
        { "Enemy_mid_Hornet", 90 },       // point: 350, 다중 탄막
        { "Enemy_mid_Knight", 120 },      // point: 450
        { "Enemy_mid_sniper", 130 },      // point: 500
        { "Enemy_mid_tank", 140 },        // point: 550
        { "Enemy_mid_Spiral", 150 },      // point: 600
        { "Enemy_mid_master", 100 },      // point: 400

        // Heavy 등급 (300-400)
        { "Enemy_heavy_mother", 350 },    // point: 800, 드론 소환
        { "Enemy_heavy_Gunship", 400 },   // point: 1000

        // Boss 등급
        { "Enemy_Boss", 1500 }            // point: 1000
    };

    private static Dictionary<string, EnemyTier> enemyTiers = new Dictionary<string, EnemyTier>()
    {
        { "Enemy_light_child", EnemyTier.Light },
        { "Enemy_light_kido", EnemyTier.Light },
        { "Enemy_light_thunder", EnemyTier.Light },
        { "Enemy_light_shield", EnemyTier.LightPlus },

        { "Enemy_mid_Ghost", EnemyTier.Mid },
        { "Enemy_mid_Hornet", EnemyTier.Mid },
        { "Enemy_mid_Knight", EnemyTier.MidPlus },
        { "Enemy_mid_sniper", EnemyTier.MidPlus },
        { "Enemy_mid_tank", EnemyTier.MidPlus },
        { "Enemy_mid_Spiral", EnemyTier.MidPlus },
        { "Enemy_mid_master", EnemyTier.Mid },

        { "Enemy_heavy_mother", EnemyTier.Heavy },
        { "Enemy_heavy_Gunship", EnemyTier.Heavy },

        { "Enemy_Boss", EnemyTier.Boss }
    };

    /// <summary>
    /// 적 프리팹 이름으로 비용 조회
    /// </summary>
    public static int GetCost(string prefabName)
    {
        if (enemyCosts.ContainsKey(prefabName))
            return enemyCosts[prefabName];

        Debug.LogWarning($"[EnemyCostData] Unknown enemy prefab: {prefabName}");
        return 50; // 기본값
    }

    /// <summary>
    /// 적 GameObject로 비용 조회
    /// </summary>
    public static int GetCost(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[EnemyCostData] Null prefab passed to GetCost");
            return 0;
        }
        return GetCost(prefab.name);
    }

    /// <summary>
    /// 적 프리팹 이름으로 등급 조회
    /// </summary>
    public static EnemyTier GetTier(string prefabName)
    {
        if (enemyTiers.ContainsKey(prefabName))
            return enemyTiers[prefabName];

        Debug.LogWarning($"[EnemyCostData] Unknown enemy tier: {prefabName}");
        return EnemyTier.Light;
    }

    /// <summary>
    /// 적 GameObject로 등급 조회
    /// </summary>
    public static EnemyTier GetTier(GameObject prefab)
    {
        if (prefab == null) return EnemyTier.Light;
        return GetTier(prefab.name);
    }

    /// <summary>
    /// 특정 등급의 모든 적 목록 반환
    /// </summary>
    public static List<string> GetEnemiesByTier(EnemyTier tier)
    {
        List<string> result = new List<string>();
        foreach (var kvp in enemyTiers)
        {
            if (kvp.Value == tier)
                result.Add(kvp.Key);
        }
        return result;
    }

    /// <summary>
    /// 특정 예산 이하의 적 목록 반환 (등급 필터 옵션)
    /// </summary>
    public static List<string> GetEnemiesByCost(int maxCost, EnemyTier? tierFilter = null)
    {
        List<string> result = new List<string>();
        foreach (var kvp in enemyCosts)
        {
            if (kvp.Value <= maxCost)
            {
                if (tierFilter == null || GetTier(kvp.Key) == tierFilter.Value)
                    result.Add(kvp.Key);
            }
        }
        return result;
    }

    /// <summary>
    /// 모든 적 비용 정보 출력 (디버깅용)
    /// </summary>
    public static void DebugPrintAllCosts()
    {
        Debug.Log("=== Enemy Cost Table ===");
        foreach (var kvp in enemyCosts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} (Tier: {GetTier(kvp.Key)})");
        }
    }
}
