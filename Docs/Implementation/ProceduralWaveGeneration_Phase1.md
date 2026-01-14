# 절차적 웨이브 생성 시스템 구현 계획 (1단계)

> Nova Drift 스타일 예산 기반 절차적 웨이브 생성 시스템 - 1단계: 기본 예산 시스템 + SmallSwarm 패턴

## 개요

현재 수작업 웨이브 시스템을 절차적 생성 시스템으로 확장합니다.

**목표**:
- 웨이브 1-9, 11-19, 21-29: 절차적 생성
- 웨이브 10, 20, 30: 보스 (상단 고정 스폰)
- 매 플레이마다 다른 적 조합

**1단계 범위**:
- 예산 계산 시스템
- 적 비용 테이블
- SmallSwarm 패턴 (1개)
- 보스 웨이브 단순 스폰

---

## 핵심 파일

### 새로 생성할 파일

```
Assets/Scripts/Wave/
├── EnemyCostData.cs          (적 비용 테이블, 정적 클래스)
├── WaveData.cs                (WaveConfig, GeneratedWaveData, WaveBudgetCalculator)
├── ProceduralWaveGenerator.cs (절차적 생성 엔진, SmallSwarm 패턴)
└── WavePreset.cs              (ScriptableObject, 추후 확장용 - 현재 미사용)

보스 웨이브는 코드에서 직접 생성 (GenerateBossWave 메서드)
```

### 수정할 파일

- `Assets/Scripts/EnemySpawner.cs` - 웨이브 시스템 통합 (기존 코드 유지, 병행 추가)
- `Assets/Scripts/ObjectSpawner.cs` - `SpawnObject()` 오버로드 메서드 추가

---

## 데이터 구조

### 1. 적 비용 테이블

**파일**: `Assets/Scripts/Wave/EnemyCostData.cs`

```csharp
public enum EnemyTier { Light, LightPlus, Mid, MidPlus, Heavy, Boss }

public static class EnemyCostData
{
    // 적 프리팹 이름 -> 비용 매핑
    private static Dictionary<string, int> enemyCosts = new()
    {
        // Light (20-30)
        { "Enemy_light_child", 20 },
        { "Enemy_light_kido", 20 },
        { "Enemy_light_thunder", 25 },

        // Light+ (40)
        { "Enemy_light_shield", 40 },

        // Mid (80-150)
        { "Enemy_mid_Ghost", 80 },
        { "Enemy_mid_Hornet", 90 },
        { "Enemy_mid_Knight", 120 },
        { "Enemy_mid_sniper", 130 },
        { "Enemy_mid_tank", 140 },
        { "Enemy_mid_Spiral", 150 },
        { "Enemy_mid_master", 100 },

        // Heavy (300-400)
        { "Enemy_heavy_mother", 350 },
        { "Enemy_heavy_Gunship", 400 },

        // Boss
        { "Enemy_Boss", 1500 }
    };

    public static int GetCost(string prefabName);
    public static int GetCost(GameObject prefab);
    public static EnemyTier GetTier(string prefabName);
    public static List<string> GetEnemiesByTier(EnemyTier tier);
    public static List<string> GetEnemiesByCost(int maxCost);
}
```

**비용 설정 원칙**:
- EnemyShip.point 값 기반
- Light: 20-30, Mid: 80-150, Heavy: 300-400
- 특수 능력(드론, 다중 무기) 가중치 적용

---

### 2. 웨이브 데이터

**파일**: `Assets/Scripts/Wave/WaveData.cs`

```csharp
public enum WaveType { Manual, Procedural }

// 웨이브 설정
public class WaveConfig
{
    public int waveNumber;
    public WaveType waveType;
    public int budget;
    // preset 필드는 추후 확장용으로 제거됨 (현재 보스는 코드로 생성)
}

// 생성된 웨이브 정보
public class GeneratedWaveData
{
    public int waveNumber;
    public int budgetUsed;
    public int budgetTotal;
    public string patternName;
    public SpawnInfo[] spawnInfos;
}

// 예산 계산 유틸리티
public static class WaveBudgetCalculator
{
    private const int BasePoints = 100;
    private const int GrowthRate = 50;

    // 예산 = 100 + (웨이브번호 × 50)
    public static int CalculateBudget(int waveNumber);

    // 웨이브 타입 결정
    public static WaveType GetWaveType(int waveNumber)
    {
        // 보스 웨이브만 Manual, 나머지는 절차적 생성
        if (waveNumber == 10 || waveNumber == 20 || waveNumber == 30)
            return WaveType.Manual;
        return WaveType.Procedural;
    }
}
```

