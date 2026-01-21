using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 시간 기반 스폰 시스템의 핵심 관리자
/// - 14분 elapsed 타이머 사용 (TimeRecordManager)
/// - 예산 누적 시스템
/// - 주기적 스폰 체크 실행
/// - 시간 범위 기반 스폰 풀 관리
/// </summary>
public class TimeBasedSpawnManager : MonoSingleton<TimeBasedSpawnManager>
{
    [Header("=== Enemy Time Ranges ===")]
    [SerializeField] private EnemyTimeRange[] enemyTimeRanges;

    [Button("자동으로 적 시간 범위 설정")]
    private void AutoSetupEnemyTimeRanges()
    {
#if UNITY_EDITOR
        List<EnemyTimeRange> ranges = new List<EnemyTimeRange>();

        // 12개 적의 시간 범위 정의 (elapsed time 기준: 0초부터 시작)
        AddEnemyRange(ranges, "Enemy_light_child", 0, 180);      // 0:00 ~ 3:00
        AddEnemyRange(ranges, "Enemy_light_kido", 30, 210);      // 0:30 ~ 3:30
        AddEnemyRange(ranges, "Enemy_light_thunder", 60, 240);   // 1:00 ~ 4:00
        AddEnemyRange(ranges, "Enemy_mid_Ghost", 120, 420);      // 2:00 ~ 7:00
        AddEnemyRange(ranges, "Enemy_mid_Hornet", 150, 450);     // 2:30 ~ 7:30
        AddEnemyRange(ranges, "Enemy_mid_master", 180, 480);     // 3:00 ~ 8:00
        AddEnemyRange(ranges, "Enemy_mid_Knight", 210, 540);     // 3:30 ~ 9:00
        AddEnemyRange(ranges, "Enemy_mid_sniper", 240, 570);     // 4:00 ~ 9:30
        AddEnemyRange(ranges, "Enemy_mid_tank", 270, 600);       // 4:30 ~ 10:00
        AddEnemyRange(ranges, "Enemy_mid_Spiral", 300, 630);     // 5:00 ~ 10:30
        AddEnemyRange(ranges, "Enemy_heavy_mother", 360, 780);   // 6:00 ~ 13:00
        AddEnemyRange(ranges, "Enemy_heavy_Gunship", 420, 840);  // 7:00 ~ 14:00

        enemyTimeRanges = ranges.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[TimeBasedSpawnManager] 자동 설정 완료: {enemyTimeRanges.Length}개 적 (elapsed time 기준)");
#endif
    }

#if UNITY_EDITOR
    private void AddEnemyRange(List<EnemyTimeRange> ranges, string enemyName, float timeMin, float timeMax)
    {
        string path = $"Assets/Prefabs/Enemys/{enemyName}.prefab";
        EnemyShip prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyShip>(path);

        if (prefab != null)
        {
            ranges.Add(new EnemyTimeRange(prefab, timeMin, timeMax));
            Debug.Log($"✓ {enemyName}: {timeMin}~{timeMax}초");
        }
        else
        {
            Debug.LogWarning($"✗ {enemyName} 프리팹을 찾을 수 없습니다: {path}");
        }
    }
#endif

    [Header("=== Timer Settings ===")]
    [SerializeField] private float gameDuration = 840f; // 14분
    private bool isRunning = false;

    [Header("=== Budget Settings ===")]
    [SerializeField] private float initialBudget = 50f;
    [SerializeField, ReadOnly] private float currentBudget;

    [Header("Budget Accumulation Rate (Linear Ramp)")]
    [SerializeField, ReadOnly] private float currentBudgetRate = 10f; // 현재 증가율 (실시간 계산)
    [SerializeField] private float minBudgetRate = 10f;  // 초기 예산 증가율 (게임 시작)
    [SerializeField] private float maxBudgetRate = 25f;  // 최종 예산 증가율 (최고 난이도)
    [SerializeField] private float budgetRateRampUpTime = 840f; // 최종값 도달 시간 (초)
    

    [Header("=== Spawn Check Settings ===")]
    [SerializeField] private float spawnCheckInterval = 1f;
    private float timeSinceLastCheck = 0f;

    [Header("Skip System")]
    [SerializeField, Range(0f, 0.99f)] private float skipProbability = 0.3f; // 건너뛰기 확률
    [SerializeField] private float maxSpawnSkipTime = 5f; // 강제 스폰까지의 최대 대기 시간 (초)
    private float timeSinceLastSpawn = 0f;

    [Header("=== Spawn Pool Settings ===")]
    [SerializeField] private float poolRefreshInterval = 5f;
    private float timeSinceLastPoolRefresh = 0f;
    private List<string> currentSpawnPool = new List<string>();
    

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLogs = true;

