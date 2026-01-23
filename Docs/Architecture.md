# Just 10 Minutes! 아키텍처 문서

> 10분 생존이 목표인 Auto-Forward 방식 슈팅 게임의 고수준 설계 문서

## 1. 아키텍처 개요

### 1.1 핵심 설계 원칙

- **싱글톤 매니저 패턴**: 게임 전역 시스템은 MonoSingleton으로 관리
- **컴포넌트 조합 설계**: 상속보다 조합을 통한 유연한 객체 구성
- **이벤트 기반 통신**: UnityEvent를 통한 느슨한 결합
- **물리 기반 게임플레이**: Rigidbody2D를 활용한 자연스러운 이동

### 1.2 컴포넌트 조합 철학

기능별 독립 컴포넌트를 조합하여 다양한 게임 오브젝트를 구성합니다.

**조합 예시**:
- **플레이어**: HP + 사격 + 자동이동 + 회전입력 + 경계즉사 + 충돌피해
- **일반 적**: HP + 경계텔레포트 + 자동이동
- **사격 적**: HP + 경계텔레포트 + 사격 + 타겟추적

## 2. 시스템 계층 구조

### 2.1 매니저 레이어 (전역 싱글톤)

게임의 핵심 시스템들은 싱글톤 매니저로 관리됩니다:
- **GameManager**: 게임 상태 및 흐름 제어 (OnTitle, OnCombat, OnUpgrade, GameOver, GameClear)
- **InputManager**: 입력 시스템 통합 (키보드/마우스/게임패드/터치)
- **UiManager**: UI 통합 관리
- **SoundManager**: 오디오 시스템
- **LevelManager**: 경험치/레벨업
- **UpgradeManager**: 업그레이드 시스템
- **ScoreManager**: 점수 관리
- **TimeRecordManager**: 경과 시간 추적 (0~600초)
- **TimeBasedSpawnManager**: 시간 기반 적 스폰 관리 (예산, 난이도 조정, 이벤트)
- **EnemySpawnWarningManager**: 적 스폰 경고 마커 시스템
- **ObjectSpawner**: 화면 가장자리 오브젝트 배치

### 2.2 게임플레이 레이어

재사용 가능한 컴포넌트들로 구성됩니다:

**핵심 컴포넌트**:
- **Damageable**: HP 및 피해 처리
- **ShooterBase**: 사격 시스템
- **BulletBase**: 발사체 기본 동작

**플레이어 전용**:
- **PlayerShip**: 플레이어 통합 제어 (자동 전진, 입력 기반 회전)
- **PlayerStats**: 중앙 스탯 관리 (싱글톤, 업그레이드 수치 저장)
- **HeatSystem**: 사격 과열 메커니즘
- **Impactable**: 충돌 피해 처리
- **RotateByInput**: 키 입력 기반 회전 제어

**적 전용**:
- **EnemyShip**: 적 행동 및 보상
- **FindTarget**: 타겟 탐색 AI

**이동/물리**:
- **MoveStandard**: 물리 기반 이동
- **BoundaryJump/BoundaryDeath**: 경계 처리
- 다양한 이동/회전 컴포넌트

## 3. 핵심 시스템

### 3.1 게임 상태 관리

**GameManager**는 게임의 전체 흐름을 제어합니다:
- 상태 전환: OnTitle → OnCombat ⇄ OnUpgrade → GameOver/GameClear
- 10분 보스 격파 시 GameClear 상태 처리
- PlayerShip 참조 관리
- 시간 조작 (슬로우 모션 등)

### 3.2 입력 시스템

**InputManager**는 다양한 입력 장치를 통합합니다:
- 이동 방향 (WASD/화살표/터치)
- 회전 입력 (A/D)
- 사격 입력 (마우스/스페이스/터치)
- Unity New Input System 기반

### 3.3 전투 시스템

**Damageable**: HP 관리 및 피해 처리
- onDamaged/onDead 이벤트로 다른 시스템과 연동
- 사망 시 이펙트 및 사운드 재생

**ShooterBase**: 발사체 생성 및 발사
- 멀티샷, 발사 딜레이, 타겟 레이어 설정
- 다양한 발사 패턴 지원 (직선, 각도 분산 등)

**BulletBase**: 발사체 기본 동작
- 수명, 타겟 충돌, 화면 이탈 처리
- 특수 발사체 (추적, 곡선, 펄스 등) 확장 가능

**Impactable**: 물리적 충돌 피해
- 양방향 피해 및 넉백 처리

### 3.4 성장 시스템

**LevelManager**: 경험치 및 레벨업
- 적 처치 시 경험치 획득
- 레벨업 시 업그레이드 포인트 지급

**UpgradeManager**: 업그레이드 적용
- 랜덤 선택지 시스템 (6개 중 3개 제시)
- 레벨업 포인트로 능력치 강화
- PlayerStats를 통해 업그레이드 반영

### 3.5 시간 기반 스폰 시스템

**TimeBasedSpawnManager**: 10분 게임 플레이를 위한 복합 스폰 시스템
- **예산 시스템**: 초기 50포인트, 시간당 10~25포인트 증가
- **존재 점수 시스템**: 현재 화면상 적 점수 추적 (목표: 50~800점)
- **동적 난이도 조정**: 목표 대비 실제 점수 비율로 스폰 속도 조정 (0.7x~1.3x)
- **시간 범위 기반 적 등장**: 각 적마다 등장 시간대 설정
- **Phase 시스템**: Phase 1-3 (각 200초)
- **특수 이벤트**: 보스 스폰, 대량 스폰 등