**예산 공식**:
```
웨이브 1:  150  ← 절차적 생성
웨이브 5:  350  ← 절차적 생성
웨이브 10: 600  ← 보스 (상단 고정 스폰)
웨이브 20: 1100 ← 보스
웨이브 30: 1600 ← 보스
```

---

### 3. 절차적 생성 엔진

**파일**: `Assets/Scripts/Wave/ProceduralWaveGenerator.cs`

```csharp
public class ProceduralWaveGenerator
{
    private static Dictionary<string, EnemyShip> enemyPrefabRegistry = new();

    // 초기화 (EnemySpawner에서 호출)
    public static void Initialize(EnemyShip[] enemyPrefabs)
    {
        // Inspector에서 받은 EnemyShip 배열로 레지스트리 초기화
        // 타입 안정성 보장: EnemyShip만 등록 가능
    }

    // 메인 생성 메서드
    public static GeneratedWaveData GenerateWave(int waveNumber, int budget)
    {
        // 1단계: SmallSwarm 패턴만
        return GenerateSmallSwarmWave(waveNumber, budget);
    }

    // SmallSwarm: 다수의 약한 적
    private static GeneratedWaveData GenerateSmallSwarmWave(int waveNumber, int budget)
    {
        // 조건: 예산의 70% 이상을 Light 등급에 사용, 최소 5마리
        // 1-2개 스폰 그룹 생성
        // SpawnInfo[] 반환 (EnemyShip.gameObject 사용)
    }

    private static EnemyShip LoadEnemyPrefab(string prefabName)
    {
        // 레지스트리에서 EnemyShip 반환
        // 타입 안정성 보장
    }
}
```

**SmallSwarm 패턴 알고리즘**:
1. Light 등급 적 목록 가져오기
2. 랜덤으로 1개 선택
3. 예산의 60% 사용 (첫 그룹, 최소 5마리)
4. 나머지 예산으로 두 번째 그룹 (선택적)
5. SpawnInfo[] 반환

**예시**:
```
웨이브 6 (예산 400):
그룹 1: child × 14 (280포인트) @ 0초, Random, 간격 0.3초
그룹 2: thunder × 4 (100포인트) @ 4초, Random, 간격 0.5초
총 사용: 380/400 (95%)
```

---

### 4. WavePreset (ScriptableObject) - 현재 미사용

**파일**: `Assets/Scripts/Wave/WavePreset.cs`

```csharp
[CreateAssetMenu(fileName = "Wave_", menuName = "프로젝트 커스텀/Wave/Wave Preset")]
public class WavePreset : ScriptableObject
{
    [Header("Wave Info")]
    public int waveNumber;

    [TextArea(3, 5)]
    public string description;

    [Header("Spawn Configuration")]
    public SpawnInfo[] spawnInfos;

    public SpawnInfo[] GetSpawnInfos()
    {
        // 깊은 복사 반환 (원본 보존)
    }
}
```

**현재 상태**:
- ⚠️ **Phase 1에서는 미사용** (보스 웨이브가 코드로 생성됨)
- 파일은 유지 (추후 복잡한 보스 패턴 구현 시 재사용 가능)
- 향후 확장 시: 보스 + 호위병, 다단계 보스 등에 활용 가능

---

## EnemySpawner 통합

### 수정 전략

**호환성 유지**:
- 기존 timeSpawnInfoList 시스템 유지 (Legacy System)
- 새로운 웨이브 시스템 병행 추가
- `useWaveSystem` 토글로 전환 가능
- 문제 발생 시 즉시 기존 시스템으로 롤백 가능

### 주요 변경사항

