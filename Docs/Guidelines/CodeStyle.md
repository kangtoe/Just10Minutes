# 코드 작성 원칙

> 일반적인 C# 및 Unity 코딩 컨벤션은 [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) 및 [Unity C# Coding Standards](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)를 따릅니다.
>
> 이 문서는 **AutoSpaceShooter 프로젝트 특화 규칙**만 다룹니다.

## 프로젝트 폴더 구조

```
Assets/
├── Scenes/           # 게임 씬
├── Scripts/          # C# 스크립트
│   ├── Player/       # 플레이어 관련 (이동, 사격, HP)
│   ├── Enemy/        # 적 관련 (AI, 생성, 패턴)
│   ├── Managers/     # 싱글톤 매니저 (GameManager, PoolManager 등)
│   ├── UI/           # UI (HUD, 업그레이드 선택 화면)
│   ├── Upgrades/     # 업그레이드 시스템
│   └── Utils/        # 유틸리티 (경계 체크, 수학 함수 등)
├── Prefabs/          # 프리팹 (플레이어, 적, 총알 등)
├── Sprites/          # 스프라이트 이미지
├── Materials/        # 머티리얼
└── Audio/            # 사운드 및 음악 (Phase 3+)
```

## 필수 디자인 패턴

### 1. Singleton Manager 패턴

게임 전역에서 접근해야 하는 매니저들에 사용:

```csharp
public class SomeManager : MonoSingleton<SomeManager>
{
    [SerializeField] private TextAsset someCsv;  // Inspector에서 할당

    public override void Initialize()
    {
        // 중복 초기화 방지
        if (IsInitialized)
        {
            Debug.LogWarning("[SomeManager] Already initialized!");
            return;
        }

        // 초기화 로직
        // CSV 로드, 데이터 구조 초기화 등

        IsInitialized = true;
        Debug.Log("[SomeManager] Initialized successfully");
    }
}
```

**네이밍 규칙:**
- 싱글톤 클래스는 구분하기 쉽게 **반드시 `Manager` 접미사 사용**
- 예: `PlayerStatsManager`, `GameManager`, `UpgradeManager`

**자동 초기화 시스템:**
- `MonoSingleton<T>.Instance` getter에서 **자동으로 `Initialize()` 호출**
- 첫 번째 인스턴스 접근 시 자동 초기화되므로 Unity 실행 순서와 무관
- `IsInitialized` 플래그로 중복 초기화 방지
- Script Execution Order 설정 불필요

**초기화 흐름:**
```csharp
// 어디서든 Instance에 접근하면 자동 초기화
PlayerStatsManager.Instance.GetStat(UpgradeField.MaxDurability);
// ↓
// MonoSingleton.Instance getter 실행
// ↓
// 인스턴스 생성 (씬에 없으면 동적 생성)
// ↓
// Initialize() 자동 호출
// ↓
// IsInitialized = true
```

**명시적 초기화 순서 제어 (필요 시):**
```csharp
void Start()
{
    // 1. PlayerStatsManager 초기화 (PlayerStats.csv 로드)
    // Instance 접근만으로 자동 초기화됨
    var _ = PlayerStatsManager.Instance;

    // 2. UpgradeData 초기화 (Upgrades.csv 로드 + DisplayName 병합)
    UpgradeData.Initialize(upgradesCsv);

    // 3. UI 업데이트
    UiManager.Instance.SetUpgradePointText(PlayerStatsManager.Instance.upgradePoint);
}
```

**사용 대상:**
- `GameManager`: 게임 상태, 점수, 레벨 관리
- `PoolManager`: 오브젝트 풀링
- `UpgradeManager`: 업그레이드 시스템
- `PlayerStatsManager`: 플레이어 스탯 관리 (CSV 기반)
- `InputManager`: 입력 처리
- `SoundManager`: 사운드 관리
- `UiManager`: UI 관리
- `LevelManager`: 레벨 및 경험치 관리
- `ScoreManager`: 점수 관리
- `TimeRecordManager`: 시간 기록 관리

### 2. Object Pooling 패턴

**권장 적용 대상:**
- 적 오브젝트
- 총알/발사체
- 파티클 이펙트
- 경험치 오브

**이유:** Instantiate/Destroy는 성능 부하가 크므로 반복 생성되는 오브젝트는 반드시 풀링 사용

```csharp
// 사용 예시
GameObject enemy = PoolManager.Instance.Get("Enemy");
// 사용 후
PoolManager.Instance.Return("Enemy", enemy);
```

## Unity 성능 최적화 규칙

### Update vs FixedUpdate
- **Update**: 입력 처리, UI 업데이트, 비물리 로직
- **FixedUpdate**: `Rigidbody2D` 조작 (AddForce, velocity 변경 등)

