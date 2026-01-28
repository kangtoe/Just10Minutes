using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 시간 기반 스폰 시스템의 핵심 관리자
/// - 10분 elapsed 타이머 사용 (TimeRecordManager)
/// - 예산 누적 시스템
/// - 주기적 스폰 체크 실행
/// - 시간 범위 기반 스폰 풀 관리
/// </summary>
public class TimeBasedSpawnManager : MonoSingleton<TimeBasedSpawnManager>
{
    [Header("=== Enemy Time Ranges ===")]
    [Tooltip("적 스폰 시간 설정 ScriptableObject")]
    [SerializeField] private EnemySpawnTimesSO enemySpawnTimesSO;
    [Tooltip("로드된 스폰 시간 데이터 (자동 설정됨)")]
    [SerializeField, ReadOnly] private EnemyTimeRange[] enemyTimeRanges;

    [Header("=== Spawn Events ===")]
    [SerializeField] private SpawnEventData[] spawnEvents;
    private bool isEventActive = false; // 이벤트 진행 중 플래그

    [Header("=== Boss Settings ===")]
    [SerializeField] private EnemyShip bossPrefab; // 보스 프리팹
    [SerializeField] private float bossSpawnTime = 600f; // 10분
    [SerializeField] private Edge bossSpawnEdge = Edge.Up; // 보스 스폰 방향
    [SerializeField, Min(1)] private int bossSpawnCount = 1; // 보스 스폰 수량
    private SpawnEventData bossEvent; // 동적으로 생성된 보스 이벤트 참조

    [Header("=== Edge Selection ===")]
    [SerializeField] private WeightedEdgeSelector edgeSelector = new WeightedEdgeSelector();

    [Header("=== Spawn Warning Settings ===")]
    [SerializeField] private bool enableSpawnWarning = true; // 스폰 경고 활성화 (이벤트/보스 스폰만)
    [SerializeField] private float warningDelay = 1f; // 경고 후 실제 스폰까지의 지연 시간

    [Header("=== Spawn Check Settings ===")]
    [SerializeField] private float spawnCheckInterval = 1f;
    private float timeSinceLastCheck = 0f;    

    [Header("Spawn Pool Settings")]
    [SerializeField] private float poolRefreshInterval = 1f;
    private float timeSinceLastPoolRefresh = 0f;
    [SerializeField, ReadOnly] private List<EnemyShip> currentSpawnPool = new List<EnemyShip>();   

    [Header("Skip System")]
    [SerializeField, Range(0f, 0.99f)] private float skipProbability = 0.3f; // 건너뛰기 확률
    [SerializeField] private float maxSpawnSkipTime = 5f; // 강제 스폰까지의 최대 대기 시간 (초)
    private float timeSinceLastSpawn = 0f;

    private HashSet<EnemyShip> trackedEnemies = new HashSet<EnemyShip>(); // 추적 중인 적들

    private bool isRunning = false;

    [Header("=== Budget Settings ===")]
    [SerializeField] private float initialBudget = 50f;
    [SerializeField, ReadOnly] private float currentBudget;

    [Header("Budget Accumulation Rate (Linear Ramp)")]
    [SerializeField] private float minBudgetRate = 10f;  // 초기 예산 증가율 (게임 시작)
    [SerializeField] private float maxBudgetRate = 25f;  // 최종 예산 증가율 (최고 난이도)
    [SerializeField] private float budgetRateMaxUpTime = 600f; // 최종값 도달 시간 (초)
    [SerializeField, ReadOnly] private float currentBudgetRate = 10f; // 현재 증가율 (실시간 계산)     

    [Header("Target Presence Score (Linear Ramp)")]
    [SerializeField] private float minTargetPresenceScore = 50f;  // 초기 목표 존재 점수 (게임 시작)
    [SerializeField] private float maxTargetPresenceScore = 800f; // 최종 목표 존재 점수 (최고 난이도)
    [SerializeField] private float targetPresenceScoreRampUpTime = 600f; // 최종값 도달 시간 (초)    
    [SerializeField, ReadOnly] private float currentPresenceScore = 0f; // 현재 존재 점수
    [SerializeField, ReadOnly] private float currentTargetPresenceScore = 50f; // 현재 목표 존재 점수 (실시간 계산)    
    