```csharp
public class EnemySpawner : MonoSingleton<EnemySpawner>
{
    [Header("Wave System (New)")]
    [SerializeField] bool useWaveSystem = true;
    [SerializeField] int startWaveNumber = 1;
    [SerializeField] int maxWaveNumber = 30;
    [SerializeField] EnemyShip[] enemyPrefabs;  // 타입 안정성!
    [SerializeField] EnemyShip boss10Prefab;
    [SerializeField] EnemyShip boss20Prefab;
    [SerializeField] EnemyShip boss30Prefab;

    [Header("Legacy System (Old)")]
    [SerializeField] List<SpawnInfo> timeSpawnInfoList;
    [SerializeField] List<SpawnInfo> endlessSpawnInfoList;

    private int currentWaveNumber = 0;
    private bool isWaveActive = false;
    private List<GameObject> currentWaveEnemies = new();

    void Start()
    {
        if (useWaveSystem) InitWaveSystem();
        else InitLegacySystem();
    }

    void Update()
    {
        if (useWaveSystem) UpdateWaveSystem();
        else UpdateLegacySystem();
    }

    // 웨이브 시스템
    void StartWave(int waveNumber)
    {
        WaveType waveType = WaveBudgetCalculator.GetWaveType(waveNumber);

        if (waveType == WaveType.Manual)
        {
            // 보스 웨이브: 상단 고정 스폰
            spawnInfos = GenerateBossWave();
        }
        else
        {
            // 절차적 생성
            spawnInfos = GenerateProceduralWave(waveNumber);
        }

        ExecuteSpawnInfos(spawnInfos);
    }

    private SpawnInfo[] GenerateBossWave(int waveNumber)
    {
        // 웨이브 번호에 따라 적절한 보스 선택
        EnemyShip selectedBoss = waveNumber switch
        {
            10 => boss10Prefab,
            20 => boss20Prefab,
            30 => boss30Prefab,
            _ => null
        };

        return new SpawnInfo[]
        {
            new SpawnInfo
            {
                spawnPrefab = selectedBoss.gameObject,  // GameObject로 변환
                spawnTime = 0f,
                spawnSide = Edge.Up,
                count = 1,
                spawnInterval = 0f
            }
        };
    }

    void ExecuteSpawnInfos(SpawnInfo[] infos)
    {
        foreach (info in infos)
            StartCoroutine(SpawnWithDelay(info));
    }

    IEnumerator SpawnWithDelay(SpawnInfo info)
    {
        yield return new WaitForSeconds(info.spawnTime);

        for (int i = 0; i < info.count; i++)
        {
            GameObject enemy = ObjectSpawner.Instance.SpawnObject(
                info.spawnPrefab, info.spawnSide
            );
            currentWaveEnemies.Add(enemy);

            if (i < info.count - 1)
                yield return new WaitForSeconds(info.spawnInterval);
        }
    }

    bool AreAllWaveEnemiesDead()
    {
        currentWaveEnemies.RemoveAll(e => e == null);
        return currentWaveEnemies.Count == 0;
    }

    void OnWaveComplete()
    {
        isWaveActive = false;
        currentWaveNumber++;
        StartCoroutine(StartWaveDelayed(currentWaveNumber, 2f));
    }
}
```

---

## ObjectSpawner 수정

### 추가 메서드

```csharp
// ObjectSpawner.cs에 추가 (public 오버로드)
public GameObject SpawnObject(GameObject objectPrefab, Edge spawnSide)
{
    if (objectPrefab == null)
    {
        Debug.Log("objectPrefab is null");
        return null;
    }

    var (pos, rot) = GetSpawnPointAndRotation(objectPrefab, spawnSide);
    GameObject go = Instantiate(objectPrefab);
    go.transform.position = pos;
    go.transform.rotation = rot;
    spawned.Add(go);

    return go;
}
```

**주의**: 기존 private SpawnObject() 메서드와 시그니처가 다르므로 오버로드로 추가

---

## 구현 순서

### Phase 1: 데이터 구조 구축 (1-2시간)

**작업**:
1. `Assets/Scripts/Wave/` 폴더 생성
2. `EnemyCostData.cs` 작성 (14개 적 비용 설정)
3. `WaveData.cs` 작성 (WaveConfig, GeneratedWaveData, WaveBudgetCalculator)
4. `WavePreset.cs` 작성 (ScriptableObject)

**검증**:
```csharp
// Unity Console에서 테스트
Debug.Log(EnemyCostData.GetCost("Enemy_light_child")); // 20
Debug.Log(WaveBudgetCalculator.CalculateBudget(6)); // 400
Debug.Log(WaveBudgetCalculator.GetWaveType(6)); // Procedural
Debug.Log(WaveBudgetCalculator.GetWaveType(10)); // Manual
```

**성공 기준**:
- 14개 적의 비용이 모두 정확히 반환
- 예산 계산이 공식과 일치
- 웨이브 타입 분기가 정확

---

### Phase 2: 절차적 생성 엔진 (2-3시간)

