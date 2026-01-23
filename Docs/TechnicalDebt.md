# 기술 부채 및 개선 사항

> 프로젝트의 장기적 유지보수성과 확장성을 위한 기술적 개선 과제

## 개요

이 문서는 AutoSpaceShooter 프로젝트의 기술 부채와 개선이 필요한 아키텍처 이슈를 추적합니다. 각 항목은 우선순위와 예상 영향도를 포함하며, 로드맵과 연계하여 점진적으로 해결할 계획입니다.

---

## 1. 성능 최적화

### 1.1 오브젝트 풀링 시스템 ⚠️ 최우선

**현재 문제**:
- 발사체, 적, FloatingText가 매 프레임 Instantiate/Destroy 됨
- 가비지 컬렉션 부하 증가
- 모바일에서 프레임 드롭 발생 가능

**영향도**: 🔴 높음 (성능)
**우선순위**: P0 (최우선)
**예상 소요**: 1-2일

**해결 방안**:
```csharp
// GenericPool<T> 제네릭 풀 클래스 작성
public class GenericPool<T> where T : Component
{
    private Queue<T> pool;
    private T prefab;
    private Transform parent;

    public T Get() { /* ... */ }
    public void Return(T obj) { /* ... */ }
}

// 사용 예시
GenericPool<BulletBase> bulletPool;
GenericPool<EnemyShip> enemyPool;
GenericPool<FloatingText> floatingTextPool;
```

**적용 대상**:
- [x] BulletBase 및 하위 클래스 (모든 발사체)
- [x] EnemyShip (모든 적)
- [x] FloatingText (점수, 레벨업 텍스트)
- [ ] 파티클 시스템 (선택적)

**검증 방법**:
- Unity Profiler로 Instantiate/Destroy 호출 수 확인
- GC Alloc 감소 확인
- 모바일 FPS 개선 확인

---

## 2. 아키텍처 개선

### 2.1 싱글톤 의존성 완화

**현재 문제**:
- 모든 매니저가 `MonoSingleton<T>` 상속
- 매니저 간 강한 결합 (예: `GameManager.Instance`, `LevelManager.Instance`)
- 단위 테스트 작성 불가능
- 씬 전환 시 의존성 관리 어려움

**영향도**: 🟡 중 (유지보수성)
**우선순위**: P2 (중기)
**예상 소요**: 1주

**현재 싱글톤 사용 매니저**:
- GameManager
- InputManager
- LevelManager
- UpgradeManager
- ScoreManager
- TimeRecordManager
- EnemySpawner
- ObjectSpawner
- SoundManager
- UiManager

**해결 방안 (옵션)**:

**옵션 A: ScriptableObject 기반 아키텍처** (권장)
```csharp
// 이벤트 채널 패턴
[CreateAssetMenu]
public class GameEventChannel : ScriptableObject
{
    private event Action listeners;
    public void RaiseEvent() => listeners?.Invoke();
    public void AddListener(Action listener) => listeners += listener;
}

// 매니저를 일반 클래스로 변경
public class LevelManager : MonoBehaviour
{
    [SerializeField] GameEventChannel onLevelUp;
    // Instance 제거, 이벤트로 통신
}
```

**옵션 B: 의존성 주입 (Zenject/VContainer)**
- 외부 라이브러리 도입
- 학습 곡선 있음

**옵션 C: Service Locator 패턴**
- 싱글톤보다 유연
- 테스트 가능

**권장**: 옵션 A (ScriptableObject 기반)
- Unity 네이티브 방식
- 추가 라이브러리 불필요
- 씬 간 데이터 공유 용이
- Unite 강연에서 권장하는 아키텍처