```csharp
// 플레이어 이동 - FixedUpdate 사용
private void FixedUpdate()
{
    _rb.AddForce(transform.up * moveSpeed);
}

// 입력 감지 - Update 사용
private void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        // 회전 처리
    }
}
```

### 컴포넌트 참조 캐싱

```csharp
// Good - Awake에서 한 번만 가져오기
private Rigidbody2D _rb;
private void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
}

// Bad - 매 프레임 GetComponent 호출
private void Update()
{
    GetComponent<Rigidbody2D>().AddForce(...); // ❌
}
```

### Find 함수 최소화

```csharp
// Bad - 비용이 높음
GameObject player = GameObject.Find("Player");
EnemyManager em = FindObjectOfType<EnemyManager>();

// Good - 싱글톤 또는 참조 캐싱
GameManager.Instance.Player;
EnemyManager.Instance;
```

## 필드 접근 제어 (OOP 은닉성)

### SerializeField 사용 원칙

**Inspector에서 편집 가능하게 하려면 `public` 대신 `[SerializeField]` 사용**

```csharp
// ❌ BAD - public 필드
public Image fillImage;
public float moveSpeed = 5f;

// ✅ GOOD - SerializeField + private
[SerializeField] Image fillImage;
[SerializeField] float moveSpeed = 5f;
```

**이유:**
- OOP 은닉성(Encapsulation) 원칙 준수
- 외부에서 의도치 않은 값 변경 방지
- Inspector 편집은 가능하면서 코드에서의 접근은 제한

**public이 적절한 경우:**
- 다른 스크립트에서 접근해야 하는 프로퍼티나 메서드
- 이벤트나 콜백

## 프로젝트별 주의사항

### 물리 기반 이동
- 플레이어 기체는 `Rigidbody2D.AddForce()`로 이동
- `transform.position` 직접 수정 금지 (물리 충돌 무시됨)

### 화면 경계 처리
- 경계 체크는 매니저에서 중앙 집중 관리
- 각 오브젝트가 개별적으로 체크하지 않도록 주의

### 업그레이드 시스템
- ScriptableObject 기반 데이터 구조 사용 권장
- 업그레이드 효과는 모듈화하여 조합 가능하게 설계

### UI 버튼 이벤트 할당

**Inspector 수동 연결 대신 코드에서 동적 할당**

```csharp
// ✅ GOOD - 코드에서 동적 할당
void Start()
{
    if (upgradeButton != null)
    {
        upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
    }
}

// ❌ BAD - Inspector에서 OnClick 이벤트 수동 연결
```

**이유:** 프로젝트 일관성, 실수 방지, Git 충돌 감소

### 코루틴 사용 제한

**완전 독립적인 로직이 아니면 코루틴 사용 지양**

```csharp
// ❌ BAD - 코루틴으로 타이머 관리
Coroutine deactivateCoroutine;

void OnValueChanged()
{
    if (deactivateCoroutine != null)
        StopCoroutine(deactivateCoroutine);

    deactivateCoroutine = StartCoroutine(DeactivateAfterDelay());
}

IEnumerator DeactivateAfterDelay()
{
    yield return new WaitForSeconds(0.5f);
    uiElement.SetActive(false);
    deactivateCoroutine = null;
}

// ✅ GOOD - Update에서 타이머 관리
float deactivateTimer = 0f;

void Update()
{
    if (deactivateTimer > 0f)
    {
        deactivateTimer -= Time.deltaTime;
        if (deactivateTimer <= 0f)
            uiElement.SetActive(false);
    }
}

void OnValueChanged()
{
    uiElement.SetActive(true);
    deactivateTimer = 0.5f;
}
```

**코루틴 사용이 적절한 경우:**
- 외부 API 호출 등 비동기 작업
- 복잡한 애니메이션 시퀀스
- 게임플레이 이벤트 시퀀스 (컷신 등)

**이유:**
- 코루틴은 실행 흐름 추적이 어려움
- 중지/재시작 로직이 복잡해짐
- Update 기반 타이머가 더 명확하고 디버깅 용이

## 중요: 디버깅 원칙

### 1. 에러를 숨기지 말고 즉시 드러내기 ⚠️

**절대 금지:**
```csharp
// ❌ BAD - 방어적 null check로 버그를 숨김
if (singleton != null && singleton.Instance != null)
{
    singleton.Instance.DoSomething();
}

// ❌ BAD - 조용히 실패
if (component == null) return;
if (uiElement == null) return;
```

**올바른 방식:**
```csharp
// ✅ GOOD - 에러가 즉시 발생 (null이면 NullReferenceException)
singleton.Instance.DoSomething();

// ✅ GOOD - 명시적 에러 로그
if (component == null)
{
    Debug.LogError($"Component is null on {gameObject.name}!", this);
    return;
}
```