**작업**:
1. `ProceduralWaveGenerator.cs` 작성
2. `GenerateSmallSwarmWave()` 메서드 구현
3. 프리팹 로딩 로직 구현 (Resources.Load + 캐싱)

**검증**:
```csharp
// 테스트 스크립트
int budget = WaveBudgetCalculator.CalculateBudget(6); // 400
GeneratedWaveData wave = ProceduralWaveGenerator.GenerateWave(6, budget);

Debug.Log($"Wave 6 - Budget: {wave.budgetUsed}/{wave.budgetTotal}");
Debug.Log($"Pattern: {wave.patternName}");
Debug.Log($"Spawn Groups: {wave.spawnInfos.Length}");

foreach (var info in wave.spawnInfos)
{
    Debug.Log($"  - {info.spawnPrefab.name} x{info.count} @ {info.spawnTime}s");
}
```

**성공 기준**:
- 예산 사용률 70-95%
- Light 등급 적이 70% 이상
- 최소 5마리 이상 스폰
- 1-2개 SpawnInfo 그룹
- 프리팹이 null 아님

---

### Phase 3: EnemySpawner 통합 (2-3시간)

**작업**:
1. EnemySpawner에 웨이브 시스템 추가 (기존 코드 유지)
2. `useWaveSystem` 토글 추가
3. 웨이브 완료 감지 로직
4. ObjectSpawner에 `SpawnObject()` 오버로드 추가

**검증**:
1. Inspector에서 `useWaveSystem = true` 설정
2. Enemy Prefabs 배열에 14개 적 프리팹 할당
3. 게임 실행 → 웨이브 1 시작 확인 (절차적 생성)
4. 모든 적 격파 → 웨이브 2 전환 확인
5. Console 로그로 예산 사용 확인

**성공 기준**:
- 웨이브 1부터 절차적 생성으로 SmallSwarm 패턴 적용
- 웨이브 완료 감지 정상 작동
- 웨이브 간 전환 2초 간격
- Console에 예산 사용 로그 출력

---

### Phase 4: 보스 웨이브 단순화 ✅ 완료

**변경 사항**:
- ScriptableObject 프리셋 대신 **상단 고정 스폰** 방식으로 단순화
- 보스 웨이브 (10, 20, 30)는 `GenerateBossWave()` 메서드로 처리
- Boss 프리팹을 Edge.Up에 1기만 스폰
- 추후 보스 패턴 확장 시 ScriptableObject 재도입 가능

**구현 내용**:
```csharp
private SpawnInfo[] GenerateBossWave(int waveNumber)
{
    // 웨이브 번호에 따라 보스 선택 (EnemyShip 타입)
    EnemyShip selectedBoss = null;

    switch (waveNumber)
    {
        case 10: selectedBoss = boss10Prefab; break;
        case 20: selectedBoss = boss20Prefab; break;
        case 30: selectedBoss = boss30Prefab; break;
    }

    if (selectedBoss == null)
    {
        Debug.LogError($"[EnemySpawner] Boss prefab for wave {waveNumber} is not assigned!");
        return new SpawnInfo[0];
    }

    return new SpawnInfo[]
    {
        new SpawnInfo
        {
            spawnPrefab = selectedBoss.gameObject,  // GameObject로 변환
            spawnTime = 0f,
            spawnSide = Edge.Up,
            count = 1,
            spawnInterval = 0f
        }
    };
}
```

**타입 안정성 개선**:
- `EnemyShip` 타입 사용으로 컴파일 타임에 오류 방지
- Inspector에서 실수로 다른 프리팹을 할당하는 것을 Unity가 차단
- 코드 의도가 명확: "이것은 적 프리팹이다"

**Unity Editor 설정 필요**:
- EnemySpawner Inspector에서 각 보스 웨이브에 맞는 프리팹 할당
  - `Boss 10 Prefab`: 웨이브 10 보스
  - `Boss 20 Prefab`: 웨이브 20 보스 (더 강함)
  - `Boss 30 Prefab`: 웨이브 30 최종 보스

**성공 기준**:
- 웨이브 10, 20, 30에서 보스가 상단에서 등장
- 별도 프리셋 파일 불필요
- 코드가 단순하고 유지보수 용이

---

### Phase 5: Unity Editor 설정