    private void Start()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // 예산 초기화
        currentBudget = initialBudget;
        UpdateBudgetAccumulationRate();

        // 적 시간 범위 데이터 초기화
        if (enemyTimeRanges != null && enemyTimeRanges.Length > 0)
        {
            EnemyTimeRangeData.Initialize(enemyTimeRanges);
            if (showDebugLogs)
                Debug.Log($"[TimeBasedSpawn] Initialized {enemyTimeRanges.Length} enemy time ranges");

            // 적 프리팹 레지스트리 초기화 (enemyTimeRanges에서 추출)
            EnemyShip[] enemyPrefabs = ExtractEnemyPrefabs(enemyTimeRanges);
            if (enemyPrefabs.Length > 0)
            {
                ProceduralWaveGenerator.Initialize(enemyPrefabs);
                if (showDebugLogs)
                    Debug.Log($"[TimeBasedSpawn] Initialized with {enemyPrefabs.Length} enemy prefabs");
            }
        }
        else
        {
            Debug.LogError("[TimeBasedSpawn] Enemy time ranges not assigned!");
        }

        // TimeRecordManager 시작
        if (TimeRecordManager.Instance != null)
        {
            TimeRecordManager.Instance.SetActiveCount(true);
        }

        // 초기 스폰 풀 갱신
        RefreshSpawnPool();

