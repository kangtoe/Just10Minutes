using UnityEngine;

// 플레이어의 모든 스탯을 중앙 집중식으로 관리하는 싱글톤
public class PlayerStats : MonoSingleton<PlayerStats>
{
    [Header("업그레이드")]
    public int upgradePoint = 0;

    [Header("생존 스탯")]
    public float maxDurability = 100f;
    public float durabilityRegenRate = 0f;  // 초당 재생량
    public float durabilityRegenDelay = 5f;  // 피해 후 재생 시작까지 지연
    public float maxShield = 100f;
    public float shieldRegenRate = 20f;    // 초당 재생량
    public float shieldRegenDelay = 2f;    // 피해 후 재생 시작까지 지연

    [Header("사격 스탯")]
    public int multiShot = 1;              // 멀티샷 개수
    public float fireRate = 1f;            // 연사 속도 (fireDelay에 반영)
    public int projectileDamage = 10;      // 발사체 피해
    public float projectileSpeed = 10f;    // 발사체 속도

    [Header("충돌 스탯")]
    public float onImpact = 10f;           // 충돌 피해 (상대에게)
    public float impactResist = 0f;        // 충돌 피해 감소 (자신)

    [Header("이동 스탯")]
    public float moveSpeed = 5f;           // 이동 속도
    public float rotateSpeed = 180f;       // 회전 속도

    // 업그레이드 적용 (증분 방식)
    public void ApplyUpgrade(UpgradeField field, float increment)
    {
        switch (field)
        {
            // 생존
            case UpgradeField.MaxDurability:
                maxDurability += increment;
                break;
            case UpgradeField.MaxShield:
                maxShield += increment;
                break;
            case UpgradeField.ShieldRegenRate:
                shieldRegenRate += increment;
                break;
            case UpgradeField.ShieldRegenDelay:
                shieldRegenDelay += increment; // 음수 증분으로 지연 감소
                break;

            // 사격
            case UpgradeField.MultiShot:
                multiShot += (int)increment;
                break;
            case UpgradeField.FireRate:
                fireRate += increment;
                break;
            case UpgradeField.ProjectileDamage:
                projectileDamage += (int)increment;
                break;
            case UpgradeField.ProjectileSpeed:
                projectileSpeed += increment;
                break;

            // 충돌
            case UpgradeField.OnImpact:
                onImpact += increment;
                break;
            case UpgradeField.ImpactResist:
                impactResist += increment;
                break;

            // 이동
            case UpgradeField.MoveSpeed:
                moveSpeed += increment;
                break;
            case UpgradeField.RotateSpeed:
                rotateSpeed += increment;
                break;
        }
    }

    // 디버깅용 - 모든 스탯 초기화
    public void ResetStats()
    {
        upgradePoint = 0;

        maxDurability = 100f;
        durabilityRegenRate = 0f;
        durabilityRegenDelay = 5f;
        maxShield = 100f;
        shieldRegenRate = 20f;
        shieldRegenDelay = 2f;

        multiShot = 1;
        fireRate = 1f;
        projectileDamage = 10;
        projectileSpeed = 10f;

        onImpact = 10f;
        impactResist = 0f;

        moveSpeed = 5f;
        rotateSpeed = 180f;
    }
}