**작업**:
1. EnemySpawner GameObject의 Inspector 설정
   - `Use Wave System` ☑ 체크
   - `Enemy Prefabs` 배열에 14개 적 프리팹 할당 (EnemyShip 타입)
     - **중요**: 프리팹 자체가 아니라 프리팹의 EnemyShip 컴포넌트를 드래그
     - Unity가 자동으로 EnemyShip 컴포넌트가 있는 프리팹만 허용
   - `Boss 10 Prefab`: 웨이브 10 보스의 EnemyShip 컴포넌트 할당
   - `Boss 20 Prefab`: 웨이브 20 보스의 EnemyShip 컴포넌트 할당 (더 강한 보스)
   - `Boss 30 Prefab`: 웨이브 30 최종 보스의 EnemyShip 컴포넌트 할당
   - `Start Wave Number`: 1
   - `Max Wave Number`: 30
   - `Wave Transition Delay`: 2

**프리팹 목록 (14개)**:
```
Light (4개):
- Enemy_light_child
- Enemy_light_kido
- Enemy_light_thunder
- Enemy_light_shield

Mid (7개):
- Enemy_mid_Ghost
- Enemy_mid_Hornet
- Enemy_mid_Knight
- Enemy_mid_sniper
- Enemy_mid_tank
- Enemy_mid_Spiral
- Enemy_mid_master

Heavy (2개):
- Enemy_heavy_mother
- Enemy_heavy_Gunship

Boss (3개, 별도 필드에 할당):
- Boss 10 Prefab: Enemy_Boss 또는 첫 번째 보스
- Boss 20 Prefab: 두 번째 보스 (Enemy_Boss보다 강함)
- Boss 30 Prefab: 최종 보스
```

**중요 - 타입 안정성**:
- `EnemyShip[]` 타입을 사용하여 실수로 적이 아닌 프리팹을 등록하는 것을 방지
- Unity Inspector에서 EnemyShip 컴포넌트가 있는 프리팹만 드래그 가능
- 프리팹 자체를 드래그하면 Unity가 자동으로 EnemyShip 컴포넌트를 찾아 할당

**성공 기준**:
- Inspector에서 모든 프리팹이 null 없이 할당됨
- Use Wave System이 활성화됨

---

### Phase 6: 통합 테스트 및 튜닝 (1-2시간)

**작업**:
1. 웨이브 1-10 풀 플레이 테스트
2. 예산 밸런싱 조정
3. SmallSwarm 패턴 튜닝 (스폰 간격, Edge 선택)
4. 버그 수정

**테스트 시나리오**:

1. **웨이브 1-10 플레이**:
   - 게임 시작 → 웨이브 1부터 절차적 생성 확인
   - Console 로그로 예산 사용 확인
   - 웨이브 1-9가 매번 다른 구성인지 확인 (재시작 필요)
   - 웨이브 10 보스가 상단에서 등장 확인

2. **예산 밸런싱 검증**:
   - 웨이브 6-9를 각각 5회씩 생성 (게임 재시작)
   - Console 로그에서 예산 사용률 수집
   - 평균 사용률 85-95% 확인
   - Light 등급 비율 70% 이상 확인

3. **롤백 테스트**:
   - `useWaveSystem = false` 설정
   - 기존 timeSpawnInfoList 시스템으로 실행
   - 정상 동작 확인
   - `useWaveSystem = true` 재설정

**성공 기준**:
- [ ] 웨이브 1-9가 절차적 생성으로 작동
- [ ] 매번 다른 적 구성 (재시작 시)
- [ ] 웨이브 10 보스 정상 등장
- [ ] 적 수가 과도하지 않음 (성능)
- [ ] 웨이브 완료 감지 오류 없음
- [ ] Console에 예산 사용 로그 정상 출력
- [ ] 10분 플레이 시 크래시 없음

---

## 디버깅 및 로깅

### Console 로그 출력

**웨이브 시작**:
```
[Wave 6] Starting Procedural Wave - Budget: 400
[Wave 6] SmallSwarm - Budget: 380/400 (95%), Groups: 2
```

**스폰 그룹 상세**:
```
[Wave 6 - Group 0] Enemy_light_child x14 @ 0s (Random)
[Wave 6 - Group 1] Enemy_light_thunder x4 @ 4s (Random)
```

**웨이브 완료**:
```
[Wave 6] Complete! (25.3s)
[Wave 7] Starting in 2 seconds...
```

---

## 리스크 관리

### 예상 리스크 및 대응

