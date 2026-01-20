using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시간 기반 스폰 시스템의 핵심 관리자
/// - 14분 카운트다운 타이머 관리
/// - 예산 누적 시스템
/// - 주기적 스폰 체크 실행
/// - 시간 범위 기반 스폰 풀 관리
/// </summary>
public class TimeBasedSpawnManager : MonoSingleton<TimeBasedSpawnManager>
{
    [Header("=== Enemy Time Ranges ===")]
    [SerializeField] private EnemyTimeRange[] enemyTimeRanges;

    [Header("=== Timer Settings ===")]
    [SerializeField] private float gameDuration = 840f; // 14분
    private bool isRunning = false;

    [Header("=== Budget Settings ===")]
    [SerializeField] private float initialBudget = 50f;
    private float currentBudget;
    private float budgetAccumulationRate = 10f; // 초당 증가량

    [Header("=== Spawn Check Settings ===")]
    [SerializeField] private float spawnCheckInterval = 1f;
    private float timeSinceLastCheck = 0f;

    [Header("=== Spawn Pool Settings ===")]
    private List<string> currentSpawnPool = new List<string>();
    [SerializeField] private float poolRefreshInterval = 5f;
    private float timeSinceLastPoolRefresh = 0f;

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
            Debug.Log($"[TimeBasedSpawn] System started - Duration: {gameDuration}s, Initial Budget: {initialBudget}");
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
    /// TimeRecordManager의 count-up 시간을 기반으로 남은 시간 계산
    /// </summary>
    private float GetGameTimeRemaining()
    {
        if (TimeRecordManager.Instance == null) return gameDuration;
        float elapsed = TimeRecordManager.Instance.TimeRecord;
        return Mathf.Max(0f, gameDuration - elapsed);
    }

    private void Update()
    {
        if (!isRunning) return;

        float deltaTime = Time.deltaTime;

        // 1. 게임 종료 확인
        if (GetGameTimeRemaining() <= 0f)
        {
            OnGameTimeEnd();
            return;
        }

        // 2. 예산 누적
        UpdateBudget(deltaTime);

        // 3. 스폰 풀 갱신 (주기적)
        timeSinceLastPoolRefresh += deltaTime;
        if (timeSinceLastPoolRefresh >= poolRefreshInterval)
        {
            RefreshSpawnPool();
            timeSinceLastPoolRefresh = 0f;
        }

        // 4. 스폰 체크 (주기적)
        UpdateSpawnCheck(deltaTime);

        // 5. UI 업데이트
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
        // Phase별 예산 증가율 조정
        UpdateBudgetAccumulationRate();

        // 예산 누적
        currentBudget += budgetAccumulationRate * deltaTime;
    }

    private void UpdateBudgetAccumulationRate()
    {
        int phase = GetCurrentPhase();
        float newRate = 0f;

        switch (phase)
        {
            case 1: newRate = 10f; break; // Phase 1: 14:00~10:00
            case 2: newRate = 17f; break; // Phase 2: 10:00~6:00
            case 3: newRate = 25f; break; // Phase 3: 6:00~0:00
        }

        if (budgetAccumulationRate != newRate)
        {
            budgetAccumulationRate = newRate;
            if (showDebugLogs)
                Debug.Log($"[TimeBasedSpawn] Phase {phase} started - Budget rate: {budgetAccumulationRate}/s");
        }
    }

    private int GetCurrentPhase()
    {
        float timeRemaining = GetGameTimeRemaining();
        if (timeRemaining > 600f) return 1; // Phase 1: 840~600초
        if (timeRemaining > 360f) return 2; // Phase 2: 600~360초
        return 3; // Phase 3: 360~0초
    }

    #endregion

    #region Spawn Pool Management

    private void RefreshSpawnPool()
    {
        float timeRemaining = GetGameTimeRemaining();
        List<string> newPool = EnemyTimeRangeData.GetSpawnableEnemiesAtTime(timeRemaining);

        if (newPool.Count != currentSpawnPool.Count)
        {
            currentSpawnPool = newPool;
            if (showDebugLogs)
                Debug.Log($"[SpawnPool] Refreshed at {FormatTime(timeRemaining)} - {currentSpawnPool.Count} enemies available");
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
                Debug.LogWarning($"[Spawn] No enemies available in spawn pool at {FormatTime(GetGameTimeRemaining())}");
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

        // 랜덤 선택
        string selectedEnemy = affordableEnemies[Random.Range(0, affordableEnemies.Count)];
        int enemyCost = EnemyCostData.GetCost(selectedEnemy);

        // 스폰 실행
        SpawnEnemy(selectedEnemy);

        // 예산 소모
        currentBudget -= enemyCost;

        if (showDebugLogs)
            Debug.Log($"[Spawn] {selectedEnemy} spawned (Cost: {enemyCost}, Remaining Budget: {currentBudget:F0})");
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
            UiManager.Instance.SetBudgetDebugText(currentBudget, budgetAccumulationRate);
            UiManager.Instance.SetPhaseDebugText(GetCurrentPhase(), GetGameTimeRemaining());
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
    public (float timeRemaining, float budget, int phase, int poolSize) GetStatus()
    {
        return (GetGameTimeRemaining(), currentBudget, GetCurrentPhase(), currentSpawnPool.Count);
    }

    #endregion
}
