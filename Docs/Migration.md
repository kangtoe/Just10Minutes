# 프로젝트 마이그레이션 노트

> 기존 스페이스 슈터 프로젝트를 AutoSpaceShooter로 전환하는 과정의 변경사항 및 작업 내역

## 프로젝트 현황

### 기존 프로젝트 구조

```
Assets/Scripts/
├── Managers (싱글톤 패턴)
│   ├── GameManager - 게임 상태 관리
│   ├── InputManager - 입력 처리
│   ├── LevelManager - 레벨/경험치 시스템 ✅
│   ├── UpgradeManager - 업그레이드 시스템 ⚠️
│   ├── UiManager - UI 관리 ✅
│   ├── SoundManager - 사운드 시스템 ✅
│   └── EnemySpawner - 적 생성 ✅
├── Player/
│   ├── PlayerShip - 플레이어 기체 메인
│   ├── HeatSystem - 과열 시스템
│   ├── BrakeSystem - 브레이크 시스템
│   ├── Impactable - 충돌 피해 시스템 ✅
│   └── StackSystem - 스택 시스템
├── SpaceShips/
│   ├── Damageable - HP 시스템 ✅
│   ├── ShooterBase - 사격 기본 클래스 ✅
│   ├── EnemyShip - 적 기체
│   └── FindTarget - 타겟 찾기
├── Movements/
│   ├── MoveStandard - 표준 이동 ❌
│   ├── RotateToTarget - 타겟 회전
│   └── 기타 이동 컴포넌트들
├── Projectiles/ - 발사체 시스템 ✅
├── UIs/ - UI 컴포넌트들 ✅
└── Drone/ - 드론 시스템 (미사용 예정)
```

범례:
- ✅ 재사용 가능
- ⚠️ 수정 필요
- ❌ 새로 구현 필요

## 주요 변경사항

### 1. 조작 시스템 (핵심 변경)

**현재 상태:**
```csharp
// InputManager.cs - 레거시 Input 시스템 사용
public bool MoveForwardInput => false;  // 자동 전진 미사용
public Vector2 MoveDirectionInput => Input.GetAxisRaw("Horizontal/Vertical");
public bool FireInput => Input.GetMouseButton(0);
```

**목표:**
- 자동 전진: transform.up 방향으로 지속적인 힘 적용
- 회전 제어: 화면 좌/우 클릭으로 회전 방향 조절
- 물리 기반 이동: Rigidbody2D.AddForce() 사용

#### 1-1. Input System 마이그레이션 (필수) ✅ 완료

**현재 상황:**
- ✅ 새 Input System 패키지 설치됨 (v1.17.0)
- ✅ InputSystem_Actions.inputactions 파일 존재
- ✅ 모든 스크립트가 새 Input System 사용 중

**마이그레이션 이유:**
1. 터치/모바일 지원 우수 (화면 좌/우 감지 용이)
2. 이미 패키지 설치되어 Input Actions 파일 존재
3. 향후 확장성 (리바인딩, 다양한 디바이스)

**완료된 작업:**
- [x] InputSystem_Actions.inputactions 수정
  - [x] Move 액션 추가 (Vector2: WASD)
  - [x] Rotate 액션 추가 (Axis: A/D, 화살표, 게임패드)
  - [x] Fire 액션 유지 (마우스, 터치, 스페이스바)
  - [x] Pause 액션 추가 (ESC, 게임패드 Start)
  - [x] 불필요한 액션 제거 (Jump, Sprint, Crouch 등)
- [x] InputManager.cs 수정
  - [x] 레거시 Input → Input System 전환
  - [x] MoveDirectionInput 프로퍼티 (Vector2)
  - [x] RotateInput 프로퍼티 추가 (float)
  - [x] FireInput, PauseInput, EscapeInput 프로퍼티
- [x] 레거시 Input 사용 파일 마이그레이션
  - [x] UiManager.cs - Mouse.current.position 사용
  - [x] LookMouseSmooth.cs - Mouse.current.position 사용
  - [x] MouseFollow.cs - Mouse.current.position 사용