    [Header("Budget Rate Multiplier Range (Dynamic Difficulty Adjustment)")]
    [SerializeField, Range(0.1f, 1.0f)] private float minBudgetRateMultiplier = 0.7f; // 최소 배율
    [SerializeField, Range(1.0f, 10.0f)] private float maxBudgetRateMultiplier = 1.3f; // 최대 배율
    [SerializeField, ReadOnly]private float budgetRateMultiplier = 1f; // 현재 예산 증가율 배율

    private float baseBudgetRate = 10f; // 시간 기반 기본 예산 증가율 (조정 전)

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

        // 동적 난이도 조정 초기값 설정
        budgetRateMultiplier = 1f;

        // Edge 선택기 초기화
        edgeSelector.Reset();

        // 적 시간 범위 데이터 초기화 (ScriptableObject에서 로드)
        if (enemySpawnTimesSO == null)
        {
            Debug.LogError("[TimeBasedSpawn] EnemySpawnTimesSO가 할당되지 않았습니다! Inspector에서 설정해주세요.");
            return;
        }

        enemyTimeRanges = enemySpawnTimesSO.ToTimeRanges();

        if (enemyTimeRanges == null || enemyTimeRanges.Length == 0)
        {
            Debug.LogError("[TimeBasedSpawn] CSV 로드 실패 또는 데이터 없음!");
            return;
        }

        EnemyTimeRangeData.Initialize(enemyTimeRanges);
        if (showDebugLogs)
            Debug.Log($"[TimeBasedSpawn] Loaded & Initialized {enemyTimeRanges.Length} enemy time ranges from CSV");

        // 보스 이벤트 생성 및 추가
        if (bossPrefab != null)
        {
            // 생성자를 사용하여 보스 이벤트 생성
            bossEvent = new SpawnEventData(
                triggerTime: bossSpawnTime,
                spawnEdge: bossSpawnEdge,
                enemyPrefab: bossPrefab,
                spawnCount: bossSpawnCount,
                spawnInterval: 0.2f,
                pauseNormalSpawn: true
            );

            // 보스 이벤트 시작 시 콜백 등록
            bossEvent.onEventStart.AddListener(OnBossEventStart);

            // 기존 스폰 이벤트 배열에 보스 이벤트 추가
            List<SpawnEventData> eventList = new List<SpawnEventData>();
            if (spawnEvents != null)
            {
                eventList.AddRange(spawnEvents);
            }
            eventList.Add(bossEvent);
            spawnEvents = eventList.ToArray();

            if (showDebugLogs)
                Debug.Log($"[Boss] Boss event created at {bossSpawnTime}s on {bossSpawnEdge}");
        }
        else
        {
            Debug.LogWarning("[Boss] Boss prefab not assigned!");
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
            Debug.Log($"[TimeBasedSpawn] System started - Initial Budget: {initialBudget}, Budget Rate: {minBudgetRate}→{maxBudgetRate}/s over {budgetRateMaxUpTime}s");
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

        // 1. 모든 스폰 이벤트 완료 확인 (일반 스폰 중단)
        if (AreAllEventsCompleted())
        {
            isRunning = false;
            if (showDebugLogs)
                Debug.Log("[TimeBasedSpawn] All spawn events completed, normal spawning stopped");
            return;
        }

        // 2. 예산 누적
        UpdateBudget(deltaTime);

        // 3. 목표 존재 점수 업데이트
        currentTargetPresenceScore = GetTargetPresenceScore();

        // 4. 강제 스폰 타이머 업데이트
        timeSinceLastSpawn += deltaTime;

        // 5. 스폰 풀 갱신 (주기적)
        timeSinceLastPoolRefresh += deltaTime;
        if (timeSinceLastPoolRefresh >= poolRefreshInterval)
        {
            RefreshSpawnPool();
            timeSinceLastPoolRefresh = 0f;
        }

        // 6. 동적 난이도 조정 (매 프레임)
        PerformDifficultyAdjustment();

        // 7. 이벤트 트리거 체크 (보스 포함)
        CheckSpawnEvents();

        // 8. 스폰 체크 (주기적, 이벤트 중이 아닐 때만)
        if (!isEventActive)
        {
            UpdateSpawnCheck(deltaTime);
        }

        // 9. UI 업데이트
        UpdateUI();
    }

