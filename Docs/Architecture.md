# AutoSpaceShooter 아키텍처 문서

> 기존 스페이스 슈터 프로젝트를 기반으로 한 Auto-Forward 방식 게임의 코드 구조 및 시스템 설계

## 1. 개요

### 1.1 아키텍처 패턴

- **싱글톤 매니저 패턴**: 모든 핵심 매니저는 MonoSingleton 상속
- **컴포넌트 기반 설계**: Unity의 컴포넌트 시스템 활용, 상속보다 조합 선호
- **이벤트 기반**: UnityEvent를 통한 느슨한 결합
- **물리 기반 이동**: Rigidbody2D.AddForce() 사용

### 1.2 폴더 구조

```
Assets/Scripts/
├── Singleton/              # 싱글톤 베이스 클래스
├── (루트)                  # 게임 매니저들
├── Player/                 # 플레이어 컴포넌트
├── SpaceShips/            # 우주선 공통 시스템
├── Movements/             # 이동/회전 컴포넌트
├── Projectiles/           # 발사체 시스템
├── UIs/                   # UI 컴포넌트
├── Sounds/                # 사운드 시스템
└── Drone/                 # 드론 시스템 (Phase 2-3)
```

## 2. 레이어 구조

### 2.1 매니저 레이어 (싱글톤)

**핵심 매니저** (MonoSingleton 상속):
```
GameManager          # 게임 상태 및 흐름 제어
InputManager         # Unity New Input System 통합
UiManager           # 모든 UI 요소 통합 관리
SoundManager        # AudioClip 재생 및 AudioMixer 관리
LevelManager        # 레벨/경험치 시스템
UpgradeManager      # 업그레이드 시스템
ScoreManager        # 점수 관리
TimeRecordManager   # 플레이 시간 기록
EnemySpawner        # 적 생성 관리
ObjectSpawner       # 오브젝트 생성 및 배치
```

### 2.2 게임플레이 레이어

**플레이어 시스템**:
- PlayerShip: 플레이어 메인 컨트롤러
- HeatSystem: 사격 과열 시스템
- Impactable: 충돌 피해 시스템
- BrakeSystem: 브레이크 (미사용 예정)

**적 시스템**:
- EnemyShip: 적 기체 및 보상 처리
- FindTarget: 타겟 자동 탐색

**공통 시스템**:
- Damageable: HP 시스템 (PlayerShip, EnemyShip 필수)
- ShooterBase: 사격 시스템
- BulletBase: 발사체 베이스 클래스

### 2.3 시스템 레이어

**물리/이동**:
- MoveStandard: 물리 기반 이동 (재사용 가능)
- BoundaryJump: 화면 경계 텔레포트 (수정 필요)
- 각종 이동/회전 컴포넌트

**UI**:
- Crosshair, FloatingText, FadeUI 등

**사운드**:
- SoundManager를 통한 중앙 집중식 관리

### 2.4 데이터 레이어

- **UpgradeData**: 업그레이드 데이터 정적 클래스
- **Enums**: 게임 전역 열거형

## 3. 핵심 시스템 상세

### 3.1 게임 상태 관리 (GameManager)

**게임 상태 흐름**:
```
OnTitle → GameStart() → OnCombat ⇄ OnUpgrade (U키)
                           ↓
                        GameOver (플레이어 사망)
```

**주요 기능**:
- 게임 상태 전환 및 관리
- PlayerShip 참조 관리
- 슬로우 모션 효과 (Time.timeScale 조절)

**의존성**: UiManager, SoundManager, TimeRecordManager

### 3.2 입력 시스템 (InputManager)

**제공 프로퍼티**:
```csharp
Vector2 MoveDirectionInput  // WASD/화살표
float RotateInput          // A/D 회전
bool FireInput             // 마우스/터치/스페이스
bool PauseInput            // ESC
bool EscapeInput           // ESC 별칭
```

