# Git 커밋 가이드

> 일반적인 마크다운 문법은 [Markdown Guide](https://www.markdownguide.org/)를 참조하세요.
>
> 이 문서는 **AutoSpaceShooter 프로젝트의 커밋 규칙**만 다룹니다.

## 커밋 메시지 형식

### 기본 구조
```
<타입>: <제목> (한 줄 요약, 50자 이내)

<본문> (선택사항)
- 변경 사항 설명
- 왜 변경했는지 설명
```

### 커밋 타입

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새로운 기능 추가 | `feat: 플레이어 자동 이동 시스템 구현` |
| `fix` | 버그 수정 | `fix: 경계 충돌 시 즉사 처리 버그 수정` |
| `refactor` | 코드 리팩토링 | `refactor: EnemySpawner 로직 개선` |
| `chore` | 빌드, 설정, Unity 에셋 변경 | `chore: FloatingText 속도 파라미터 조정` |
| `docs` | 문서 작업 | `docs: 게임 기획 문서 작성` |
| `perf` | 성능 개선 | `perf: Object Pooling 적용` |

### 작성 원칙

1. **제목은 50자 이내**, 명령형으로 작성
2. **한글 사용** (프로젝트 통일)
3. **무엇을, 왜** 변경했는지 명확히
4. 본문은 72자마다 줄바꿈 (선택사항)
5. **Co-Authored-By 사용 금지** (불필요한 메타정보 제거)

### 좋은 커밋 예시
```
feat: 플레이어 회전 조작 시스템 구현

- 화면 좌/우 터치로 회전 방향 조절
- Input System 패키지 활용
- 모바일 터치 및 마우스 클릭 모두 지원
```

### 나쁜 커밋 예시
```
❌ update
❌ 작업중
❌ 기능 추가했음
❌ fix bug
```

## 커밋 단위

**하나의 커밋 = 하나의 논리적 변경**

논리적으로 독립적인 변경사항은 별도 커밋으로 분리합니다.

### 분리해야 하는 경우
- 서로 다른 시스템 변경 (예: PlayerStats + UI)
- 기능 추가 + 문서 작성
- 여러 버그 수정

### 분리하지 않는 경우
- 한 기능 구현에 필요한 여러 파일
- 버그 수정과 관련 테스트

**판단 기준**: "이 커밋을 되돌리면 기능이 완전히 제거되는가?"

## 브랜치 전략

### 기본 작업
- **main 브랜치**에서 직접 작업 (1인 개발)
- 커밋 단위를 작게 유지

### 큰 기능 개발 시 (선택사항)
```bash
# 기능 브랜치 생성
git checkout -b feature/upgrade-system

# 작업 후 병합
git checkout main
git merge feature/upgrade-system
git branch -d feature/upgrade-system
```

## 참고 자료
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Markdown Guide](https://www.markdownguide.org/)
