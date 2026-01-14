using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
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
    [Header("Wave System")]
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

    [Header("Wave Status (Read Only)")]
    [SerializeField,ReadOnly] private int currentWaveNumber = 0;
    [SerializeField,ReadOnly] private bool isWaveActive = false;
    [SerializeField,ReadOnly] private int currentWaveEnemyCount = 0;

    // 내부 데이터
    private List<GameObject> currentWaveEnemies = new List<GameObject>();
    private bool isAllSpawnsComplete = false;  // 모든 스폰이 완료되었는지
    private int activeSpawnCoroutines = 0;     // 실행 중인 스폰 코루틴 수

    private void Start()
    {
        InitWaveSystem();
    }

    private void Update()
    {
        UpdateWaveSystem();

        // Inspector 디버그 정보 업데이트
        currentWaveEnemyCount = currentWaveEnemies.Count;

        // UI 디버그 텍스트 업데이트
        if (UiManager.Instance != null)
        {
            bool isSpawning = isWaveActive && !isAllSpawnsComplete;
            UiManager.Instance.SetWaveDebugText(currentWaveNumber, isSpawning, currentWaveEnemyCount);
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
        // 웨이브 완료 체크: 모든 스폰이 완료되고 + 모든 적이 죽었을 때만
        if (isWaveActive && isAllSpawnsComplete && AreAllWaveEnemiesDead())
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
        isAllSpawnsComplete = false;
        activeSpawnCoroutines = 0;
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
        activeSpawnCoroutines = spawnInfos.Length;
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

        // 이 스폰 그룹 완료
        activeSpawnCoroutines--;
        if (activeSpawnCoroutines <= 0)
        {
            isAllSpawnsComplete = true;
            Debug.Log($"[Wave {currentWaveNumber}] All spawns complete - {currentWaveEnemies.Count} enemies spawned");
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

}