**EnemySpawnWarningManager**: 적 스폰 경고 시스템
- 이벤트/보스 스폰 1초 전 화면 가장자리 경고 마커 표시
- 오브젝트 풀을 사용한 마커 재사용

### 3.6 물리 이동

**MoveStandard**: Rigidbody2D 기반 이동
- 자동 전진 모드 (`moveManually = false`)로 플레이어 자동 이동
- 회전 입력으로 이동 방향 제어
- 최대 속도 제한 기능

**경계 처리**:
- **BoundaryJump**: 적용 (화면 반대편 텔레포트)
- **BoundaryDeath**: 플레이어용 (화면 이탈 시 int.MaxValue 데미지로 즉사)

## 4. 주요 게임플레이 흐름

### 4.1 10분 생존 루프

1. **게임 시작**: TimeRecordManager 시작 (0~600초)
2. **적 생성**: TimeBasedSpawnManager → 예산 누적 → 스폰 풀 갱신 → ObjectSpawner 호출
3. **전투**: PlayerShip 자동 전진 + 회전 조작 → 사격 → BulletBase 충돌 → Damageable 피해
4. **보상**: 적 사망 → ScoreManager/LevelManager에 point 전달
5. **성장**: 경험치 누적 → 레벨업 → 랜덤 3개 업그레이드 선택
6. **보스**: 600초(10분) 도달 → 경고 시스템 → 보스 스폰
7. **클리어**: 보스 격파 → GameManager.GameClear → 모든 적 제거

### 4.2 동적 난이도 조정

1. **목표 점수 계산**: 시간에 따라 50~800점 선형 증가
2. **실제 점수 추적**: HashSet으로 현재 화면상 적 존재 점수 계산
3. **난이도 배율**: 목표/실제 비율로 예산 증가율 조정 (0.7x~1.3x)
4. **스폰 조절**: 적이 부족하면 빠르게, 많으면 느리게

## 5. 설계 패턴

### 5.1 컴포넌트 조합

GameObject는 필요한 컴포넌트만 부착하여 구성됩니다:

**PlayerShip**:
- Damageable (HP/Shield)
- ShooterBase (발사)
- HeatSystem (과열)
- MoveStandard (자동 전진, moveManually=false)
- RotateByInput (회전 조작)
- Impactable (충돌 피해)
- BoundaryDeath (화면 이탈 즉사)

**EnemyShip**:
- Damageable (필수)
- BoundaryJump (필수, 화면 반대편 이동)
- (선택) ShooterBase + FindTarget + 이동 컴포넌트

### 5.2 이벤트 기반 통신

UnityEvent를 활용한 느슨한 결합:
- Damageable의 onDead/onDamaged 이벤트
- LevelManager의 onLevelUp 이벤트
- 각 시스템이 이벤트를 구독하여 반응

## 6. 확장 가이드

### 6.1 새 업그레이드 추가

1. UpgradeData에 새 UpgradeField 및 수치 정의
2. PlayerShip.SetSystem()에 적용 로직 추가
3. 필요시 관련 컴포넌트에 설정 메서드 구현
4. UI 연결

### 6.2 새 적 타입 추가

1. 프리팹 생성 (Sprite, Rigidbody2D, Collider2D)
2. 필수 컴포넌트: EnemyShip + Damageable + BoundaryJump
3. 선택 컴포넌트: ShooterBase, FindTarget, 이동 컴포넌트 등
4. EnemySpawner에 등록

### 6.3 새 발사체 추가

1. BulletBase를 상속하여 특수 동작 구현
2. 프리팹 생성 후 ShooterBase에 연결

## 7. 현재 기술 부채

- **오브젝트 풀링 부재** (P0): BulletBase, EnemyShip, FloatingText
- **레이어 하드코딩** (P1): 매직 넘버 대신 상수 관리 필요
- **싱글톤 의존성** (P2): 테스트 가능성 개선 필요

자세한 내용은 [TechnicalDebt.md](TechnicalDebt.md) 참고

## 8. 아키텍처 평가

### 8.1 강점

- **명확한 책임 분리**: 각 매니저가 독립적인 역할 수행
- **컴포넌트 조합**: 유연하고 재사용 가능한 설계
- **이벤트 기반**: UnityEvent로 느슨한 결합
- **복합 스폰 시스템**: 예산, 난이도, 존재 점수를 결합한 정교한 스폰 로직
- **자동 전진 + 회전**: 직관적인 조작으로 모바일 적합
- **10분 클리어 목표**: 명확한 승리 조건

### 8.2 개선 필요 영역

**P0 (최우선)**:
- 오브젝트 풀링 부재 (발사체, 적, FloatingText)

**P1 (단기)**:
- 레이어/태그 하드코딩
- 스폰 시스템 코루틴 동시성 문제 (부분 해결)

**P2 (중기)**:
- 싱글톤 의존성 (DI/ScriptableObject 이벤트 채널)
- 단위 테스트 부재

### 8.3 시스템 복잡도

**복잡한 시스템**:
- TimeBasedSpawnManager (740줄): 예산, 난이도, 이벤트, 경고 통합
- PlayerStats: 중앙 스탯 관리

**단순한 시스템**:
- EnemyShip (27줄): 단순 데이터 컨테이너
- 대부분의 컴포넌트: 단일 책임 원칙 준수

---

## 참고

- [Migration.md](Migration.md): 마이그레이션 상세 계획
- [TestingGuide.md](Guidelines/TestingGuide.md): 테스트 작성 가이드
- [CodeStyle.md](Guidelines/CodeStyle.md): 코드 스타일 가이드
