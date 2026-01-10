# 문서 작성 가이드

> 일반적인 마크다운 문법은 [Markdown Guide](https://www.markdownguide.org/)를 참조하세요.
>
> 이 문서는 **AutoSpaceShooter 프로젝트의 문서 작성 규칙**만 다룹니다.

## 문서 구조

```
Docs/
├── Roadmap.md               # 개발 로드맵
├── Design/                  # 게임 디자인
│   └── GameDesignOverview.md
└── Guidelines/              # 개발 가이드라인
    ├── CodeStyle.md
    ├── CommitGuide.md
    └── DocumentationGuide.md    # 이 문서
```

## 문서 작성 원칙

### 1. 문서 길이 관리
- 문서가 **100줄을 넘어가면 분리를 고려**
- 분리 기준: 주제별, 시스템별로 독립적인 문서 생성
- 예: `GameDesignOverview.md` → 시스템별로 `CombatSystem.md`, `ProgressionSystem.md` 등

### 2. 파일명 규칙
- **PascalCase** 사용 (예: `GameDesignOverview.md`)
- 명확하고 설명적인 이름
- 약어 사용 지양

### 3. 내용 작성
- **프로젝트 특화 내용**에 집중
- 일반적인 규칙은 외부 링크로 대체
- "당연한" 내용은 과감히 제거

### 4. 업데이트 원칙
- 코드 변경 시 관련 문서도 함께 업데이트
- 문서와 실제 구현이 일치하도록 유지
- 오래된 정보는 주기적으로 정리

## 기획 문서 작성 가이드 (Design/)

### 핵심 원칙

**코드가 아닌 게임플레이에 집중**
- "무엇을 하는가"에 초점
- 플레이어 경험과 게임 메카닉 설명

**제외할 내용**:
- 클래스/메서드 구현 코드
- Dictionary, Enum 등 데이터 구조 코드
- 파일별 상세 구현 방법

**포함할 내용**:
- 시스템 개요 및 작동 원리
- 수치 밸런싱 정보
- 빌드 전략 및 시너지
- 확장 가능성

### 문서 구조

```markdown
# [시스템 이름]
> 한 줄 요약

## 시스템 개요
## 상세 설명 (수치, 효과, 시너지)
## 밸런싱 가이드
## 확장 가능성
## 참고 문서
```

### 항목 번호 표기

**5개 이상의 항목**을 나열할 때는 번호를 표기하면 가독성과 참조 편의성이 향상됩니다.

**예시**:
```markdown
| No. | 필드 | 한글 이름 | 증가량 |
|-----|------|----------|--------|
| 1 | MaxDurability | 최대 내구도 | +50 |
| 2 | MaxShield | 최대 실드 | +50 |
| 3 | ShieldRegenRate | 실드 재생 속도 | +5/초 |
...
```

**참조 시**:
- "#3 실드 재생 속도를..."
- "#1~4는 생존 관련 업그레이드..."
- "탱커 빌드는 #2, #3, #4를 우선시..."

### 코드 참조

문서 끝에 간단하게:
```markdown
## 구현 파일
- [UpgradeData.cs](../../Assets/Scripts/UpgradeData.cs)
- [PlayerStats.cs](../../Assets/Scripts/Player/PlayerStats.cs)
```

## 참고 자료
- [Markdown Guide](https://www.markdownguide.org/)
- [Writing Good Documentation](https://docs.github.com/en/get-started/writing-on-github)
