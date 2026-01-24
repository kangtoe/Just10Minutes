using System;
using UnityEngine;
using UnityEngine.Events;

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

    [Tooltip("스폰할 적 프리팹")]
    [SerializeField] private EnemyShip enemyPrefab;

    [Tooltip("스폰할 수량")]
    [SerializeField, Min(1)] private int spawnCount = 5;

    [Tooltip("스폰 간격 (초)")]
    [SerializeField, Range(0f, 5f)] private float spawnInterval = 0.2f;

    [Header("Event Behavior")]
    [Tooltip("이벤트 중 일반 스폰 일시 중지 여부")]
    [SerializeField] private bool pauseNormalSpawn = true;

    [Header("Event Callbacks")]
    [Tooltip("이벤트 시작 시 호출될 콜백")]
    public UnityEvent onEventStart;

    // 이벤트 실행 상태 (런타임)
    [NonSerialized] public bool hasTriggered = false;

    // 기본 생성자 (Unity 직렬화용)
    public SpawnEventData()
    {
        onEventStart = new UnityEvent();
    }

    // 런타임 생성자 (코드로 이벤트 생성할 때 사용)
    public SpawnEventData(float triggerTime, Edge spawnEdge, EnemyShip enemyPrefab, int spawnCount, float spawnInterval = 0.2f, bool pauseNormalSpawn = true)
    {
        this.triggerTime = triggerTime;
        this.spawnEdge = spawnEdge;
        this.enemyPrefab = enemyPrefab;
        this.spawnCount = spawnCount;
        this.spawnInterval = spawnInterval;
        this.pauseNormalSpawn = pauseNormalSpawn;
        this.onEventStart = new UnityEvent();
    }

    // 프로퍼티
    public float TriggerTime => triggerTime;
    public Edge SpawnEdge => spawnEdge;
    public EnemyShip EnemyPrefab => enemyPrefab;
    public int SpawnCount => spawnCount;
    public float SpawnInterval => spawnInterval;
    public bool PauseNormalSpawn => pauseNormalSpawn;
}
