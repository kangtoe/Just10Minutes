using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpawnInfo
{
    [SerializeField] string desc = "Spawn Desc";

    [Space]
    public GameObject spawnPrefab;

    public float spawnTime;
    public Edge spawnSide;

    [Header("multi spawn")]
    public int count;
    public float spawnInterval;

    [HideInInspector] public float SpawnEndTime => spawnTime + count * spawnInterval;
}

public class EnemySpawner : MonoSingleton<EnemySpawner>
{
    [Header("MUST SET 0 ON SHPPING")]
    [SerializeField]
    float devStartTime;

    [Header("Wave System (New)")]
    [SerializeField] bool useWaveSystem = false;  // false로 시작 (안전)
    [SerializeField] int startWaveNumber = 1;
    [SerializeField] int maxWaveNumber = 30;
    [SerializeField] float waveTransitionDelay = 2f;

    [Header("Enemy Prefabs for Wave Generation")]
    [Tooltip("절차적 생성에 사용될 모든 적 프리팹 (14개)")]
    [SerializeField] EnemyShip[] enemyPrefabs;

    [Header("Boss Wave Settings")]
    [Tooltip("웨이브 10 보스")]
    [SerializeField] EnemyShip boss10Prefab;

    [Tooltip("웨이브 20 보스")]
    [SerializeField] EnemyShip boss20Prefab;

    [Tooltip("웨이브 30 보스 (최종)")]
    [SerializeField] EnemyShip boss30Prefab;

    [Header("Legacy System (Old)")]
    [Space]
    [SerializeField]
    GameObject defaultEnemyPrefab;

    [SerializeField]
    List<SpawnInfo> timeSpawnInfoList;

    [SerializeField]
    List<SpawnInfo> endlessSpawnInfoList;

    // 웨이브 시스템 변수
    private int currentWaveNumber = 0;
    private bool isWaveActive = false;
    private List<GameObject> currentWaveEnemies = new List<GameObject>();

    // Legacy 시스템 변수
    List<SpawnInfo> endlessSpawnInfoListOrigin;
    float ElapsedTime => TimeRecordManager.Instance.TimeRecord;
    float spawnEndTime;

    // 디버깅용 (Inspector에서 확인 가능)
    public int CurrentWaveNumber => currentWaveNumber;
    public bool IsWaveActive => isWaveActive;
    public int CurrentWaveEnemyCount => currentWaveEnemies.Count;

    private void Start()
    {
        if (useWaveSystem)
        {
            InitWaveSystem();
        }
        else
        {
            InitLegacySystem();
        }
    }

    private void Update()
    {
        if (useWaveSystem)
        {
            UpdateWaveSystem();
        }
        else
        {
            UpdateLegacySystem();
        }
    }

    // ==================== 웨이브 시스템 ====================

    private void InitWaveSystem()
    {
        Debug.Log("[EnemySpawner] Initializing Wave System");

        // 적 프리팹 레지스트리 초기화
        ProceduralWaveGenerator.Initialize(enemyPrefabs);

        // 첫 웨이브 시작
        currentWaveNumber = startWaveNumber;
        StartWave(currentWaveNumber);
    }

    private void UpdateWaveSystem()
    {
        // 웨이브 완료 체크
        if (isWaveActive && AreAllWaveEnemiesDead())
        {
            OnWaveComplete();
        }
    }

    private void StartWave(int waveNumber)
    {
        if (waveNumber > maxWaveNumber)
        {
            Debug.Log("[EnemySpawner] All waves completed!");
            return;
        }

        Debug.Log($"[Wave {waveNumber}] Starting...");

        isWaveActive = true;
        currentWaveEnemies.Clear();

        WaveType waveType = WaveBudgetCalculator.GetWaveType(waveNumber);
        SpawnInfo[] spawnInfos = null;

        if (waveType == WaveType.Manual)
        {
            // 보스 웨이브 (10, 20, 30): 상단 고정 스폰
            spawnInfos = GenerateBossWave(waveNumber);
            Debug.Log($"[Wave {waveNumber}] Boss wave - spawning at top");
        }
        else
        {
            // 절차적 생성
            spawnInfos = GenerateProceduralWave(waveNumber);
        }

        // SpawnInfo 실행
        if (spawnInfos != null && spawnInfos.Length > 0)
        {
            ExecuteSpawnInfos(spawnInfos);
        }
        else
        {
            Debug.LogError($"[Wave {waveNumber}] No spawn infos generated!");
            OnWaveComplete(); // 빈 웨이브면 즉시 완료
        }
    }

    private SpawnInfo[] GenerateProceduralWave(int waveNumber)
    {
        int budget = WaveBudgetCalculator.CalculateBudget(waveNumber);
        Debug.Log($"[Wave {waveNumber}] Generating procedural wave with budget: {budget}");

        GeneratedWaveData waveData = ProceduralWaveGenerator.GenerateWave(waveNumber, budget);
        return waveData.spawnInfos;
    }