| 리스크 | 확률 | 영향 | 대응 방안 |
|--------|------|------|----------|
| 프리팹 로드 실패 | 중 | 높음 | 프리팹을 Resources/Prefabs/Enemys/로 이동, 로드 실패 시 기본 프리팹 사용 |
| 예산 초과 | 높음 | 중 | 안전 마진 10% 추가, 초과 시 마지막 그룹 제거 |
| 웨이브 완료 미감지 | 중 | 높음 | 타임아웃 추가 (90초), 강제 완료 로직 |
| 성능 문제 (적 과다) | 낮음 | 중 | 웨이브당 최대 20마리 제한 |

### 성능 최적화

**프리팹 캐싱**:
```csharp
private static Dictionary<string, GameObject> prefabCache = new();

// 첫 로드 시 캐싱, 이후 캐시에서 반환
GameObject prefab = prefabCache.ContainsKey(name)
    ? prefabCache[name]
    : Resources.Load<GameObject>($"Prefabs/Enemys/{name}");
```

**적 수 제한**:
```csharp
private const int MAX_ENEMIES_PER_WAVE = 20;

// 생성 후 검증
if (totalEnemyCount > MAX_ENEMIES_PER_WAVE)
{
    // 마지막 그룹 제거 또는 count 감소
}
```

---

## 롤백 계획

문제 발생 시:
1. Inspector에서 `useWaveSystem = false` 설정
2. 기존 timeSpawnInfoList 시스템으로 즉시 복귀
3. 절차적 생성 코드 디버깅
4. 수정 후 다시 전환

---

## 검증 방법

### 자동화 테스트 (선택적)

```csharp
[Test]
public void TestEnemyCost()
{
    Assert.AreEqual(20, EnemyCostData.GetCost("Enemy_light_child"));
    Assert.AreEqual(400, EnemyCostData.GetCost("Enemy_heavy_Gunship"));
}

[Test]
public void TestBudgetCalculation()
{
    Assert.AreEqual(150, WaveBudgetCalculator.CalculateBudget(1));
    Assert.AreEqual(400, WaveBudgetCalculator.CalculateBudget(6));
    Assert.AreEqual(600, WaveBudgetCalculator.CalculateBudget(10));
}

[Test]
public void TestWaveType()
{
    Assert.AreEqual(WaveType.Manual, WaveBudgetCalculator.GetWaveType(1));
    Assert.AreEqual(WaveType.Procedural, WaveBudgetCalculator.GetWaveType(6));
    Assert.AreEqual(WaveType.Manual, WaveBudgetCalculator.GetWaveType(10));
}
```

### 수동 플레이 테스트

**체크리스트**:
- [ ] 웨이브 1-9가 절차적 생성으로 작동
- [ ] 매 플레이마다 웨이브 1-9 구성이 다름
- [ ] 웨이브 10 보스가 등장
- [ ] 웨이브 완료 감지가 정확함
- [ ] 적 수가 적절함 (5-15마리)
- [ ] 예산 사용률이 70-95%
- [ ] 성능 문제 없음 (60fps 유지)

---

## 다음 단계 (2단계)

1단계 완료 후 2단계에서 추가:

**추가 패턴 (5개)**:
- MidSquadron (중형 편대)
- EliteEscort (엘리트 + 호위)
- Encirclement (포위 공격)
- WaveAssault (파동 공격)

**패턴 선택 시스템**:
- 웨이브 단계별 가중치
- 중복 방지 (최근 5개 웨이브 기록)

**난이도 조절 (3단계)**:
- 플레이어 성능 추적
- 예산 배율 조정 (0.7 ~ 1.3)

---

## 예상 소요 시간

| Phase | 작업 내용 | 소요 시간 | 상태 |
|-------|----------|----------|------|
| 1 | 데이터 구조 | 1-2시간 | ✅ 완료 |
| 2 | 절차적 생성 엔진 | 2-3시간 | ✅ 완료 |
| 3 | EnemySpawner 통합 | 2-3시간 | ✅ 완료 |
| 4 | 보스 웨이브 단순화 | 30분 | ✅ 완료 |
| 5 | Unity Editor 설정 | 30분 | 대기 중 |
| 6 | 통합 테스트 | 1-2시간 | 대기 중 |
| **총합** | | **7-11시간** | Phase 1-4 완료 |

---

## 참고 문서

- [ProceduralWaveGeneration.md](../Design/ProceduralWaveGeneration.md) - 기획 문서
- [Architecture.md](../Architecture.md) - 전체 아키텍처
- [EnemyList.md](../Design/EnemyList.md) - 적 종류 및 특성