**참고**:
- [Unite Austin 2017 - Game Architecture with Scriptable Objects](https://www.youtube.com/watch?v=raQ3iHhE_Kk)

---

### 2.2 레이어 및 태그 중앙 관리

**현재 문제**:
- 레이어 번호가 하드코딩됨 (`m_Bits: 128` = 레이어 7)
- 태그 문자열이 코드에 분산 (`"Player"`, `"Enemy"` 등)
- 레이어 변경 시 여러 파일 수정 필요
- 오타 발생 가능

**영향도**: 🟡 중 (유지보수성)
**우선순위**: P2 (중기)
**예상 소요**: 2-3시간

**해결 방안**:
```csharp
// LayerMasks.cs (새 파일)
public static class Layers
{
    public const int Player = 7;
    public const int Enemy = 8;
    public const int PlayerProjectile = 9;
    public const int EnemyProjectile = 10;

    public static class Masks
    {
        public static readonly LayerMask Player = 1 << Layers.Player;
        public static readonly LayerMask Enemy = 1 << Layers.Enemy;
        public static readonly LayerMask AllProjectiles =
            (1 << Layers.PlayerProjectile) | (1 << Layers.EnemyProjectile);
    }
}

// Tags.cs (새 파일)
public static class Tags
{
    public const string Player = "Player";
    public const string Enemy = "Enemy";
    public const string Projectile = "Projectile";
}

// 사용 예시
targetLayer = Layers.Masks.Player;  // 기존: m_Bits: 128
if (other.CompareTag(Tags.Enemy))   // 기존: "Enemy"
```

**적용 범위**:
- ShooterBase (targetLayer)
- FindTarget (targetLayer)
- BulletBase (충돌 감지)
- Impactable (충돌 감지)
- 모든 태그 사용 코드

---

### 2.3 매니저 통합 검토

**현재 문제**:
- ScoreManager와 TimeRecordManager가 역할 유사
- 단순 데이터 저장/조회 기능
- 별도 싱글톤으로 분리할 필요성 낮음

**영향도**: 🟢 낮음 (단순화)
**우선순위**: P3 (장기)
**예상 소요**: 1-2시간

**해결 방안**:
```csharp
// GameStatsManager로 통합
public class GameStatsManager : MonoSingleton<GameStatsManager>
{
    public int Score { get; private set; }
    public float PlayTime { get; private set; }
    public int WaveNumber { get; private set; }
    public int KillCount { get; private set; }

    public void AddScore(int amount) { /* ... */ }
    public void UpdatePlayTime(float delta) { /* ... */ }
}
```

**통합 후보**:
- ScoreManager + TimeRecordManager → GameStatsManager
- (검토 필요) UpgradeManager + LevelManager 통합 가능성

---

### 2.4 스폰 시스템 동시성 문제 ⚠️

**현재 문제**:
- 적 스폰 시 경고 시스템으로 인해 실제 스폰이 `warningDelay`(1초) 지연됨
- `SpawnEnemy()` → `StartCoroutine(SpawnEnemyDelayed())` 방식으로 구현
- 이벤트/보스 스폰 시 for 루프로 여러 적을 스폰하는데, 각 스폰이 독립적인 코루틴으로 실행됨
- 게임 상태 변경(예: 보스 격파)이 발생해도 **이미 시작된 코루틴들은 계속 실행**되어 의도하지 않은 스폰 발생

**영향도**: 🟡 중 (게임플레이 로직)
**우선순위**: P1 (단기)
**예상 소요**: 해결 완료 (현재 우회 방안 적용)

**현재 우회 방안** (적용 완료):
```csharp
// TimeBasedSpawnManager.cs

// 1. for 루프에서 게임 상태 확인
for (int i = 0; i < spawnCount; i++)
{
    if (GameManager.Instance.GameState == GameState.GameClear)
    {
        break; // 새로운 스폰 중단
    }
    SpawnEnemy(...);
}

// 2. 지연 코루틴에서도 게임 상태 확인
private IEnumerator SpawnEnemyDelayed(...)
{
    yield return new WaitForSeconds(delay);

    // 이미 시작된 코루틴도 실행 전 상태 확인
    if (GameManager.Instance.GameState == GameState.GameClear)
    {
        yield break; // 지연 스폰 취소
    }

    SpawnEnemyImmediate(...);
}
```

**근본적 해결 방안** (장기 개선):

**옵션 A: 코루틴 추적 및 일괄 중단**
```csharp
private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();

private void SpawnEnemy(...)
{
    var coroutine = StartCoroutine(SpawnEnemyDelayed(...));
    activeSpawnCoroutines.Add(coroutine);
}

public void CancelAllPendingSpawns()
{
    foreach (var coroutine in activeSpawnCoroutines)
    {
        StopCoroutine(coroutine);
    }
    activeSpawnCoroutines.Clear();
}
```

**옵션 B: CancellationToken 패턴**
```csharp
private CancellationTokenSource spawnCancellation = new CancellationTokenSource();

private IEnumerator SpawnEnemyDelayed(CancellationToken token)
{
    yield return new WaitForSeconds(delay);

    if (token.IsCancellationRequested)
        yield break;

    SpawnEnemyImmediate(...);
}

public void OnBossDefeated()
{
    spawnCancellation.Cancel(); // 모든 대기 중인 스폰 취소
    spawnCancellation = new CancellationTokenSource();
}
```

**옵션 C: 이벤트 기반 스폰 시스템**
- 코루틴 대신 Update()에서 타이머 관리
- 게임 상태 변경 시 즉시 타이머 클리어 가능
- 동시성 문제 원천 차단

**권장**: 옵션 C (이벤트 기반)
- 코루틴 의존성 제거
- 상태 관리 명확화
- 다른 시스템에도 응용 가능

**주의 사항**:
- 코루틴 기반 지연 처리는 동시성 문제에 취약
- 게임 상태 변경 시 실행 중인 코루틴들을 항상 고려할 것
- 유사한 패턴(경고 → 지연 → 실행)을 사용하는 다른 시스템에도 동일한 이슈 존재 가능

---

## 3. 코드 품질

### 3.1 단위 테스트 작성

**현재 문제**:
- 단위 테스트가 전혀 없음
- 리팩토링 시 버그 발생 위험
- 웨이브 생성 로직 검증 어려움

**영향도**: 🟡 중 (안정성)
**우선순위**: P2 (중기)
**예상 소요**: 1주

**테스트 우선순위**:

**Phase 1: 순수 로직 테스트** (Unity 독립적)
```csharp
[Test]
public void WaveBudget_CalculatedCorrectly()
{
    Assert.AreEqual(150, WaveBudgetCalculator.CalculateBudget(1));
    Assert.AreEqual(400, WaveBudgetCalculator.CalculateBudget(6));
    Assert.AreEqual(600, WaveBudgetCalculator.CalculateBudget(10));
}

[Test]
public void EnemyCost_ReturnsValidCost()
{
    Assert.AreEqual(20, EnemyCostData.GetCost("Enemy_light_child"));
    Assert.AreEqual(400, EnemyCostData.GetCost("Enemy_heavy_Gunship"));
}

[Test]
public void WaveType_DeterminedCorrectly()
{
    Assert.AreEqual(WaveType.Procedural, WaveBudgetCalculator.GetWaveType(6));
    Assert.AreEqual(WaveType.Manual, WaveBudgetCalculator.GetWaveType(10));
}
```

**Phase 2: 통합 테스트** (Unity PlayMode)
```csharp
[UnityTest]
public IEnumerator ProceduralWave_SpawnsEnemies()
{
    // 웨이브 생성 → 적 스폰 → 격파 → 웨이브 완료 전 과정 테스트
}
```

**Phase 3: PlayerStats 테스트**
```csharp
[Test]
public void PlayerStats_UpgradeAppliedCorrectly()
{
    var stats = new PlayerStats();
    stats.UpgradeStat(StatType.MaxShield, 50);
    Assert.AreEqual(150, stats.MaxShield);
}
```

**도구**:
- Unity Test Framework (내장)
- NUnit (Unity 기본 제공)

---

### 3.2 매직 넘버 제거

**현재 문제**:
- 하드코딩된 상수들 (예: 레벨업 공식 `level * 1100`)
- 밸런싱 조정 시 코드 수정 필요

**영향도**: 🟢 낮음 (가독성)
**우선순위**: P3 (장기)
**예상 소요**: 1-2시간

**해결 방안**:
```csharp
// GameConstants.cs
public static class GameConstants
{
    // 레벨 시스템
    public const int ExpPerLevel = 1100;

    // 웨이브 시스템
    public const int WaveBudgetBase = 100;
    public const int WaveBudgetGrowth = 50;

    // 플레이어
    public const float DefaultMoveSpeed = 10f;
    public const float DefaultRotateSpeed = 180f;
}
```

---

## 4. 문서화

### 4.1 API 레퍼런스 작성

**현재 상태**:
- 디자인 문서는 충실함
- 코드 API 레퍼런스 없음
- 주요 클래스의 public 메서드 설명 부족

**영향도**: 🟢 낮음 (협업)
**우선순위**: P3 (장기)
**예상 소요**: 2-3일

**대상**:
- PlayerStats (스탯 시스템의 핵심)
- UpgradeManager (업그레이드 적용 로직)
- ProceduralWaveGenerator (웨이브 생성)
- Damageable (HP 시스템)

**형식**: XML 주석 + Doxygen/Sandcastle

---

## 5. 기타 개선 사항

### 5.1 에러 핸들링 강화

**현재 문제**:
- null 참조 예외 잠재적 위험
- 프리팹 로드 실패 시 처리 미흡

**해결 방안**:
```csharp
// null 체크 강화
if (enemyPrefab == null)
{
    Debug.LogError($"[EnemySpawner] Enemy prefab not assigned!");
    return;
}

// 프리팹 로드 실패 시 기본값
EnemyShip LoadEnemySafe(string name)
{
    var prefab = LoadEnemy(name);
    if (prefab == null)
    {
        Debug.LogWarning($"[ProceduralWave] Failed to load {name}, using default");
        return defaultEnemyPrefab;
    }
    return prefab;
}
```

### 5.2 로깅 시스템 개선

**현재 상태**:
- Debug.Log 직접 호출
- 로그 레벨 제어 불가

**해결 방안**:
```csharp
// Logger.cs
public static class Logger
{
    public enum Level { Debug, Info, Warning, Error }
    public static Level CurrentLevel = Level.Info;

    public static void Log(string message, Level level = Level.Info)
    {
        if (level >= CurrentLevel)
            Debug.Log($"[{level}] {message}");
    }
}

// 사용
Logger.Log("Wave 6 started", Logger.Level.Debug);  // 개발 중에만 출력
Logger.Log("Boss defeated!", Logger.Level.Info);   // 항상 출력
```

---

## 우선순위 요약

### P0 (최우선 - 1-2주 내)
- [ ] 오브젝트 풀링 시스템 (성능 필수)

### P1 (단기 - 1개월 내)
- [ ] 레이어/태그 중앙 관리
- [ ] 에러 핸들링 강화

### P2 (중기 - 2-3개월)
- [ ] 싱글톤 의존성 완화 (ScriptableObject 기반)
- [ ] 단위 테스트 작성

### P3 (장기 - 6개월)
- [ ] 매니저 통합 검토
- [ ] 매직 넘버 제거
- [ ] API 레퍼런스 작성
- [ ] 로깅 시스템 개선

---

## 참고

- [Roadmap.md](Roadmap.md) - 개발 로드맵
- [Architecture.md](Architecture.md) - 현재 아키텍처
- [Guidelines/CodeStyle.md](Guidelines/CodeStyle.md) - 코드 스타일 가이드