**다음 단계:**
- [ ] Unity에서 동작 테스트
- [ ] Player Settings를 "Input System Only"로 변경
- [ ] MoveStandard → 새로운 자동 전진 컴포넌트 작성
- [ ] 회전 시스템 구현

### 2. 경계 처리 시스템

**현재 상태:**
- 확인 필요: BoundaryJump.cs 존재 (텔레포트 기능?)

**목표:**
- 화면 경계를 벗어나면 즉사

**작업 필요:**
- [ ] 현재 경계 처리 방식 확인
- [ ] 경계 즉사 시스템 구현

### 3. 업그레이드 시스템 (대폭 수정) ✅ 완료

**이전 상태:**
```csharp
// UpgradeManager.cs
// 3가지 고정 타입만 존재
enum UpgradeType {
    Ship,
    Shooter,
    EmergencyProtocol
}
```

**현재 상태:**
- ✅ 증분 기반 로그라이크 시스템 구현 완료
- ✅ 레벨업 시 랜덤 3가지 옵션 제시
- ✅ 각 업그레이드별 독립적인 레벨 추적 (예: 멀티샷 Lv.2/4)
- ✅ PlayerStats 싱글톤 중앙화
- ✅ 6가지 업그레이드 구현: 최대 내구도/실드, 실드 재생 속도/지연, 멀티샷, 충돌 데미지

**구현된 시스템:**
```csharp
// UpgradeData.cs - 정적 데이터 클래스
- IncrementValues: 증분값 정의
- MaxLevels: 최대 레벨 정의
- DisplayNames: 한글 이름 매핑
- GetRandomUpgradeOptions(): 랜덤 3개 선택

// UpgradeManager.cs - 업그레이드 관리
- statLevels: 필드별 레벨 추적
- SelectUpgrade(): 업그레이드 선택 처리
- PlayerStats.ApplyUpgrade() 호출
```

**상세 문서**: [UpgradeSystem.md](Design/UpgradeSystem.md)

**다음 단계 (Phase 2):**
- [ ] 미구현 업그레이드 활성화 (연사 속도, 발사체 데미지/속도, 이동/회전 속도, 충돌 저항)
- [ ] 특수 업그레이드 (Missile, Pulse, EmergencyProtocol)
- [ ] 희귀도 시스템
- [ ] 시너지 시스템

### 4. 레벨/경험치 시스템 (재사용)

**현재 상태:**
```csharp
// LevelManager.cs
- 적 격파 → 경험치 획득 ✅
- 경험치 누적 → 레벨업 ✅
- 레벨업 시 UpgradePoint 지급 ✅
```

**목표:**
- 동일한 시스템 유지
- 밸런싱만 조정

**작업 필요:**
- [x] 시스템 확인 완료
- [ ] 경험치량 밸런싱 (Phase 1 후반)

### 5. 전투 시스템 (일부 재사용)

**재사용 가능:**
- Damageable.cs - HP 시스템
- ShooterBase.cs - 사격 기본 클래스
- Impactable.cs - 충돌 피해 시스템
- 발사체 시스템

**작업 필요:**
- [ ] 자동 사격 vs 수동 사격 결정
- [ ] 충돌 피해량 밸런싱
- [ ] 사격 빌드 vs 충돌 빌드 차별화

### 6. 적 생성 시스템 (대폭 개선) 🚧 진행 중

**현재 상태:**
```csharp
// EnemySpawner.cs
- 시간 기반 스폰 ✅
- 무한 스폰 모드 ✅
- Edge 별 스폰 위치 ✅
```