**특징**:
- Unity New Input System (v1.17.0) 사용
- 모바일 터치 지원
- 키보드, 마우스, 게임패드, 터치 지원

### 3.3 전투 시스템

#### 3.3.1 Damageable (HP 시스템)

**구조**:
```csharp
float maxHealth, currHealth
bool isDead
UnityEvent onDead, onDamaged
GameObject diePrefab
AudioClip deathSound, hitSound
```

**피해 처리 흐름**:
```
GetDamaged(damage, attacker) → currHealth 감소 →
  onDamaged.Invoke() →
    currHealth == 0 → onDead.Invoke() →
      Instantiate(diePrefab) → Destroy(gameObject)
```

**사용처**: PlayerShip, EnemyShip (필수 컴포넌트)

#### 3.3.2 ShooterBase (사격 시스템)

**핵심 파라미터**:
```csharp
Transform[] firePoints           // 발사 지점
LayerMask targetLayer           // 공격 대상
GameObject projectilePrefab     // 발사체 프리팹

float fireDelay                 // 발사 간격
int shotCountPerFirepoint       // 멀티샷 개수
float shotAngle                 // 탄환 각도 간격
int damage, impactPower         // 피해량, 넉백
float projectileMovePower       // 발사체 속도
```

**발사 흐름**:
```
TryFire() → Available 체크 → Fire() →
  foreach firePoint → FireMulty() →
    for shotCount → 위치/회전 계산 →
      Instantiate(projectile) → BulletBase.Init()
```

**멀티샷 배치 패턴**:
- 홀수: 중앙 기준 좌우 대칭
- 짝수: 중앙 기준 좌우 0.5칸씩 이동
- 바깥쪽 탄환일수록 속도 감소 (outsideSlower)

#### 3.3.3 BulletBase (발사체)

**소멸 조건**:
1. liveTime 경과
2. 화면 밖으로 나감
3. 타겟 충돌

**충돌 처리**:
```csharp
OnHitDestory(hitColl) {
    Damageable.GetDamaged(damage)
    Rigidbody2D.AddForce(dir * impact)
    Instantiate(hitEffect)
    if (destoryOnHit) Destroy(gameObject)
}
```

**특수 발사체**:
- BulletCurve: 곡선 궤도
- BulletChase: 타겟 추적
- Pulse: 확대되는 원형 범위 공격

#### 3.3.4 Impactable (충돌 피해)

**충돌 효과**:
1. 양쪽에 피해 (`impactDamageSelf`, `impactDamageOther`)
2. 양쪽에 힘 (`impactPowerSelf`, `impactPowerOther`)
3. 충돌 이펙트 생성

**사용처**: PlayerShip (충돌 빌드용)

### 3.4 성장 시스템

#### 3.4.1 LevelManager (레벨/경험치)

**레벨업 흐름**:
```
GetExp(amount) → exp 누적 →
  while (exp >= NextLevelExp) →
    LevelUp() → UpgradeManager.PointUp()
```

**레벨 공식**:
```csharp
NextLevelExp = level * 1100
```

**이벤트**: `onLevelUp` (PlayerShip에서 구독)

#### 3.4.2 UpgradeManager (업그레이드)

**현재 업그레이드 타입**:
```csharp
enum UpgradeType {
    Ship,               // 실드 업그레이드
    Shooter,            // 멀티샷, 히트 업그레이드
    EmergencyProtocol   // 전체 복구
}
```

**업그레이드 적용 흐름**:
```
TryUsePoint(type) → point 소비 →
  UpgradeData.GetFieldInfo() →
    PlayerShip.SetSystem(field, amount)
```

**⚠️ 수정 필요**: 고정 3개 타입 → 선택지 시스템으로 확장

### 3.5 물리 이동 시스템

#### 3.5.1 MoveStandard (이동)

**핵심 파라미터**:
```csharp
bool moveManually = false;    // false: 자동 전진
bool limitMaxSpeed = true;    // 최대 속도 제한
float movePower = 10f;        // 이동 힘
```