    #region Event Management

    /// <summary>
    /// 모든 스폰 이벤트가 완료되었는지 확인
    /// </summary>
    private bool AreAllEventsCompleted()
    {
        if (spawnEvents == null || spawnEvents.Length == 0)
            return false;

        foreach (SpawnEventData eventData in spawnEvents)
        {
            if (!eventData.hasTriggered)
                return false;
        }

        return true;
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
        float t = Mathf.Clamp01(elapsed / budgetRateMaxUpTime); // 0~1 진행도

        // 시간 기반 기본 예산 증가율 계산
        baseBudgetRate = Mathf.Lerp(minBudgetRate, maxBudgetRate, t);

        // 동적 난이도 조정 배율 적용
        currentBudgetRate = baseBudgetRate * budgetRateMultiplier;
    }

    private int GetCurrentPhase()
    {
        float elapsed = GetElapsedTime();
        if (elapsed < 200f) return 1; // Phase 1: 0~200초 (0:00~3:20)
        if (elapsed < 400f) return 2; // Phase 2: 200~400초 (3:20~6:40)
        return 3; // Phase 3: 400~600초 (6:40~10:00)
    }

    #endregion

    #region Spawn Pool Management

    private void RefreshSpawnPool()
    {
        float elapsed = GetElapsedTime();
        List<EnemyShip> newPool = EnemyTimeRangeData.GetSpawnableEnemiesAtTime(elapsed);

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
        List<EnemyShip> affordableEnemies = new List<EnemyShip>();
        foreach (EnemyShip enemyPrefab in currentSpawnPool)
        {
            if (enemyPrefab.point <= currentBudget)
            {
                affordableEnemies.Add(enemyPrefab);
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
        EnemyShip selectedEnemy = affordableEnemies[Random.Range(0, affordableEnemies.Count)];
        int enemyCost = selectedEnemy.point;

        // 스폰 실행
        SpawnEnemy(selectedEnemy);

        // 예산 소모
        currentBudget -= enemyCost;

        // 마지막 스폰 시간 초기화
        timeSinceLastSpawn = 0f;

        if (showDebugLogs)
        {
            string forceFlag = forceSpawn ? " [FORCED]" : "";
            Edge lastEdge = edgeSelector.GetDebugInfo().lastEdge;
            Debug.Log($"[Spawn] {selectedEnemy.name} spawned{forceFlag} at {lastEdge} (Cost: {enemyCost}, Remaining Budget: {currentBudget:F0})");
        }
    }

    /// <summary>
    /// 적 스폰 (일반 스폰 및 이벤트 스폰)
    /// </summary>
    /// <param name="prefab">스폰할 적 프리팹</param>
    /// <param name="spawnEdge">스폰 방향 (Undefined면 가중치 기반 자동 선택)</param>
    /// <param name="lengthRatio">Edge 내 위치 비율 (0~1), null이면 랜덤</param>
    /// <param name="showWarning">경고 표시 여부 (true: 이벤트/보스 스폰, false: 일반 스폰)</param>
    /// <param name="onSpawned">스폰 완료 시 호출될 콜백 (즉시 또는 지연 후 호출)</param>
    private void SpawnEnemy(EnemyShip prefab, Edge spawnEdge = Edge.Undefined, float? lengthRatio = null, bool showWarning = false, System.Action<EnemyShip> onSpawned = null)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawn] Enemy prefab is null");
            onSpawned?.Invoke(null);
            return;
        }