**진행 중인 작업 (절차적 웨이브 생성 시스템):**
- [x] 시스템 분석 및 기획 문서 작성 완료
- [x] 구현 계획 수립 완료 (1단계)
- [x] Phase 1: 데이터 구조 구축 ✅
- [x] Phase 2: 절차적 생성 엔진 (SmallSwarm 패턴) ✅
- [x] Phase 3: EnemySpawner 통합 ✅
- [x] Phase 4: 보스 웨이브 단순화 (ScriptableObject 대신 상단 고정 스폰) ✅
- [ ] Phase 5: Unity Editor 설정 (프리팹 연결)
- [ ] Phase 6: 통합 테스트

**목표 (1단계)**:
- 웨이브 1-9, 11-19, 21-29: 절차적 생성 (SmallSwarm 패턴)
- 웨이브 10, 20, 30: 보스 (상단 고정 스폰, 각 웨이브마다 다른 보스 1기)
- 예산 기반 적 생성 (예산 = 100 + 웨이브번호 × 50)
- 타입 안정성: EnemyShip 컴포넌트 직접 등록 (GameObject 대신)

**상세 문서**: [ProceduralWaveGeneration_Phase1.md](Implementation/ProceduralWaveGeneration_Phase1.md)

## 삭제/미사용 예정 시스템

- [ ] Drone 시스템 (DroneMaster, DroneSevant)
- [ ] BrakeSystem (자동 전진 방식에서 불필요)
- [ ] HeatSystem (과열 시스템 - 필요성 재검토)
- [ ] StackSystem (용도 불명 - 확인 필요)

## 확인 필요 사항

### 물리 시스템
- [ ] MoveStandard.cs - 현재 물리 기반인지 확인
- [ ] BoundaryJump.cs - 경계 처리 방식 확인
- [ ] 충돌 레이어 설정 확인

### UI 시스템
- [ ] 현재 UI 구조 확인
- [ ] 업그레이드 선택 UI 재사용 가능 여부
- [ ] HUD 요소들 확인

### 발사체 시스템
- [ ] BulletBase, BulletChase, BulletCurve 등 확인
- [ ] 발사체 종류 정리
- [ ] Object Pooling 구현 여부 확인

## 다음 작업 단계

### Phase 1A: 핵심 시스템 분석 ✅ 완료
1. [x] 물리 이동 시스템 상세 분석
2. [x] 경계 처리 시스템 확인
3. [x] UI 구조 파악
4. [x] 발사체 시스템 파악
5. [x] Architecture.md 문서화 완료

**결과:** 전체 코드베이스 구조, 패턴, 시스템 분석 완료

### Phase 1B: 조작 시스템 구현 ✅ 완료
1. [x] 자동 전진 구현 (MoveStandard moveManually=false)
2. [x] 회전 제어 구현 (RotateByInput.cs 생성)
3. [x] 입력 시스템 수정 (이미 Phase 1에서 완료)

**구현 내용:**
- **MoveStandard.cs**: 기존 컴포넌트 재사용, moveManually=false 설정으로 자동 전진
- **RotateByInput.cs**: 새 컴포넌트 생성, InputManager.RotateInput 사용
  - 물리 기반(토크) 또는 직접 회전 선택 가능
  - rotationSpeed로 회전 속도 조절
- **PlayerShip.cs**: 이동 로직 제거, 사격만 담당
  - movement, brakeSystem 참조 제거
  - MoveCheck() 메서드 삭제
  - FixedUpdate 삭제

**Unity 설정 필요:**
1. Player 게임오브젝트에 MoveStandard 컴포넌트 추가/확인
   - moveManually = false
   - movePower = 10 (조절 가능)
2. Player 게임오브젝트에 RotateByInput 컴포넌트 추가
   - rotationSpeed = 180
   - usePhysics = false (직접 회전 추천)
3. BrakeSystem 컴포넌트 제거 (미사용)

### Phase 1C: 게임플레이 루프
1. [ ] 경계 즉사 구현
2. [ ] 기본 적 AI 조정
3. [ ] 기본 업그레이드 3-5개 구현

## 참고사항

- 기존 코드의 MonoSingleton 패턴은 유지
- SoundManager, UiManager 등은 최대한 재사용
- 불필요한 기능은 과감히 제거하여 단순화
