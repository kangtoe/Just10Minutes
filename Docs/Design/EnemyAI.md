# 적 AI 시스템

## 개요

AutoSpaceShooter의 적은 **컴포넌트 조합 방식**으로 다양한 AI 패턴을 구현합니다.
각 적은 `FindTarget`, `MoveStandard`, `RotateToTarget`, `ShooterBase` 등의 컴포넌트를 조합하여 고유한 행동 패턴을 가집니다.

## AI 패턴 분류

### 1. 직진 추적형 (Tracking Pattern)
**구성**: `FindTarget` + `RotateToTarget` + `MoveStandard`

플레이어를 탐색하고, 천천히 회전하면서 바라보는 방향으로 자동 전진합니다.

**특징**:
- 가장 기본적이고 예측 가능한 패턴
- 회전 속도(`turnSpeed`)로 난이도 조절
- 이동 속도(`movePower`)가 낮아 회피 가능

**해당 적**:
- **Enemy_light_child**: Mass 0.5, Power 2 - 가장 기본적인 추적형
- **Enemy_mid_hornet**: Mass 1.5, Power 2 + 사격 - 멀티샷 장착
- **Enemy_mid_tank**: Mass 4, Power 0.5 + 사격 - 느리지만 내구도 높음
- **Enemy_mid_sniper**: Mass 1, Power 0.5 + 사격 - 원거리 저속 사격
- **Enemy_heavy_mother**: Mass 5, Power 0.5 + 소환 - Child 생성
- **Enemy_Boss**: Mass 100, Power 0.1 + 멀티 사격 - 거의 고정

### 2. 돌격형 (Pursuit Pattern)
**구성**: `FindTarget` + `MoveToTarget` 또는 `MoveTowardTarget`

현재 바라보는 방향과 무관하게 플레이어 방향으로 직진합니다.

**특징**:
- 회전 없이 플레이어 방향으로 즉시 이동
- 충돌 회피가 어려운 위협적인 패턴
- 임펄스 기반(`MoveToTarget`)과 부드러운 접근(`MoveTowardTarget`)으로 나뉨

**해당 적**:
- **Enemy_light_thunder**: Mass 0.5, Power 5 (임펄스) - 빠른 돌진
- **Enemy_mid_knight**: Mass 8, Power 10 (임펄스) - 무거운 돌격
- **Enemy_mid_spiral**: Mass 2, Power 2 (부드러운 접근) + 4방향 사격
- **Enemy_mid_master**: Mass 1, Power 1.5 (부드러운 접근) + 드론 소환

### 3. 거리 유지형 (Kiting Pattern)
**구성**: `FindTarget` + `MoveAwayTarget` + `RotateToTarget` + `ShooterBase`

플레이어와 일정 거리를 유지하며 사격합니다.

**특징**:
- 플레이어가 접근하면 후퇴
- 일정 거리 이상 멀어지면 추적 중단
- 원거리 공격자로 설계됨

**해당 적**:
- **Enemy_heavy_Gunship**: Mass 4, Power 3, 유지 거리 6 + 2방향 사격

### 4. 파동 이동형 (Wave Pattern)
**구성**: `WaveMovement` + `FindTarget` + `ShooterBase`

사인파 궤적으로 이동하며 사격합니다.

**특징**:
- 좌우 또는 상하로 흔들리며 이동
- 탄도 예측 어려움
- amplitude(진폭), frequency(주파수) 파라미터로 조절

**해당 적**:
- **Enemy_mid_Ghost**: Mass 1, Power 1, 진폭 1, 주파수 1

### 5. 단순 직진형 (Direct Pattern)
**구성**: `MoveStandard` (FindTarget 없음)

회전 없이 한 방향으로 직진만 합니다.

**특징**:
- 가장 단순한 패턴
- 예측 가능하고 회피 쉬움
- 대량 생성 시 위협적

**해당 적**:
- **Enemy_light_shield**: Mass 1.5, Power 0.5 - 높은 HP (120)

## 적 능력별 분류

### 사격 능력 보유
사격 컴포넌트(`ShooterBase`)를 가진 적:
- **Enemy_light_kido** (60점)
- **Enemy_mid_Ghost** (250점) - 파동 이동
- **Enemy_mid_Hornet** (350점) - 멀티샷
- **Enemy_mid_Spiral** (600점) - 4방향
- **Enemy_mid_tank** (550점)
- **Enemy_mid_sniper** (500점)
- **Enemy_heavy_Gunship** (1000점) - 2방향
- **Enemy_Boss** (1000점) - 3개 무기

### 소환 능력 보유
Factory 컴포넌트로 자식 유닛 생성:
- **Enemy_heavy_mother** (800점) - light_child 생성
- **Enemy_mid_master** (400점) - 드론 소환

## 적 스탯 테이블