    private SpawnInfo[] GenerateBossWave(int waveNumber)
    {
        // 웨이브 번호에 따라 보스 선택
        EnemyShip selectedBoss = null;

        switch (waveNumber)
        {
            case 10:
                selectedBoss = boss10Prefab;
                break;
            case 20:
                selectedBoss = boss20Prefab;
                break;
            case 30:
                selectedBoss = boss30Prefab;
                break;
            default:
                Debug.LogError($"[EnemySpawner] Invalid boss wave number: {waveNumber}");
                return new SpawnInfo[0];
        }

        if (selectedBoss == null)
        {
            Debug.LogError($"[EnemySpawner] Boss prefab for wave {waveNumber} is not assigned!");
            return new SpawnInfo[0];
        }

        SpawnInfo bossSpawn = new SpawnInfo
        {
            spawnPrefab = selectedBoss.gameObject,
            spawnTime = 0f,
            spawnSide = Edge.Up,
            count = 1,
            spawnInterval = 0f
        };

        return new SpawnInfo[] { bossSpawn };
    }

    private void ExecuteSpawnInfos(SpawnInfo[] spawnInfos)
    {
        foreach (SpawnInfo info in spawnInfos)
        {
            StartCoroutine(SpawnWithDelay(info));
        }
    }

    private IEnumerator SpawnWithDelay(SpawnInfo info)
    {
        yield return new WaitForSeconds(info.spawnTime);

        for (int i = 0; i < info.count; i++)
        {
            GameObject enemy = ObjectSpawner.Instance.SpawnObject(
                info.spawnPrefab,
                info.spawnSide
            );

            if (enemy != null)
            {
                currentWaveEnemies.Add(enemy);
            }

            if (i < info.count - 1)
            {
                yield return new WaitForSeconds(info.spawnInterval);
            }
        }
    }

    private bool AreAllWaveEnemiesDead()
    {
        // null이거나 destroyed된 적 제거
        currentWaveEnemies.RemoveAll(e => e == null);
        return currentWaveEnemies.Count == 0;
    }

    private void OnWaveComplete()
    {
        isWaveActive = false;
        Debug.Log($"[Wave {currentWaveNumber}] Complete!");

        // 다음 웨이브 시작 (delay 후)
        currentWaveNumber++;

        if (currentWaveNumber <= maxWaveNumber)
        {
            Debug.Log($"[Wave {currentWaveNumber}] Starting in {waveTransitionDelay} seconds...");
            StartCoroutine(StartWaveDelayed(currentWaveNumber, waveTransitionDelay));
        }
        else
        {
            Debug.Log("[EnemySpawner] All waves completed!");
        }
    }

    private IEnumerator StartWaveDelayed(int waveNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartWave(waveNumber);
    }

    // ==================== Legacy 시스템 ====================

    private void InitLegacySystem()
    {
        for (int i = timeSpawnInfoList.Count - 1; i >= 0; i--)
        {
            SpawnInfo spawnInfo = timeSpawnInfoList[i];
            if (devStartTime > spawnInfo.spawnTime)
            {
                timeSpawnInfoList.RemoveAt(i);
            }
        }

        endlessSpawnInfoListOrigin = new(endlessSpawnInfoList);
    }

    private void UpdateLegacySystem()
    {
        for (int i = timeSpawnInfoList.Count - 1; i >= 0; i--)
        {
            SpawnInfo spawnInfo = timeSpawnInfoList[i];
            if (ElapsedTime + devStartTime > spawnInfo.spawnTime)
            {
                ObjectSpawner.Instance.SpawnObjects(spawnInfo.spawnPrefab, spawnInfo.spawnSide, spawnInfo.count, spawnInfo.spawnInterval);
                timeSpawnInfoList.RemoveAt(i);

                if (timeSpawnInfoList.Count == 0)
                {
                    spawnEndTime = ElapsedTime;
                }
            }
        }

        if (timeSpawnInfoList.Count > 0) return;
        for (int i = endlessSpawnInfoList.Count - 1; i >= 0; i--)
        {
            SpawnInfo spawnInfo = endlessSpawnInfoList[i];
            if (ElapsedTime - spawnEndTime > spawnInfo.spawnTime)
            {
                ObjectSpawner.Instance.SpawnObjects(spawnInfo.spawnPrefab, spawnInfo.spawnSide, spawnInfo.count, spawnInfo.spawnInterval);
                endlessSpawnInfoList.RemoveAt(i);

                if (endlessSpawnInfoList.Count == 0)
                {
                    spawnEndTime = ElapsedTime;
                    endlessSpawnInfoList = new(endlessSpawnInfoListOrigin);
                }
            }
        }
    }

    // ==================== Editor 지원 메서드 ====================

    public void AddSpawnInfo()
    {
        float SpawnEndTime = GetSpawnEndTime();

        SpawnInfo info = new();
        info.spawnPrefab = defaultEnemyPrefab;
        info.spawnTime = GetSpawnEndTime();
        info.count = 1;
        info.spawnSide = Edge.Random;
        timeSpawnInfoList.Add(info);
    }

    public float GetSpawnEndTime()
    {
        if (timeSpawnInfoList.Count == 0) return 0;
        return timeSpawnInfoList[timeSpawnInfoList.Count - 1].SpawnEndTime;
    }
}