        bool lookCenter = false;

        // 가중치 기반 Edge 선택
        // Undefined Edge일 경우 무작위 면을 지정하고, 중앙을 바라보도록 설정
        if (spawnEdge == Edge.Undefined)
        {
            spawnEdge = edgeSelector.GetNextEdge();
            lookCenter = true;
        }

        // 경고 시스템 활성화 시, 경고 표시 후 지연 스폰 (이벤트/보스 스폰만)
        if (showWarning && enableSpawnWarning && EnemySpawnWarningManager.Instance != null)
        {
            // lengthRatio가 null이면 미리 결정 (경고 위치와 실제 스폰 위치 일치)
            if (lengthRatio == null)
            {
                lengthRatio = Random.Range(0f, 1f);
            }

            // 경고 표시 (경고 시스템에서 위치 계산 및 조정)
            EnemySpawnWarningManager.Instance.ShowWarning(
                prefab.gameObject,
                spawnEdge,
                lengthRatio.Value,
                warningDelay
            );

            // 지연 스폰 실행 (콜백 전달)
            StartCoroutine(SpawnEnemyDelayed(prefab, spawnEdge, lengthRatio, lookCenter, warningDelay, onSpawned));
        }
        else
        {
            // 경고 없이 즉시 스폰 (일반 스폰)
            EnemyShip enemy = SpawnEnemyImmediate(prefab, spawnEdge, lookCenter, lengthRatio);
            onSpawned?.Invoke(enemy);
        }
    }

    /// <summary>
    /// 적 즉시 스폰 (내부용)
    /// </summary>
    /// <returns>스폰된 EnemyShip 컴포넌트</returns>
    private EnemyShip SpawnEnemyImmediate(EnemyShip prefab, Edge spawnEdge, bool lookCenter, float? lengthRatio)
    {
        // ObjectSpawner를 통해 스폰
        GameObject enemy = ObjectSpawner.Instance.SpawnObject(
            prefab.gameObject,
            spawnEdge,
            lookCenter,
            lengthRatio
        );

        // 존재 점수 추적
        if (enemy != null)
        {
            EnemyShip enemyShip = enemy.GetComponent<EnemyShip>();
            if (enemyShip != null)
            {
                RegisterEnemy(enemyShip);
                return enemyShip;
            }
        }

        return null;
    }

    /// <summary>
    /// 지연 스폰 코루틴
    /// </summary>
    private IEnumerator SpawnEnemyDelayed(EnemyShip prefab, Edge spawnEdge, float? lengthRatio, bool lookCenter, float delay, System.Action<EnemyShip> onSpawned)
    {
        yield return new WaitForSeconds(delay);

        // 게임 클리어 상태 확인 (보스 격파 시 지연 스폰 취소)
        if (GameManager.Instance != null && GameManager.Instance.GameState == GameState.GameClear)
        {
            if (showDebugLogs)
                Debug.Log($"[Spawn] Delayed spawn cancelled - Boss defeated");
            yield break;
        }

        EnemyShip spawnedEnemy = SpawnEnemyImmediate(prefab, spawnEdge, lookCenter, lengthRatio);
        onSpawned?.Invoke(spawnedEnemy);
    }

    #endregion

    #region Presence Score Tracking

    /// <summary>
    /// 적 등록 및 존재 점수 증가
    /// </summary>
    private void RegisterEnemy(EnemyShip enemy)
    {
        if (enemy == null || trackedEnemies.Contains(enemy))
            return;

        trackedEnemies.Add(enemy);
        currentPresenceScore += enemy.point;

        // 적 파괴 시 존재 점수 감소를 위한 리스너 등록
        Damageable damageable = enemy.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.onDead.AddListener(() => UnregisterEnemy(enemy));
        }

        if (showDebugLogs)
            Debug.Log($"[PresenceScore] Enemy registered: {enemy.name} (+{enemy.point}) → Total: {currentPresenceScore:F0}");
    }

    /// <summary>
    /// 적 제거 및 존재 점수 감소
    /// </summary>
    private void UnregisterEnemy(EnemyShip enemy)
    {
        if (enemy == null || !trackedEnemies.Contains(enemy))
            return;

        trackedEnemies.Remove(enemy);
        currentPresenceScore -= enemy.point;

        // 음수 방지
        if (currentPresenceScore < 0)
            currentPresenceScore = 0;

        if (showDebugLogs)
            Debug.Log($"[PresenceScore] Enemy destroyed: {enemy.name} (-{enemy.point}) → Total: {currentPresenceScore:F0}");
    }

    /// <summary>
    /// 현재 시간의 목표 존재 점수 계산
    /// </summary>
    private float GetTargetPresenceScore()
    {
        float elapsed = GetElapsedTime();
        float t = Mathf.Clamp01(elapsed / targetPresenceScoreRampUpTime); // 0~1 진행도

        return Mathf.Lerp(minTargetPresenceScore, maxTargetPresenceScore, t);
    }

    /// <summary>
    /// 목표 대비 존재 점수 차이율 계산 (±%)
    /// </summary>
    private float GetPresenceScoreDifference()
    {
        float target = GetTargetPresenceScore();
        if (target <= 0) return 0f;

        return ((currentPresenceScore - target) / target) * 100f;
    }

    #endregion

    #region Dynamic Difficulty Adjustment

    /// <summary>
    /// 연속적인 난이도 조정 계산 (매 프레임 실행)
    /// 공식: 목표 존재 점수 / 현재 존재 점수
    /// </summary>
    private void PerformDifficultyAdjustment()
    {
        float target = GetTargetPresenceScore();
        if (target <= 0)
        {
            budgetRateMultiplier = 1f;
            return;
        }

        // 연속적인 배율 계산 (비율 기반)
        float calculatedMultiplier = target / Mathf.Max(currentPresenceScore, 1f);

        // 범위 제한 (min ~ max)
        budgetRateMultiplier = Mathf.Clamp(calculatedMultiplier, minBudgetRateMultiplier, maxBudgetRateMultiplier);

        if (showDebugLogs)
        {
            float difference = GetPresenceScoreDifference(); // 디버그용
            Debug.Log($"[DynamicDifficulty] Current: {currentPresenceScore:F0}, Target: {target:F0}, Diff: {difference:F1}%, Multiplier: {budgetRateMultiplier:F2}x");
        }
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

    #region Enemy Management

    /// <summary>
    /// 모든 적을 보상 없이 제거
    /// </summary>
    public void ClearAllEnemies()
    {
        EnemyShip[] enemies = FindObjectsByType<EnemyShip>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.Die(giveReward: false);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[SpawnManager] Cleared {enemies.Length} enemies");
    }

    #endregion

    #region Boss Event Callbacks

    /// <summary>
    /// 보스 이벤트 시작 시 호출되는 콜백
    /// </summary>
    private void OnBossEventStart()
    {
        if (showDebugLogs)
            Debug.Log("[Boss] Boss event started - Clearing existing enemies");

        // 기존 모든 적 제거
        ClearAllEnemies();

        // 플레이어 위치에 플로팅 텍스트 표시
        if (UiManager.Instance != null && GameManager.Instance != null && GameManager.Instance.PlayerShip != null)
        {
            Vector3 playerPos = GameManager.Instance.PlayerShip.transform.position;
            UiManager.Instance.CreateText("! BOSS INCOMING !", playerPos);
        }

        // 화면 떨림 효과
        // if (UiManager.Instance != null)
        // {
        //     UiManager.Instance.ShakeUI(20f, 0.5f);
        // }
    }

    #endregion

    #region Spawn Events

    /// <summary>
    /// 스폰 이벤트 트리거 체크
    /// </summary>
    private void CheckSpawnEvents()
    {
        if (spawnEvents == null || spawnEvents.Length == 0)
            return;

        float elapsed = GetElapsedTime();

        foreach (SpawnEventData eventData in spawnEvents)
        {
            // 이미 트리거된 이벤트는 건너뛰기
            if (eventData.hasTriggered)
                continue;

            // 이벤트 발동 시간 도달 확인
            if (elapsed >= eventData.TriggerTime)
            {
                StartCoroutine(StartSpawnEvent(eventData));
                eventData.hasTriggered = true;
            }
        }
    }
    /// <summary>
    /// 스폰 이벤트 실행 코루틴
    /// </summary>
    private IEnumerator StartSpawnEvent(SpawnEventData eventData)
    {
        if (showDebugLogs)
            Debug.Log($"[SpawnEvent] Event started at {FormatTime(GetElapsedTime())} - Edge: {eventData.SpawnEdge}, Count: {eventData.SpawnCount}");

        // 이벤트 시작 콜백 호출
        eventData.onEventStart?.Invoke();

        // 일반 스폰 일시 중지 (옵션)
        if (eventData.PauseNormalSpawn)
        {
            isEventActive = true;
        }

        // 적 프리팹 확인
        if (eventData.EnemyPrefab == null)
        {
            Debug.LogWarning("[SpawnEvent] Enemy prefab is null, aborting event");
            if (eventData.PauseNormalSpawn)
                isEventActive = false;
            yield break;
        }

        int spawnCount = eventData.SpawnCount;

        // 보스 이벤트 여부 확인 (참조 비교)
        bool isBossEvent = (eventData == bossEvent);

        // 지정된 수량만큼 스폰
        for (int i = 0; i < spawnCount; i++)
        {
            // 게임 클리어 상태 확인 (보스 격파 시 스폰 중단)
            if (GameManager.Instance != null && GameManager.Instance.GameState == GameState.GameClear)
            {
                if (showDebugLogs)
                    Debug.Log($"[SpawnEvent] Event cancelled at index {i}/{spawnCount} - Boss defeated");
                break;
            }

            // Edge 결정 (Random일 경우 매번 랜덤 선택)
            Edge spawnEdge = GetRandomEdge(eventData.SpawnEdge);

            // 균등 배치 계산 (0~1 범위에서 균등하게 분산)
            float lengthRatio = (i + 0.5f) / spawnCount;

            // 보스 이벤트이고 첫 번째 스폰인 경우 게임 클리어 콜백 등록
            if (isBossEvent && i == 0)
            {
                SpawnEnemy(eventData.EnemyPrefab, spawnEdge, lengthRatio, showWarning: true, onSpawned: (enemy) =>
                {
                    if (enemy != null)
                    {
                        Damageable damageable = enemy.GetComponent<Damageable>();
                        if (damageable != null)
                        {
                            damageable.onDead.AddListener(() =>
                            {
                                Debug.Log("[Boss] Boss defeated! Game Clear!");
                                if (GameManager.Instance != null)
                                {
                                    GameManager.Instance.OnBossDefeated();
                                }
                            });

                            if (showDebugLogs)
                                Debug.Log($"[Boss] Boss spawned at {FormatTime(GetElapsedTime())} - OnDead callback registered");
                        }
                    }
                });
            }
            else
            {
                // 스폰 실행 (이벤트 스폰이므로 경고 표시)
                SpawnEnemy(eventData.EnemyPrefab, spawnEdge, lengthRatio, showWarning: true);
            }

            // 스폰 간격 대기
            yield return new WaitForSeconds(eventData.SpawnInterval);
        }

        // 이벤트 종료
        if (eventData.PauseNormalSpawn)
        {
            isEventActive = false;
        }

        if (showDebugLogs)
            Debug.Log($"[SpawnEvent] Event ended at {FormatTime(GetElapsedTime())}");
    }

    /// <summary>
    /// Edge Random 처리 (Random일 경우 랜덤 방향 반환)
    /// </summary>
    private Edge GetRandomEdge(Edge edge)
    {
        if (edge == Edge.Random)
        {
            Edge[] edges = { Edge.Up, Edge.Down, Edge.Left, Edge.Right };
            return edges[Random.Range(0, edges.Length)];
        }
        return edge;
    }

    #endregion
}
