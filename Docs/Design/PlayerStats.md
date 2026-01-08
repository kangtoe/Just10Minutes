# 플레이어 스탯 시스템

> 플레이어 기체의 능력치와 업그레이드 가능한 스탯 정의

## Phase 1 핵심 스탯

### 즉시 구현 가능 (기존 코드)
1. **최대 체력** - `Damageable.maxHealth`
2. **충돌 피해 (공격)** - `Impactable.impactDamageOther`
3. **멀티샷** - `ShooterBase.shotCountPerFirepoint`

### 추가 구현 필요
4. **연사 속도** - `ShooterBase.fireDelay` (감소)
5. **발사체 피해** - `ShooterBase.damage`
6. **발사체 속도** - `ShooterBase.projectileMovePower`
7. **이동 속도** - `MoveStandard.movePower`
8. **회전 속도** - `RotateByInput.rotationSpeed`

## 스탯 카테고리 전체

| 카테고리 | 주요 스탯 | 구현 위치 | Phase 1 |
|---------|----------|-----------|---------|
| **생존** | 최대 체력, 체력 재생, 무적 시간 | Damageable | 체력만 |
| **이동** | 이동 속도, 회전 속도 | MoveStandard, RotateByInput | ✅ |
| **사격** | 연사 속도, 멀티샷, 발사체 피해/속도 | ShooterBase | ✅ |
| **충돌** | 충돌 피해 (공격/자신), 충격력 | Impactable | 피해만 |
| **특수** | 크리티컬, 관통, 유도 | - | Phase 2+ |

## UpgradeField Enum 확장

```csharp
public enum UpgradeField
{
    // 생존
    Shield,           // 최대 체력

    // 사격
    FireRate,         // 연사 속도 (fireDelay 감소)
    ProjectileDamage, // 발사체 피해
    ProjectileSpeed,  // 발사체 속도
    MultiShot,        // 멀티샷

    // 충돌
    OnImpact,         // 충돌 피해 (공격)
    ImpactResist,     // 충돌 피해 (자신) 감소

    // 이동
    MoveSpeed,        // 이동 속도
    RotateSpeed,      // 회전 속도
}
```

## 빌드 예시

**사격 빌드**: 연사↑↑ + 멀티샷↑↑ + 발사체 피해↑
**충돌 빌드**: 충돌 피해↑↑↑ + 이동 속도↑↑ + 체력↑↑
**균형 빌드**: 모든 스탯 골고루 상승

## 기본값 (Phase 1)

- 최대 체력: 100
- 이동/회전 속도: 10 / 180 deg/s
- 연사 속도: 0.2초 (5발/초)
- 멀티샷: 1발
- 발사체 피해/속도: 10 / 10
- 충돌 피해: 10 / 10 (공격/자신)