**이동 메커니즘**:
```csharp
// FixedUpdate
if (!moveManually) {
    rbody.AddForce(transform.up * movePower * rbody.mass);
}

// 최대 속도 제한
limit = movePower * rbody.mass / rbody.linearDamping;
rbody.linearVelocity = Vector2.ClampMagnitude(velocity, limit);
```

**✅ 새 게임 방식 적용 가능**:
- `moveManually = false` → 자동 전진
- 회전만 조작 → transform.up 자동 변경 → 이동 방향 변경

#### 3.5.2 BoundaryJump (경계 처리)

**현재 동작**: 화면 경계 → 반대편 텔레포트

**⚠️ 새 게임 방식과 충돌**:
- 현재: 경계 → 텔레포트
- 목표: 경계 → 즉사

**해결 방법**: BoundaryDeath 컴포넌트 신규 작성

## 4. 데이터 흐름

### 4.1 게임플레이 루프

```
GameStart() →
  TimeRecordManager.SetActiveCount(true) →
  EnemySpawner 시작 →
    ObjectSpawner.SpawnObjects() →
      EnemyShip 생성

PlayerShip 사격 →
  HeatSystem 체크 →
    ShooterBase.TryFire() →
      BulletBase 생성 →
        적 충돌 →
          Damageable.GetDamaged() →
            onDead →
              ScoreManager.AddScore() →
              LevelManager.GetExp() →
                레벨업 →
                  UpgradeManager.PointUp()
```

### 4.2 전투 데이터 흐름

```
플레이어 사격:
  InputManager.FireInput →
    PlayerShip.FireCheck() →
      HeatSystem.CanFire() →
        ShooterBase.TryFire() →
          BulletBase.Init(ownerLayer, targetLayer, damage, ...) →
            충돌 →
              EnemyShip.Damageable.GetDamaged()

적 충돌:
  PlayerShip.Impactable.OnCollisionEnter2D() →
    자신 피해: Damageable.GetDamaged(impactDamageSelf) →
      onDamaged → UiManager.SetHealthUI() + ShakeUI()
    상대 피해: other.Damageable.GetDamaged(impactDamageOther)
    양쪽 힘: AddForce()
```

### 4.3 업그레이드 데이터 흐름

```
레벨업:
  LevelManager.onLevelUp →
    UpgradeManager.PointUp(1)

업그레이드 구매:
  UpgradeButtonUI.OnClick() →
    UpgradeManager.TryUsePoint(type) →
      UpgradeData.GetRalatedFields(type) →
        foreach field →
          UpgradeData.GetFieldInfo(field, level) →
            PlayerShip.SetSystem(field, amount)

PlayerShip.SetSystem():
  case Shield → Damageable.SetMaxHealth()
  case MultiShot → ShooterBase.SetMultiShot()
  case Heat → heatPerShot 조정
  case OnImpact → Impactable.SetDamageAmount()
```

## 5. 컴포넌트 설계

### 5.1 베이스 클래스

**MonoSingleton<T>**:
```csharp
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T instance = null;
    public static T Instance { get; }  // Thread-Safe
}
```

**주요 베이스 클래스**:
- MonoSingleton: 매니저용
- Damageable: HP 시스템
- ShooterBase: 사격 시스템
- BulletBase: 발사체

### 5.2 컴포넌트 조합 패턴

**PlayerShip 조합**:
```
PlayerShip
├── Damageable (HP)
├── ShooterBase (사격)
├── HeatSystem (과열)
├── MoveStandard (이동)
├── Impactable (충돌)
└── BrakeSystem (브레이크, 미사용)
```

**EnemyShip 조합**:
```
EnemyShip
├── Damageable (필수)
├── BoundaryJump (필수)
├── ShooterBase (선택)
├── FindTarget (선택)
└── 이동 컴포넌트 (선택)
```