        // 시스템 시작
        isRunning = true;
        if (showDebugLogs)
            Debug.Log($"[TimeBasedSpawn] System started - Duration: {gameDuration}s, Initial Budget: {initialBudget}, Budget Rate: {minBudgetRate}→{maxBudgetRate}/s over {budgetRateRampUpTime}s");
    }

    /// <summary>
    /// EnemyTimeRange 배열에서 EnemyShip 프리팹 배열 추출
    /// </summary>
    private EnemyShip[] ExtractEnemyPrefabs(EnemyTimeRange[] ranges)
    {
        List<EnemyShip> prefabs = new List<EnemyShip>();
        foreach (var range in ranges)
        {
            if (range.enemyPrefab != null)
            {
                prefabs.Add(range.enemyPrefab);
            }
        }
        return prefabs.ToArray();
    }

    /// <summary>
    /// 현재 경과 시간 가져오기 (TimeRecordManager의 count-up 시간)
    /// </summary>
    private float GetElapsedTime()
    {
        if (TimeRecordManager.Instance == null) return 0f;
        return TimeRecordManager.Instance.TimeRecord;
    }

    private void Update()
    {
        if (!isRunning) return;

        float deltaTime = Time.deltaTime;

        // 1. 게임 종료 확인
        if (GetElapsedTime() >= gameDuration)
        {
            OnGameTimeEnd();
            return;
        }

        // 2. 예산 누적
        UpdateBudget(deltaTime);

        // 3. 강제 스폰 타이머 업데이트
        timeSinceLastSpawn += deltaTime;

        // 4. 스폰 풀 갱신 (주기적)
        timeSinceLastPoolRefresh += deltaTime;
        if (timeSinceLastPoolRefresh >= poolRefreshInterval)
        {
            RefreshSpawnPool();
            timeSinceLastPoolRefresh = 0f;
        }

        // 5. 스폰 체크 (주기적)
        UpdateSpawnCheck(deltaTime);

        // 6. UI 업데이트
        UpdateUI();
    }

    #region Timer Management

    private void OnGameTimeEnd()
    {
        isRunning = false;
        if (showDebugLogs)
            Debug.Log("[TimeBasedSpawn] Game time ended!");

        // TODO: 게임 종료 처리
    }

    #endregion

    #region Budget Management

    private void UpdateBudget(float deltaTime)
    {
        // 시간 비례 예산 증가율 조정
        UpdateBudgetAccumulationRate();

        // 예산 누적
        currentBudget += currentBudgetRate * deltaTime;
    }

    private void UpdateBudgetAccumulationRate()
    {
        float elapsed = GetElapsedTime();
        float t = Mathf.Clamp01(elapsed / budgetRateRampUpTime); // 0~1 진행도

        currentBudgetRate = Mathf.Lerp(minBudgetRate, maxBudgetRate, t);
    }

    private int GetCurrentPhase()
    {
        float elapsed = GetElapsedTime();
        if (elapsed < 240f) return 1; // Phase 1: 0~240초 (0:00~4:00)
        if (elapsed < 480f) return 2; // Phase 2: 240~480초 (4:00~8:00)
        return 3; // Phase 3: 480~840초 (8:00~14:00)
    }

    #endregion

    #region Spawn Pool Management

    private void RefreshSpawnPool()
    {
        float elapsed = GetElapsedTime();
        List<string> newPool = EnemyTimeRangeData.GetSpawnableEnemiesAtTime(elapsed);

        if (newPool.Count != currentSpawnPool.Count)
        {
            currentSpawnPool = newPool;
            if (showDebugLogs)
                Debug.Log($"[SpawnPool] Refreshed at {FormatTime(elapsed)} - {currentSpawnPool.Count} enemies available");
        }
        else
        {
            currentSpawnPool = newPool;
        }
    }

    #endregion

    #region Spawn Check

    private void UpdateSpawnCheck(float deltaTime)
    {
        timeSinceLastCheck += deltaTime;

        if (timeSinceLastCheck >= spawnCheckInterval)
        {
            PerformSpawnCheck();
            timeSinceLastCheck = 0f;
        }
    }

    private void PerformSpawnCheck()
    {
        if (currentSpawnPool.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[Spawn] No enemies available in spawn pool at {FormatTime(GetElapsedTime())}");
            return;
        }

        // 현재 예산 내에서 스폰 가능한 적 필터링
        List<string> affordableEnemies = new List<string>();
        foreach (string enemyName in currentSpawnPool)
        {
            int cost = EnemyCostData.GetCost(enemyName);
            if (cost <= currentBudget)
            {
                affordableEnemies.Add(enemyName);
            }
        }

        if (affordableEnemies.Count == 0)
        {
            // 예산 부족, 스폰 불가 (예산 축적)
            return;
        }

        // 강제 스폰 체크: MaxIdleTime 도달 여부
        bool forceSpawn = timeSinceLastSpawn >= maxSpawnSkipTime;

        // 건너뛰기 체크 (강제 스폰이 아닐 때만)
        if (!forceSpawn && Random.value < skipProbability)
        {
            // 스폰 건너뛰기 (예산 유지)
            if (showDebugLogs)
                Debug.Log($"[Spawn] Skipped (Probability: {skipProbability * 100}%, Idle Time: {timeSinceLastSpawn:F1}s)");
            return;
        }

        // 랜덤 선택
        string selectedEnemy = affordableEnemies[Random.Range(0, affordableEnemies.Count)];
        int enemyCost = EnemyCostData.GetCost(selectedEnemy);

        // 스폰 실행
        SpawnEnemy(selectedEnemy);

        // 예산 소모
        currentBudget -= enemyCost;

        // 마지막 스폰 시간 초기화
        timeSinceLastSpawn = 0f;

        if (showDebugLogs)
        {
            string forceFlag = forceSpawn ? " [FORCED]" : "";
            Debug.Log($"[Spawn] {selectedEnemy} spawned{forceFlag} (Cost: {enemyCost}, Remaining Budget: {currentBudget:F0})");
        }
    }

    private void SpawnEnemy(string enemyName)
    {
        // 프리팹 찾기
        EnemyShip prefab = LoadEnemyPrefab(enemyName);
        if (prefab == null)
        {
            Debug.LogError($"[Spawn] Enemy prefab not found: {enemyName}");
            return;
        }

        // ObjectSpawner를 통해 스폰
        GameObject enemy = ObjectSpawner.Instance.SpawnObject(
            prefab.gameObject,
            Edge.Random
        );
    }

    private EnemyShip LoadEnemyPrefab(string prefabName)
    {
        foreach (var range in enemyTimeRanges)
        {
            if (range.enemyPrefab != null && range.enemyPrefab.name == prefabName)
                return range.enemyPrefab;
        }
        return null;
    }

    #endregion

    #region UI Update

    private void UpdateUI()
    {
        // UiManager를 통해 UI 업데이트
        if (UiManager.Instance != null)
        {
            // 시간 표시는 TimeRecordManager가 담당
            // 예산 및 Phase 디버그 텍스트만 업데이트
            UiManager.Instance.SetBudgetDebugText(currentBudget, currentBudgetRate);
            UiManager.Instance.SetPhaseDebugText(GetCurrentPhase(), GetElapsedTime());
        }
    }

    #endregion

    #region Utility

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes}:{seconds:00}";
    }

    #endregion

    #region Public API

    /// <summary>
    /// 시스템 일시 정지/재개
    /// </summary>
    public void SetPaused(bool paused)
    {
        isRunning = !paused;
    }

    /// <summary>
    /// 현재 상태 정보 조회 (디버그용)
    /// </summary>
    public (float elapsedTime, float budget, int phase, int poolSize) GetStatus()
    {
        return (GetElapsedTime(), currentBudget, GetCurrentPhase(), currentSpawnPool.Count);
    }

    #endregion
}
