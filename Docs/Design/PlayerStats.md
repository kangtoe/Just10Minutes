# 플레이어 스탯 시스템

> 플레이어 기체의 능력치와 업그레이드 가능한 스탯 정의

## 생존 시스템: 실드/내구도 분리

### 실드 (Shield)
- **우선 피해 흡수**: 모든 피해를 실드가 먼저 받음
- **자동 재생**: 일정 시간 피해를 받지 않으면 자동 회복
  - 재생 지연: 마지막 피해 후 2초 대기
  - 재생 속도: 초당 최대 실드의 20%
- **소진 가능**: 실드가 0이 되어도 게임은 계속됨

### 내구도 (Durability)
- **최후의 방어선**: 실드 소진 후 피해 받음
- **회복 제한**: 레벨업 시 일부 회복만 가능
- **0 = 게임 오버**: 내구도가 0이 되면 즉시 사망

### UI 표시
```
[=====실드=====] 100/100 (파란색/청록색)
[====내구도====]  80/100 (빨간색)
```

## Phase 1 핵심 스탯

### 즉시 구현 가능 (기존 코드)
1. **최대 내구도** - `Damageable.maxDurability`
2. **최대 실드** - `Damageable.maxShield` (신규)
3. **충돌 피해 (공격)** - `Impactable.impactDamageOther`
4. **멀티샷** - `ShooterBase.shotCountPerFirepoint`

### 추가 구현 필요
5. **연사 속도** - `ShooterBase.fireDelay` (감소)
6. **발사체 피해** - `ShooterBase.damage`
7. **발사체 속도** - `ShooterBase.projectileMovePower`
8. **이동 속도** - `MoveStandard.movePower`
9. **회전 속도** - `RotateByInput.rotationSpeed`
10. **실드 재생 속도** - `Damageable.shieldRegenRate` (신규)
11. **실드 재생 지연** - `Damageable.shieldRegenDelay` (신규)

## 스탯 카테고리 전체

| 카테고리 | 주요 스탯 | 구현 위치 | Phase 1 |
|---------|----------|-----------|---------|
| **생존** | 최대 내구도, 최대 실드, 실드 재생 속도/지연 | Damageable | ✅ |
| **이동** | 이동 속도, 회전 속도 | MoveStandard, RotateByInput | ✅ |
| **사격** | 연사 속도, 멀티샷, 발사체 피해/속도 | ShooterBase | ✅ |
| **충돌** | 충돌 피해 (공격/자신), 충격력 | Impactable | 피해만 |
| **특수** | 크리티컬, 관통, 유도 | - | Phase 2+ |

## UpgradeField Enum 확장

```csharp
public enum UpgradeField
{
    // 생존
    MaxDurability,    // 최대 내구도
    MaxShield,        // 최대 실드
    ShieldRegenRate,  // 실드 재생 속도
    ShieldRegenDelay, // 실드 재생 지연 (감소)

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

**탱커 빌드**: 최대 내구도↑↑ + 최대 실드↑↑ + 실드 재생↑ + 충돌 피해↑
**사격 빌드**: 연사↑↑ + 멀티샷↑↑ + 발사체 피해↑
**충돌 빌드**: 충돌 피해↑↑↑ + 이동 속도↑↑ + 실드↑
**기동 빌드**: 이동 속도↑↑↑ + 회전 속도↑↑ + 실드 재생↑↑
**균형 빌드**: 모든 스탯 골고루 상승

## 기본값 (Phase 1)

### 생존
- 최대 내구도: 100
- 최대 실드: 100
- 실드 재생 속도: 20/초 (최대 실드의 20%)
- 실드 재생 지연: 2초

### 이동
- 이동 속도: 10 (movePower)
- 회전 속도: 180 deg/s

### 사격
- 연사 속도: 0.2초 (5발/초)
- 멀티샷: 1발
- 발사체 피해: 10
- 발사체 속도: 10

### 충돌
- 충돌 피해 (공격): 10
- 충돌 피해 (자신): 10