**특징**: 상속보다 조합을 선호하여 유연한 설계

### 5.3 이벤트 시스템

**UnityEvent 사용 예시**:
```csharp
// Damageable
UnityEvent onDead;
UnityEvent onDamaged;

// LevelManager
UnityEvent onLevelUp;

// PlayerShip에서 구독
LevelManager.Instance.onLevelUp.AddListener(OnLevelUp);
damageable.onDamaged.AddListener(OnDamaged);
damageable.onDead.AddListener(OnDead);
```

**장점**: 느슨한 결합, Inspector에서 설정 가능

## 6. 확장 포인트

### 6.1 새 업그레이드 추가

**단계**:
1. UpgradeData.cs에 새 UpgradeField 추가
2. UpgradeData.Datas에 레벨별 수치 추가
3. PlayerShip.SetSystem()에 case 추가
4. UI 버튼 연결

**예시**:
```csharp
// 1. Enums.cs
enum UpgradeField {
    // 기존...
    ProjectileSpeed  // 새 업그레이드
}

// 2. UpgradeData.cs
Datas.Add(UpgradeField.ProjectileSpeed, new float[] {
    1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f  // 레벨별 배속
});

// 3. PlayerShip.cs
void SetSystem(UpgradeField type, float amount) {
    switch (type) {
        // 기존...
        case UpgradeField.ProjectileSpeed:
            shooter.SetProjectileSpeed(amount);
            break;
    }
}

// 4. ShooterBase.cs에 SetProjectileSpeed 메서드 추가
```

### 6.2 새 적 타입 추가

**단계**:
1. 적 프리팹 생성 (Sprite, Rigidbody2D, Collider2D)
2. EnemyShip 컴포넌트 부착
3. Damageable 부착 및 설정 (HP, 사운드, 이펙트)
4. BoundaryJump 부착 및 설정
5. 선택적 컴포넌트 추가:
   - ShooterBase (사격하는 적)
   - FindTarget (플레이어 추적)
   - MoveToTarget (플레이어에게 접근)
6. EnemySpawner의 SpawnInfo에 등록

### 6.3 새 발사체 추가

**단계**:
1. BulletBase 상속 클래스 작성
2. 특수 이동 로직 구현 (Update, FixedUpdate)
3. 발사체 프리팹 생성
4. ShooterBase의 projectilePrefab에 연결

**예시**:
```csharp
// BulletSplit.cs (분열 탄환)
public class BulletSplit : BulletBase
{
    int splitCount = 3;

    protected override void OnHitDestory(Collision2D hitColl)
    {
        // 충돌 시 분열
        for (int i = 0; i < splitCount; i++) {
            float angle = 360f / splitCount * i;
            Instantiate(splitPrefab, transform.position, Quaternion.Euler(0, 0, angle));
        }
        base.OnHitDestory(hitColl);
    }
}
```

## 7. 마이그레이션 가이드

### 7.1 즉시 재사용 가능 시스템

**✅ 그대로 사용**:
- 모든 싱글톤 매니저 (GameManager, UiManager, SoundManager, LevelManager, ScoreManager, TimeRecordManager, EnemySpawner, ObjectSpawner)
- Damageable, ShooterBase, BulletBase 및 모든 파생 클래스
- HeatSystem, Impactable
- UI 시스템 전체
- 발사체 시스템 전체

### 7.2 수정 필요 시스템

**⚠️ 수정 후 재사용**:

#### BoundaryJump → BoundaryDeath
**현재**: 경계 → 텔레포트
**목표**: 경계 → 즉사

**해결책**: 새 컴포넌트 작성
```csharp
// BoundaryDeath.cs (신규 작성)
public class BoundaryDeath : MonoBehaviour
{
    void Start() {
        InvokeRepeating(nameof(CheckBoundary), 0.1f, 0.1f);
    }

    void CheckBoundary() {
        // 카메라 경계 계산
        // 오브젝트 크기 고려
        // 경계 벗어나면 Damageable.GetDamaged(9999)
    }
}
```

