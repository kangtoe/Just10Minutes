# 기술 부채 및 개선 사항

> 장기적 유지보수성과 확장성을 위한 기술적 개선 과제 추적

---

## 1. 성능 최적화

### 1.1 오브젝트 풀링 시스템 ⚠️ 최우선

**문제**: 발사체, 적, FloatingText가 매 프레임 Instantiate/Destroy되어 GC 부하 발생

**영향도**: 🔴 높음 | **우선순위**: P0

**해결**: `GenericPool<T>` 제네릭 풀 클래스 작성

**적용 대상**:
- [x] BulletBase 및 하위 클래스
- [x] EnemyShip
- [x] FloatingText
- [ ] 파티클 시스템 (선택적)

**검증**: Unity Profiler로 Instantiate/Destroy 호출 수 및 GC Alloc 확인

---

## 2. 아키텍처 개선

### 2.1 싱글톤 의존성 완화

**문제**: 모든 매니저가 `MonoSingleton<T>` 상속으로 강한 결합, 단위 테스트 불가

**영향도**: 🟡 중 | **우선순위**: P2

**대상 매니저**: GameManager, InputManager, LevelManager, UpgradeManager, ScoreManager, TimeRecordManager, EnemySpawner, ObjectSpawner, SoundManager, UiManager

**해결 옵션**:
- **A. ScriptableObject 기반 이벤트 채널 패턴** (권장) - Unity 네이티브, 라이브러리 불필요
- **B. 의존성 주입** (Zenject/VContainer) - 외부 라이브러리, 학습 곡선
- **C. Service Locator 패턴** - 싱글톤보다 유연, 테스트 가능

**참고**: [Unite Austin 2017 - Game Architecture with Scriptable Objects](https://www.youtube.com/watch?v=raQ3iHhE_Kk)

---

### 2.2 레이어 및 태그 중앙 관리

**문제**: 레이어 번호 하드코딩 (`m_Bits: 128`), 태그 문자열 분산으로 변경 시 여러 파일 수정 필요

**영향도**: 🟡 중 | **우선순위**: P2

**해결**: `Layers`, `Tags` 정적 클래스로 상수 중앙 관리

**적용 대상**: ShooterBase, FindTarget, BulletBase, Impactable, 모든 태그 사용 코드

---

### 2.3 매니저 통합 검토

**문제**: ScoreManager, TimeRecordManager 역할 유사 (단순 데이터 저장/조회)

**영향도**: 🟢 낮음 | **우선순위**: P3

**해결**: `GameStatsManager`로 통합 검토 (Score, PlayTime, WaveNumber, KillCount 통합 관리)

**추가 검토**: UpgradeManager + LevelManager 통합 가능성

---

### 2.4 스폰 시스템 동시성 문제 ✅ 해결 완료

**문제**: 경고 시스템의 지연 코루틴으로 인해 게임 상태 변경 후에도 스폰 계속 실행

**영향도**: 🟡 중 | **우선순위**: P1 (해결 완료)

**현재 적용**: for 루프 및 코루틴 내부에서 GameState 확인하여 조기 종료

**장기 개선안**:
- 코루틴 추적 및 일괄 중단
- CancellationToken 패턴
- **이벤트 기반 타이머 시스템** (권장) - 코루틴 의존성 제거, 동시성 문제 원천 차단

**주의**: 코루틴 기반 지연 처리는 동시성 문제에 취약, 상태 변경 시 항상 실행 중인 코루틴 고려 필요

---

## 3. 코드 품질

### 3.1 단위 테스트 작성

**문제**: 단위 테스트 없음, 리팩토링 시 버그 발생 위험

**영향도**: 🟡 중 | **우선순위**: P2

**테스트 단계**:
1. **순수 로직** - WaveBudget 계산, EnemyCost, WaveType 결정 (Unity 독립적)
2. **통합 테스트** - 웨이브 생성 → 스폰 → 격파 전 과정 (Unity PlayMode)
3. **PlayerStats** - 업그레이드 적용 검증

**도구**: Unity Test Framework (내장), NUnit

---

### 3.2 매직 넘버 제거

**문제**: 하드코딩된 상수 (레벨업 공식 `level * 1100` 등), 밸런싱 조정 시 코드 수정 필요

**영향도**: 🟢 낮음 | **우선순위**: P3

**해결**: `GameConstants` 정적 클래스로 상수 중앙 관리 (ExpPerLevel, WaveBudget, MoveSpeed 등)

---

## 4. 문서화

### 4.1 API 레퍼런스 작성

**문제**: 코드 API 레퍼런스 없음, 주요 클래스 public 메서드 설명 부족

**영향도**: 🟢 낮음 | **우선순위**: P3

**대상**: PlayerStats, UpgradeManager, ProceduralWaveGenerator, Damageable

**형식**: XML 주석 + Doxygen/Sandcastle

---

## 5. 기타 개선 사항

### 5.1 에러 핸들링 강화

**문제**: null 참조 예외 위험, 프리팹 로드 실패 시 처리 미흡

**해결**: null 체크 강화, 프리팹 로드 실패 시 기본값 제공 로직 추가

### 5.2 로깅 시스템 개선

**문제**: Debug.Log 직접 호출, 로그 레벨 제어 불가

**해결**: `Logger` 정적 클래스로 로그 레벨 제어 (Debug, Info, Warning, Error)

---

## 우선순위 요약

| 우선순위 | 항목 | 상태 |
|---------|------|------|
| **P0** (최우선) | 오브젝트 풀링 시스템 | ⬜ |
| **P1** (단기) | 레이어/태그 중앙 관리 | ⬜ |
| **P1** (단기) | 에러 핸들링 강화 | ⬜ |
| **P1** (완료) | 스폰 시스템 동시성 문제 | ✅ |
| **P2** (중기) | 싱글톤 의존성 완화 | ⬜ |
| **P2** (중기) | 단위 테스트 작성 | ⬜ |
| **P3** (장기) | 매니저 통합 검토 | ⬜ |
| **P3** (장기) | 매직 넘버 제거 | ⬜ |
| **P3** (장기) | API 레퍼런스 작성 | ⬜ |
| **P3** (장기) | 로깅 시스템 개선 | ⬜ |

---

## 참고

- [Roadmap.md](Roadmap.md) - 개발 로드맵
- [Architecture.md](Architecture.md) - 현재 아키텍처
- [Guidelines/CodeStyle.md](Guidelines/CodeStyle.md) - 코드 스타일 가이드
