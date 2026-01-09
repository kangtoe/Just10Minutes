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
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

**사용 대상:**
- `GameManager`: 게임 상태, 점수, 레벨 관리
- `PoolManager`: 오브젝트 풀링
- `UpgradeManager`: 업그레이드 시스템
- `InputManager`: 입력 처리 (선택사항)

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

**올바른 방식:**
```csharp
// ✅ GOOD - 명시적 Initialize 메소드 + 명확한 호출 순서
public void Initialize()
{
    // 외부에서 명시적으로 호출되어야 함
    value = CalculateValue();
}

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

**계층적 초기화 예시:**
```csharp
// Manager가 최상위에서 초기화 순서 제어
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 1. UI 초기화
        UiManager.Instance.Initialize();

        // 2. 플레이어 초기화 (UI에 의존)
        PlayerShip.Instance.Initialize();

        // 3. 게임 시작
        StartGame();
    }
}
```

**이유:**
- Awake/Start 실행 순서는 Unity가 결정하므로 통제 불가능
- 각 컴포넌트가 독립적으로 Awake에서 초기화하면 의존성 문제 발생
- 명확한 초기화 순서를 상위 레벨에서 제어해야 디버깅 가능

## 참고 자료

- [Unity C# Coding Standards](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Unity Performance Optimization](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)
