using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 절차적 웨이브 생성 엔진
/// 예산 기반으로 적 조합 및 스폰 패턴 생성
/// </summary>
public class ProceduralWaveGenerator
{
    // 적 프리팹 레지스트리 (EnemySpawner에서 초기화)
    private static Dictionary<string, EnemyShip> enemyPrefabRegistry = new Dictionary<string, EnemyShip>();

    // 성능 제한
    private const int MAX_ENEMIES_PER_WAVE = 40;

    /// <summary>
    /// 적 프리팹 레지스트리 초기화 (게임 시작 시 EnemySpawner에서 호출)
    /// </summary>
    public static void Initialize(EnemyShip[] enemyPrefabs)
    {
        enemyPrefabRegistry.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[ProceduralWaveGenerator] No enemy prefabs provided for initialization!");
            return;
        }

        foreach (EnemyShip prefab in enemyPrefabs)
        {
            if (prefab != null)
            {
                enemyPrefabRegistry[prefab.name] = prefab;
                Debug.Log($"[ProceduralWaveGenerator] Registered enemy prefab: {prefab.name}");
            }
        }

        Debug.Log($"[ProceduralWaveGenerator] Initialized with {enemyPrefabRegistry.Count} enemy prefabs");
    }

    /// <summary>
    /// 메인 생성 메서드 - 웨이브 번호와 예산으로 웨이브 생성
    /// </summary>
    public static GeneratedWaveData GenerateWave(int waveNumber, int budget)
    {
        // 1단계: SmallSwarm 패턴만 구현
        return GenerateSmallSwarmWave(waveNumber, budget);
    }

    /// <summary>
    /// SmallSwarm 패턴: 다수의 약한 적
    /// 조건: 예산의 70% 이상을 Light 등급에 사용, 최소 5마리
    /// </summary>
    private static GeneratedWaveData GenerateSmallSwarmWave(int waveNumber, int budget)
    {
        List<SpawnInfo> spawnInfoList = new List<SpawnInfo>();
        int budgetRemaining = budget;
        int totalBudgetUsed = 0;

        // Light 등급 적 목록
        List<string> lightEnemies = EnemyCostData.GetEnemiesByTier(EnemyTier.Light);
        List<string> lightPlusEnemies = EnemyCostData.GetEnemiesByTier(EnemyTier.LightPlus);

        if (lightEnemies.Count == 0)
        {
            Debug.LogError("[ProceduralWaveGenerator] No Light enemies available!");
            return new GeneratedWaveData(waveNumber, 0, budget, "SmallSwarm", new SpawnInfo[0]);
        }

        // 조건: 예산의 70% 이상을 Light 등급에 사용
        int lightBudget = Mathf.RoundToInt(budget * 0.7f);

        // ========== 첫 번째 그룹: Light 적 ==========
        string selectedEnemy = lightEnemies[Random.Range(0, lightEnemies.Count)];
        int cost = EnemyCostData.GetCost(selectedEnemy);
        int count = Mathf.Max(5, lightBudget / cost); // 최소 5마리

        // 예산 초과 방지
        while (count * cost > budget * 0.6f && count > 5)
            count--;

        // 최대 개수 제한
        count = Mathf.Min(count, MAX_ENEMIES_PER_WAVE);

        int groupCost = count * cost;
        budgetRemaining -= groupCost;
        totalBudgetUsed += groupCost;

        EnemyShip prefab = LoadEnemyPrefab(selectedEnemy);
        if (prefab != null)
        {
            SpawnInfo spawnInfo = new SpawnInfo
            {
                spawnPrefab = prefab.gameObject,
                spawnTime = 0f,
                spawnSide = Random.value > 0.5f ? Edge.Random : Edge.Up,
                count = count,
                spawnInterval = Random.Range(0.2f, 0.5f)
            };
            spawnInfoList.Add(spawnInfo);

            Debug.Log($"[Wave {waveNumber} - Group 0] {selectedEnemy} x{count} @ 0s ({spawnInfo.spawnSide}) - Cost: {groupCost}");
        }

        // ========== 두 번째 그룹: 나머지 예산으로 추가 적 (선택적) ==========
        if (budgetRemaining > 30)
        {
            List<string> availableEnemies = new List<string>();
            availableEnemies.AddRange(lightEnemies);

            // Light+ 적 추가 가능 여부 확인
            if (budgetRemaining >= 40)
                availableEnemies.AddRange(lightPlusEnemies);

            // 이미 사용한 적 제외
            if (spawnInfoList.Count > 0 && spawnInfoList[0].spawnPrefab != null)
            {
                string usedEnemy = spawnInfoList[0].spawnPrefab.name;
                availableEnemies.Remove(usedEnemy);
            }

            if (availableEnemies.Count > 0)
            {
                string secondEnemy = availableEnemies[Random.Range(0, availableEnemies.Count)];
                int secondCost = EnemyCostData.GetCost(secondEnemy);
                int secondCount = budgetRemaining / secondCost;

                if (secondCount > 0)
                {
                    // 전체 적 수가 MAX_ENEMIES_PER_WAVE를 넘지 않도록
                    int currentTotal = count;
                    secondCount = Mathf.Min(secondCount, MAX_ENEMIES_PER_WAVE - currentTotal);

                    if (secondCount > 0)
                    {
                        int secondGroupCost = secondCount * secondCost;
                        budgetRemaining -= secondGroupCost;
                        totalBudgetUsed += secondGroupCost;

                        EnemyShip secondPrefab = LoadEnemyPrefab(secondEnemy);
                        if (secondPrefab != null)
                        {
                            SpawnInfo spawnInfo = new SpawnInfo
                            {
                                spawnPrefab = secondPrefab.gameObject,
                                spawnTime = Random.Range(3f, 5f), // 첫 그룹 후 3-5초 뒤
                                spawnSide = Edge.Random,
                                count = secondCount,
                                spawnInterval = Random.Range(0.3f, 0.6f)
                            };
                            spawnInfoList.Add(spawnInfo);

                            Debug.Log($"[Wave {waveNumber} - Group 1] {secondEnemy} x{secondCount} @ {spawnInfo.spawnTime}s ({spawnInfo.spawnSide}) - Cost: {secondGroupCost}");
                        }
                    }
                }
            }
        }

        GeneratedWaveData waveData = new GeneratedWaveData(
            waveNumber,
            totalBudgetUsed,
            budget,
            "SmallSwarm",
            spawnInfoList.ToArray()
        );

        // 로그 출력 (디버깅)
        float usagePercent = (float)totalBudgetUsed / budget * 100f;
        Debug.Log($"[Wave {waveNumber}] SmallSwarm - Budget: {totalBudgetUsed}/{budget} ({usagePercent:F1}%), Groups: {spawnInfoList.Count}");

        return waveData;
    }

    /// <summary>
    /// 적 프리팹 가져오기 (레지스트리에서)
    /// </summary>
    private static EnemyShip LoadEnemyPrefab(string prefabName)
    {
        if (enemyPrefabRegistry.ContainsKey(prefabName))
        {
            return enemyPrefabRegistry[prefabName];
        }

        Debug.LogError($"[ProceduralWaveGenerator] Enemy prefab not found in registry: {prefabName}. Did you forget to call Initialize()?");
        return null;
    }
}