| 등급 | 이름 | HP | 점수 | Mass | Power | AI 패턴 | 특수 능력 |
|------|------|-----|------|------|-------|---------|-----------|
| Light | child | 60 | 50 | 0.5 | 2 | 직진 추적 | - |
| Light | kido | 40 | 60 | 0.5 | 1 | 직진 추적 | 사격 |
| Light | shield | 120 | 100 | 1.5 | 0.5 | 단순 직진 | 고HP |
| Light | thunder | 50 | 80 | 0.5 | 5 | 임펄스 돌격 | - |
| Mid | Ghost | 100 | 250 | 1 | 1 | 파동 이동 | 사격 |
| Mid | Hornet | 250 | 350 | 1.5 | 2 | 직진 추적 | 멀티샷 |
| Mid | Knight | 300 | 450 | 8 | 10 | 임펄스 돌격 | - |
| Mid | Spiral | 300 | 600 | 2 | 2 | 부드러운 돌격 | 4방향 사격 |
| Mid | tank | 500 | 550 | 4 | 0.5 | 직진 추적 | 고HP + 사격 |
| Mid | sniper | 200 | 500 | 1 | 0.5 | 직진 추적 | 원거리 사격 |
| Mid | master | 100 | 400 | 1 | 1.5 | 부드러운 돌격 | 드론 소환 |
| Heavy | mother | 800 | 800 | 5 | 0.5 | 직진 추적 | Child 소환 |
| Heavy | Gunship | 500 | 1000 | 4 | 3 | 거리 유지 | 2방향 사격 |
| Boss | Boss | 99999 | 1000 | 100 | 0.1 | 직진 추적 | 3무기 |
| Bonus | bonus | 200 | 0 | - | - | - | 테스트용 |

## 난이도 설계

### Light 등급 (50-100점)
- HP: 40-120
- 단순한 패턴 (직진, 느린 추적)
- 초반 웜업용

### Mid 등급 (250-600점)
- HP: 100-500
- 다양한 패턴 (파동, 임펄스, 사격)
- 메인 위협 요소

### Heavy 등급 (800-1000점)
- HP: 500-800
- 복잡한 메카닉 (소환, 거리 유지)
- 주요 장애물

### Boss 등급 (1000점)
- HP: 99999
- 멀티 무기 시스템
- 거의 이동하지 않음

## 자동 전진 시스템과의 호환성

### ✅ 호환 가능 패턴
1. **직진 추적형**: 플레이어도 자동 전진하므로 서로 추격전 형성
2. **돌격형**: 빠른 접근으로 회피 긴장감 유지
3. **거리 유지형**: 플레이어 전진을 역이용하는 패턴
4. **파동 이동형**: 독립적인 궤적으로 예측 어려움

### ⚠️ 조정 필요 사항
- **회전 속도 밸런싱**: 너무 빠르면 항상 정면 대치만 발생
- **이동 속도 밸런싱**: 플레이어보다 너무 빠르면 도망 불가
- **소환 빈도**: 자동 전진으로 화면이 복잡해지기 쉬움

## 파라미터 조정 가이드

### turnSpeed (회전 속도)
- **느림 (0.5-1.0)**: 회피 가능한 추적
- **중간 (1.0-2.0)**: 긴장감 있는 추적
- **빠름 (2.0+)**: 항상 정면 대치

### movePower (이동 힘)
- **느림 (0.1-0.5)**: 거의 고정, 보스/탱크용
- **중간 (1.0-2.0)**: 표준 추적
- **빠름 (5.0-10.0)**: 돌격형, 임펄스용

### searchRadius (탐색 범위)
- **좁음 (5-10)**: 근접에서만 반응
- **중간 (15-20)**: 화면 중간부터 추적
- **넓음 (30+)**: 화면 전체 추적

### fireDelay (사격 간격)
- **느림 (1.0+초)**: 저위협 원거리
- **중간 (0.5-1.0초)**: 표준 사격
- **빠름 (0.2-0.5초)**: 고위협 사격

## 컴포넌트 조합 예시

### 새로운 적 만들기
```
기본 추적형 적:
- EnemyShip (점수, 설명)
- Damageable (HP, 사운드)
- BoundaryJump (화면 경계 처리)
- FindTarget (targetLayer: Player)
- RotateToTarget (turnSpeed: 1.0)
- MoveStandard (movePower: 2.0)
- Rigidbody2D (mass, linearDamping)
- Collider2D

사격 추가:
+ ShooterBase (fireDelay, damage, projectilePrefab)

돌격형으로 변경:
- RotateToTarget 제거
+ MoveToTarget (movePower: 5.0)

거리 유지형으로 변경:
+ MoveAwayTarget (distance: 6, relaxZone: 0.5)
```

## 참고

- [Architecture.md](../Architecture.md#22-게임플레이-레이어): 전체 시스템 구조
- [GameDesignOverview.md](GameDesignOverview.md): 게임 디자인 개요
