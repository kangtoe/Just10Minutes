using System.Collections.Generic;
using UnityEngine;

// 플레이어의 모든 스탯을 중앙 집중식으로 관리하는 싱글톤
public class PlayerStatsManager : MonoSingleton<PlayerStatsManager>
{
    [Header("=== Player Stats Data ===")]
    [Tooltip("StatConfigDatabase SO (필수) - 모든 스탯 설정")]
    [SerializeField] private StatConfigDatabase statDatabase;

    // 내부 Dictionary (모든 스탯 값 저장)
    private Dictionary<UpgradeField, float> stats = new Dictionary<UpgradeField, float>();

    // === 프로퍼티 래퍼 (기존 코드 호환성 유지) ===

    [Header("생존 스탯")]
    public float maxDurability
    {
        get => GetStat(UpgradeField.MaxDurability);
        set => SetStat(UpgradeField.MaxDurability, value);
    }

    public float durabilityRegenRate
    {
        get => GetStat(UpgradeField.DurabilityRegenRate);
        set => SetStat(UpgradeField.DurabilityRegenRate, value);
    }

    public float durabilityRegenDelay
    {
        get => GetStat(UpgradeField.DurabilityRegenDelay);
        set => SetStat(UpgradeField.DurabilityRegenDelay, value);
    }

    public float maxShield
    {
        get => GetStat(UpgradeField.MaxShield);
        set => SetStat(UpgradeField.MaxShield, value);
    }

    public float shieldRegenRate
    {
        get => GetStat(UpgradeField.ShieldRegenRate);
        set => SetStat(UpgradeField.ShieldRegenRate, value);
    }

    public float shieldRegenDelay
    {
        get => GetStat(UpgradeField.ShieldRegenDelay);
        set => SetStat(UpgradeField.ShieldRegenDelay, value);
    }

    [Header("사격 스탯")]
    public int multiShot
    {
        get => (int)GetStat(UpgradeField.MultiShot);
        set => SetStat(UpgradeField.MultiShot, value);
    }

    public float fireRate
    {
        get => GetStat(UpgradeField.FireRate);
        set => SetStat(UpgradeField.FireRate, value);
    }

    public int projectileDamage
    {
        get => (int)GetStat(UpgradeField.ProjectileDamage);
        set => SetStat(UpgradeField.ProjectileDamage, value);
    }

    public float projectileSpeed
    {
        get => GetStat(UpgradeField.ProjectileSpeed);
        set => SetStat(UpgradeField.ProjectileSpeed, value);
    }

    public float projectileSize
    {
        get => GetStat(UpgradeField.ProjectileSize);
        set => SetStat(UpgradeField.ProjectileSize, value);
    }

    public float spread
    {
        get => GetStat(UpgradeField.Spread);
        set => SetStat(UpgradeField.Spread, value);
    }

    public float homingPower
    {
        get => GetStat(UpgradeField.HomingPower);
        set => SetStat(UpgradeField.HomingPower, value);
    }

    public float explosionDamageRatio
    {
        get => GetStat(UpgradeField.ExplosionDamageRatio);
        set => SetStat(UpgradeField.ExplosionDamageRatio, value);
    }

    [Header("충돌 스탯")]
    public float onImpact
    {
        get => GetStat(UpgradeField.OnImpact);
        set => SetStat(UpgradeField.OnImpact, value);
    }

    public float impactResist
    {
        get => GetStat(UpgradeField.ImpactResist);
        set => SetStat(UpgradeField.ImpactResist, value);
    }

    [Header("이동 스탯")]
    public float moveSpeed
    {
        get => GetStat(UpgradeField.MoveSpeed);
        set => SetStat(UpgradeField.MoveSpeed, value);
    }

    public float rotateSpeed
    {
        get => GetStat(UpgradeField.RotateSpeed);
        set => SetStat(UpgradeField.RotateSpeed, value);
    }

    public float mass
    {
        get => GetStat(UpgradeField.Mass);
        set => SetStat(UpgradeField.Mass, value);
    }

    // === Dictionary 접근 메서드 ===

    /// <summary>
    /// 특정 스탯 값 조회
    /// </summary>
    public float GetStat(UpgradeField field)
    {
        if (stats.ContainsKey(field))
            return stats[field];

        // 없으면 기본값 반환
        var config = statDatabase?.GetConfig(field);
        float defaultValue = config?.defaultValue ?? 0f;
        stats[field] = defaultValue;
        return defaultValue;
    }

    /// <summary>
    /// 특정 스탯 값 설정
    /// </summary>
    public void SetStat(UpgradeField field, float value)
    {
        stats[field] = value;
    }

    // === 초기화 ===

    /// <summary>
    /// 매니저 초기화 (첫 Instance 접근 시 자동 호출)
    /// StatConfigDatabase를 로드하고 StatMetadataRegistry 초기화
    /// </summary>
    /// <returns>true: 성공적으로 초기화됨, false: 이미 초기화되어 있음</returns>
    public override bool Initialize()
    {
        if (!base.Initialize()) return false;

        // StatConfigDatabase 검증
        if (statDatabase == null)
        {
            Debug.LogError("[PlayerStatsManager] StatConfigDatabase가 할당되지 않았습니다!");
            return false;
        }

        stats.Clear();

        // 1. StatConfigDatabase 초기화
        statDatabase.Initialize();

        // 2. StatMetadataRegistry 초기화 (기존 코드 호환성)
        var metadata = new Dictionary<UpgradeField, StatMetadata>();
        foreach (var config in statDatabase.allStats)
        {
            metadata[config.field] = config.ToMetadata();
        }
        StatMetadataRegistry.InitializeFromPlayerStats(metadata);

        // 3. 모든 UpgradeField를 기본값으로 초기화
        foreach (var config in statDatabase.allStats)
        {
            stats[config.field] = config.defaultValue;
        }

        Debug.Log($"[PlayerStatsManager] Initialized {stats.Count} stats from StatConfigDatabase");
        return true;
    }

    // === 업그레이드 적용 (switch 문 제거!) ===

    /// <summary>
    /// 업그레이드 적용 (증분 방식)
    /// </summary>
    public void ApplyUpgrade(UpgradeField field, float increment)
    {
        float currentValue = GetStat(field);
        SetStat(field, currentValue + increment);

        Debug.Log($"[PlayerStatsManager] {field}: {currentValue} -> {GetStat(field)} (+{increment})");
    }

    // === 디버깅용 초기화 (switch 문 제거!) ===

    /// <summary>
    /// 모든 스탯 초기화
    /// </summary>
    public void ResetStats()
    {
        // 재초기화
        IsInitialized = false;
        if (!Initialize())
        {
            Debug.LogWarning("[PlayerStatsManager] Failed to reset stats");
            return;
        }

        Debug.Log("[PlayerStatsManager] All stats have been reset to default values");
    }
}
