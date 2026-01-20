# 적 시간 범위 정의

> **목적**: 시간 기반 스폰 시스템에서 각 적이 스폰 가능한 시간 범위를 정의합니다.
>
> **데이터 파일**: `Assets/Resources/Data/EnemyTimeRanges.csv`

## 핵심 개념

- **카운트다운 방식**: 게임은 14분(840초)에서 시작하여 0초까지 감소
- **spawnTimeMin**: 스폰 가능한 최소 시간 (작은 값, 게임 후반)
- **spawnTimeMax**: 스폰 가능한 최대 시간 (큰 값, 게임 초반)
- **시간 범위**: spawnTimeMin ≤ 현재 남은 시간 ≤ spawnTimeMax일 때 스폰 가능

---

## 적별 시간 범위

| # | 적 이름 | 등급 | 비용 | 시간 범위 | 초 단위 (min ~ max) | 스폰 기간 |
|---|---------|------|------|----------|---------------------|----------|
| 1 | Enemy_light_child | Light | 20 | 14:00 ~ 11:00 | 660 ~ 840 | 3분 |
| 2 | Enemy_light_kido | Light | 20 | 13:30 ~ 10:30 | 630 ~ 810 | 3분 |
| 3 | Enemy_light_thunder | Light | 25 | 13:00 ~ 10:00 | 600 ~ 780 | 3분 |
| 4 | Enemy_light_shield | Light+ | 40 | 12:30 ~ 9:00 | 540 ~ 750 | 3.5분 |
| 5 | Enemy_mid_Ghost | Mid | 80 | 12:00 ~ 7:00 | 420 ~ 720 | 5분 |
| 6 | Enemy_mid_Hornet | Mid | 90 | 11:30 ~ 6:30 | 390 ~ 690 | 5분 |
| 7 | Enemy_mid_master | Mid | 100 | 11:00 ~ 6:00 | 360 ~ 660 | 5분 |
| 8 | Enemy_mid_Knight | Mid+ | 120 | 10:30 ~ 5:00 | 300 ~ 630 | 5.5분 |
| 9 | Enemy_mid_sniper | Mid+ | 130 | 10:00 ~ 4:30 | 270 ~ 600 | 5.5분 |
| 10 | Enemy_mid_tank | Mid+ | 140 | 9:30 ~ 4:00 | 240 ~ 570 | 5.5분 |
| 11 | Enemy_mid_Spiral | Mid+ | 150 | 9:00 ~ 3:30 | 210 ~ 540 | 5.5분 |
| 12 | Enemy_heavy_mother | Heavy | 350 | 8:00 ~ 1:00 | 60 ~ 480 | 7분 |
| 13 | Enemy_heavy_Gunship | Heavy | 400 | 7:00 ~ 0:00 | 0 ~ 420 | 7분 |

---

## 설계 의도

### 1. 점진적 난이도 증가
- 비용이 낮은 적(Light)이 먼저 등장
- 비용이 높은 적(Heavy)은 게임 후반에 등장
- 30초~1분 간격으로 새로운 적 타입 등장

### 2. 자연스러운 전환
- 시간대가 겹치도록 설계 (급격한 변화 방지)
- 예: Light 적이 사라지기 전에 Mid 적이 등장 시작

### 3. Phase별 분포
**Phase 1 (14:00 ~ 10:00, 840~600초)**:
- Light 적 4종 전체 활성
- 게임 초반, 낮은 난이도

**Phase 2 (10:00 ~ 6:00, 600~360초)**:
- Light 적 일부 + Mid 적 전체 활성
- 중반, 중간 난이도

**Phase 3 (6:00 ~ 0:00, 360~0초)**:
- Mid 적 일부 + Heavy 적 전체 활성
- 후반, 높은 난이도

### 4. 다양성 확보
- 같은 등급이라도 각 적마다 고유한 시간 범위
- 플레이할 때마다 다른 적 조합 경험

---

## 시간대별 스폰 가능 적

### 14:00 (840초)
- Enemy_light_child

### 13:30 (810초)
- Enemy_light_child, Enemy_light_kido