**이유:** 방어적 코딩은 버그를 숨기고 디버깅을 어렵게 만듭니다. 문제가 발생하면 즉시 명확한 에러를 내야 합니다.

### 1-1. Early Return 패턴 사용 ⚠️

**if 중첩 대신 Early Return 사용 (로그 출력 + 즉시 리턴)**

```csharp
// ❌ BAD - if 중첩
if (data != null)
{
    if (data.IsValid())
    {
        DoSomething(data);
    }
}

// ✅ GOOD - Early return
if (data == null)
{
    Debug.LogError("Data is null!", this);
    return;
}

if (!data.IsValid())
{
    Debug.LogError("Data is invalid!", this);
    return;
}

DoSomething(data);
```

**이유:** 중첩 감소, 각 실패 케이스마다 명확한 로그

### 1-2. 중첩된 함수 호출 방지 ⚠️

**복잡한 함수 내 함수 호출로 인해 코드가 길어지는 경우, 중간 변수를 사용하여 가독성 향상**

```csharp
// ❌ BAD - 중첩된 함수 호출로 길어지고 읽기 어려움
UiManager.Instance.CreateText("No Point!", InputManager.Instance.PointerPosition);
PlayerStatsManager.Instance.ApplyUpgrade(option.field, UpgradeData.GetIncrementValue(option.type));

// ✅ GOOD - 중간 변수 사용으로 명확한 의도 표현
Vector2 touchPos = InputManager.Instance.PointerPosition;
UiManager.Instance.CreateText("No Point!", touchPos);

float incrementValue = UpgradeData.GetIncrementValue(option.type);
PlayerStatsManager.Instance.ApplyUpgrade(option.field, incrementValue);
```

**이유:**
- 코드 가독성 향상 (한 줄에 하나의 작업)
- 디버깅 시 중간 값 확인 가능
- 변수 이름을 통해 값의 의미 명확히 표현

### 2. 명확한 초기화 순서 확립 ⚠️

**절대 금지:**
```csharp
// ❌ BAD - Awake/Start 실행 순서에 의존
void Awake()
{
    // 다른 컴포넌트가 초기화되었다고 가정
    value = otherComponent.GetValue();
}

// ❌ BAD - 개별적으로 Awake에서 초기화
void Awake()
{
    // 각 컴포넌트가 독립적으로 초기화
    // 초기화 순서를 통제할 수 없음
    InitializeMyself();
}
```

**올바른 방식 (MonoSingleton 사용 시):**
```csharp
// ✅ GOOD - MonoSingleton 자동 초기화 활용
public class PlayerStatsManager : MonoSingleton<PlayerStatsManager>
{
    [SerializeField] private TextAsset playerStatsCsv;

    public override void Initialize()
    {
        if (IsInitialized) return;

        // CSV 로드 및 초기화
        // Instance 첫 접근 시 자동 호출됨
        LoadData();

        IsInitialized = true;
    }
}

// ✅ GOOD - 의존성 있는 초기화는 명시적 순서 제어
void Start()
{
    // 1. PlayerStatsManager 초기화 (Instance 접근 시 자동)
    var _ = PlayerStatsManager.Instance;

    // 2. UpgradeData 초기화 (PlayerStats에 의존)
    UpgradeData.Initialize(upgradesCsv);

    // 3. UI 업데이트 (모든 데이터 로드 후)
    UiManager.Instance.SetUpgradePointText(PlayerStatsManager.Instance.upgradePoint);
}
```

**일반 컴포넌트 초기화 예시:**
```csharp
// ✅ GOOD - 상위 컴포넌트에서 순서 제어
void Start()
{
    // 1. 먼저 자식 컴포넌트들 초기화
    childComponent1.Initialize();
    childComponent2.Initialize();

    // 2. 그 다음 자신 초기화
    Initialize();
}
```

**현재 프로젝트의 초기화 흐름:**
```
게임 시작
  ↓
UpgradeManager.Start()
  ↓
PlayerStatsManager.Instance 접근
  ↓
MonoSingleton.Instance getter → Initialize() 자동 호출
  ↓
PlayerStats.csv 로드 → StatMetadataRegistry 초기화
  ↓
UpgradeData.Initialize(upgradesCsv)
  ↓
Upgrades.csv 로드 → DisplayName 병합
  ↓
UI 업데이트
```

**이유:**
- `MonoSingleton<T>.Instance`는 첫 접근 시 자동으로 `Initialize()` 호출
- 의존성이 있는 초기화만 `Start()`에서 명시적 순서 제어
- Unity의 Awake/Start 실행 순서에 의존하지 않음
- Script Execution Order 설정 불필요

## 참고 자료

- [Unity C# Coding Standards](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Unity Performance Optimization](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)
