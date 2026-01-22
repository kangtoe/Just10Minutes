using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특수 스폰 이벤트 데이터
/// Vampire Survivors 스타일의 특정 방향 집중 스폰 이벤트
/// </summary>
[Serializable]
public class SpawnEventData
{
    [Header("Event Trigger")]
    [Tooltip("이벤트 발동 시간 (초, elapsed time 기준)")]
    [SerializeField] private float triggerTime = 180f; // 기본값: 3분

    [Header("Spawn Settings")]
    [Tooltip("스폰 방향 (Random 선택 시 랜덤 방향에서 스폰)")]
    [SerializeField] private Edge spawnEdge = Edge.Up;

    [Tooltip("스폰할 적 구성")]
    [SerializeField] private List<EnemyComposition> enemyComposition = new();

    [Tooltip("스폰 간격 (초)")]
    [SerializeField, Range(0f, 5f)] private float spawnInterval = 0.2f;

    [Header("Event Behavior")]
    [Tooltip("이벤트 중 일반 스폰 일시 중지 여부")]
    [SerializeField] private bool pauseNormalSpawn = true;

    // 이벤트 실행 상태 (런타임)
    [NonSerialized] public bool hasTriggered = false;

    // 프로퍼티
    public float TriggerTime => triggerTime;
    public Edge SpawnEdge => spawnEdge;
    public List<EnemyComposition> EnemyComposition => enemyComposition;
    public float SpawnInterval => spawnInterval;
    public bool PauseNormalSpawn => pauseNormalSpawn;
}

/// <summary>
/// 적 구성 (적 종류 + 수량)
/// </summary>
[Serializable]
public class EnemyComposition
{
    [Tooltip("스폰할 적 프리팹")]
    [SerializeField] private EnemyShip enemyPrefab;

    [Tooltip("스폰할 수량")]
    [SerializeField, Min(1)] private int spawnCount = 1;

    // 프로퍼티
    public EnemyShip EnemyPrefab => enemyPrefab;
    public int SpawnCount => spawnCount;
}