**적용**:
- PlayerShip: BoundaryDeath 사용
- EnemyShip: BoundaryJump 유지 (destoryJumpCount = 5)

#### LookMouseSmooth → RotateByInput
**현재**: 마우스 위치 추적 회전
**목표**: 입력 방향으로 회전

**해결책**: 새 컴포넌트 작성
```csharp
// RotateByInput.cs (신규 작성)
public class RotateByInput : MonoBehaviour
{
    float rotateSpeed = 180f;  // 도/초

    void Update() {
        float input = InputManager.Instance.RotateInput;
        transform.Rotate(0, 0, -input * rotateSpeed * Time.deltaTime);
    }
}
```

#### UpgradeManager 확장
**현재**: 고정 3개 타입 (Ship, Shooter, EmergencyProtocol)
**목표**: 레벨업 시 랜덤 선택지 3-5개 제시

**변경 사항**:
- 업그레이드 풀 시스템 구현
- 선택지 UI 구현
- 업그레이드 데이터 확장

#### PlayerShip 설정 조정
**변경**:
```csharp
// MoveStandard
moveManually = false;  // 자동 전진 활성화

// PlayerShip.MoveCheck() 제거 또는 수정
// 회전만 입력으로 조작
```

### 7.3 삭제 대상

**❌ Phase 1에서 제거**:
- **BrakeSystem**: InputManager.BrakeInput = false로 비활성화되어 있음, 자동 전진 방식에서 불필요

**❓ 보류 (Phase 2-3에서 활용)**:
- **DroneMaster, DroneSevant**: 적 패턴으로 활용 가능
- **StackSystem, StackWeapon**: 특수 무기 시스템으로 활용 가능

**✅ 유지 (적 AI 및 이펙트용)**:
- MoveImpulse, MoveToTarget, RotateToTarget (적 AI)
- RotateByVelocity, RotateObject (이펙트)
- 모든 특수 발사체 (BulletChase, BulletCurve 등)
- UI 애니메이션 (FadeUI, BlinkUI 등)

## 8. 아키텍처 평가

### 8.1 강점

1. **명확한 책임 분리**: 각 매니저가 명확한 역할
2. **싱글톤 패턴 일관성**: 모든 매니저가 동일한 패턴
3. **이벤트 기반 설계**: UnityEvent로 느슨한 결합
4. **컴포넌트 조합**: 상속보다 조합 선호 (유연성)
5. **물리 기반 이동**: Rigidbody2D로 자연스러운 움직임
6. **높은 재사용성**: 대부분의 시스템을 새 게임에 재사용 가능

### 8.2 개선 가능 영역

1. **매니저 과다**: ScoreManager + TimeRecordManager 통합 가능
2. **의존성 관리**: GameManager ↔ UiManager 순환 참조 가능성
3. **레이어 하드코딩**: Layer 8, 9 등 하드코딩 (const 또는 enum 권장)
4. **오브젝트 풀링 부재**: 발사체 생성/파괴 빈번 (성능 이슈 가능)
5. **테스트 용이성**: 싱글톤 패턴으로 인한 단위 테스트 어려움

### 8.3 권장 개선 사항 (Phase 2-3)

1. **오브젝트 풀링 도입**: 발사체, 이펙트
2. **ScriptableObject 데이터**: UpgradeData를 SO로 전환
3. **레이어 관리 개선**: LayerManager 또는 Constants 클래스
4. **이벤트 버스 도입**: 싱글톤 의존성 감소
5. **단위 테스트 작성**: 핵심 로직 (LevelManager, UpgradeData 등)

---

## 참고

- [Migration.md](Migration.md): 마이그레이션 상세 계획
- [TestingGuide.md](Guidelines/TestingGuide.md): 테스트 작성 가이드
- [CodeStyle.md](Guidelines/CodeStyle.md): 코드 스타일 가이드