### 13:00 (780초)
- Enemy_light_child, Enemy_light_kido, Enemy_light_thunder

### 12:30 (750초)
- Enemy_light_child, Enemy_light_kido, Enemy_light_thunder, Enemy_light_shield

### 12:00 (720초)
- Enemy_light_child, Enemy_light_kido, Enemy_light_thunder, Enemy_light_shield, Enemy_mid_Ghost

### 11:30 (690초)
- 위 5종 + Enemy_mid_Hornet

### 11:00 (660초)
- 위 6종 + Enemy_mid_master (Light 4종 + Mid 3종)

### 10:30 (630초)
- Enemy_light_kido, Enemy_light_thunder, Enemy_light_shield + Mid 3종 + Enemy_mid_Knight

### 10:00 (600초)
- Enemy_light_thunder, Enemy_light_shield + Mid 4종 + Enemy_mid_sniper

### 9:30 (570초)
- Enemy_light_shield + Mid 5종 + Enemy_mid_tank

### 9:00 (540초)
- Mid 6종 + Enemy_mid_Spiral (Mid 중심)

### 8:00 (480초)
- Mid 6종 + Enemy_heavy_mother

### 7:00 (420초)
- Mid 5종 + Heavy 2종

### 6:00 (360초)
- Mid 4종 + Heavy 2종

### 5:00 (300초)
- Mid+ 3종 + Heavy 2종

### 4:00 (240초)
- Mid+ 2종 + Heavy 2종

### 3:30 (210초)
- Mid+ 1종 + Heavy 2종

### 1:00 (60초)
- Enemy_heavy_Gunship (최종 보스급만 남음)

### 0:00 (0초)
- Enemy_heavy_Gunship (게임 종료)

---

## 구현 참고

### CSV 파일 형식
**파일 위치**: `Assets/Resources/Data/EnemyTimeRanges.csv`

```csv
EnemyName,Tier,Cost,TimeMin,TimeMax,Comment
Enemy_light_child,Light,20,660,840,14:00 ~ 11:00 (3분)
Enemy_light_kido,Light,20,630,810,13:30 ~ 10:30 (3분)
...
```

### CSV 편집 방법
1. 엑셀이나 구글 시트에서 열기
2. TimeMin, TimeMax 값 조정
3. 저장 후 Unity 재실행

### EnemyTimeRangeData.cs 사용법
```csharp
// 자동으로 CSV에서 로드 (정적 생성자)
// 사용 시:
List<string> spawnableEnemies = EnemyTimeRangeData.GetSpawnableEnemiesAtTime(720f);
bool canSpawn = EnemyTimeRangeData.CanSpawnAtTime("Enemy_light_child", 720f);
```

### 스폰 가능 여부 확인
```csharp
bool canSpawn = timeRemaining >= min && timeRemaining <= max;
```

---

## 밸런싱 고려사항

### 조정 가능한 요소
1. **시간 범위 폭**: 현재 3~7분, 너무 길거나 짧으면 조정
2. **등장 시점**: 특정 적이 너무 일찍/늦게 등장하면 조정
3. **겹침 정도**: 시간대 겹침을 늘리거나 줄여서 난이도 곡선 조정

### 테스트 체크리스트
- [ ] 게임 초반 (14:00~12:00): 적절한 난이도인지
- [ ] 게임 중반 (10:00~6:00): 너무 쉽거나 어렵지 않은지
- [ ] 게임 후반 (6:00~0:00): 생존 가능한 난이도인지
- [ ] 전환 구간 (11:00, 9:00, 7:00): 자연스럽게 난이도 증가하는지

---

## 관련 문서
- [TimeBasedBudgetSpawnSystem.md](TimeBasedBudgetSpawnSystem.md) - 시간 기반 스폰 시스템 설계
- [Phases.md](Phases.md) - 구현 계획
- [EnemySpawnScaling.md](EnemySpawnScaling.md) - 적 스탯 스케일링

---

## 문서 정보
- **작성일**: 2026-01-20
- **버전**: 1.0
- **상태**: Phase 1 구현용 초안
