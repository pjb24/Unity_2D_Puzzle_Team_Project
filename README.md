# 2D 퍼즐 게임 프로토타입

본 프로젝트는 게임의 핵심 구조(턴 시스템, Rewind, 난이도 정책)를 검증하기 위한 **프로토타입 구현**을 목표로 한다.

아트 품질, 연출 디테일은 범위에서 제외하며 **게임 루프가 끝까지 완주 가능한 상태**를 완료 기준으로 한다.

---

## 🎯 목표

* 턴 기반 퍼즐 구조 검증
* Father → Child 동기화 규칙 구현
* 실패 시 Rewind 메커니즘 검증
* 난이도(Easy / Normal / Hard)별 정책 분기 확인

---

## 🧱 프로젝트 구조

```text
Assets/
 ├─ Runtime/
 │   ├─ Core/           # GameFlow, TurnState
 │   ├─ Board/          # Grid, Tile, Path
 │   ├─ Characters/     # Father, Child
 │   ├─ Rewind/         # Snapshot, Controller
 │   └─ Difficulty/
 ├─ Data/
 │   ├─ GameConfig.asset
 │   ├─ StageDefinition.asset
 │   └─ ChapterVisualProfile.asset
 ├─ UI/
 ├─ Debug/
 └─ Tests/
```

---

## 1. 프로젝트 스캐폴딩

* Unity 6 프로젝트 생성
* 기본 씬 구성

  * `Boot`
  * `MainMenu`
  * `Gameplay`
* 입력 시스템 설정

  * 1턴 = 1입력 원칙
* 기본 폴더 / Assembly Definition 구성

---

## 2. 데이터 구조 (ScriptableObject)

### GameConfig

* 챕터 목록
* 난이도별 기본 설정
* 전역 파라미터

### StageDefinition

* 중앙 보드 크기
* 타일 / 장애물 데이터
* Father / Child 스폰 정보
* Child 이동 경로(`ChildPathDefinition`)
* 스테이지 전환 타입

### ChapterVisualProfile

* 챕터별 Father / Child 외형
* 사운드, 기본 애니메이션 파라미터

> 모든 SO는 `OnValidate`로 최소 유효성 검증 수행

---

## 3. 상태 머신 구조

### GameFlowState (상위 흐름)

```text
Boot
 → MainMenu
 → StageLoad
 → Play
 → StageClear
```

* StageLoad 시점에 데이터 로드 및 런타임 오브젝트 생성
* StageClear 후 다음 스테이지 또는 엔딩 분기

---

### TurnState (턴 진행)

```text
Input
 → FatherAction
 → ChildStep
 → Resolve
 → Snapshot
 → End
```

* 입력은 `Input` 상태에서만 허용
* FatherAction 시작 이후 입력 잠금
* Snapshot은 항상 턴 종료 직전에 저장

---

## 4. 핵심 플레이 구현

### 중앙 보드 (Top View)

* Grid 기반 타일 점유 관리
* Father 이동 (타일 단위)
* 장애물 / 이동 실패 판정

### 테두리 경로 (Side View)

* Child는 1차원 경로를 따라 이동
* 턴당 1스텝 전진
* 다음 위치가 막혀 있으면 `Blocked` 상태

### Resolve

* Child Blocked 여부 판정
* 난이도 정책에 따라 결과 분기

---

## 5. Rewind 시스템 (핵심)

### 인터페이스

```csharp
public interface IRewindable
{
    object CaptureState();
    void RestoreState(object state);
}
```

### TurnSnapshot

* turnIndex
* Rewindable 객체별 상태 저장

### RewindController

* Normal 난이도에서 실패 시 자동 진입
* 턴 단위 되감기
* 확인 시 되감기 횟수 소모
* 소진 시 스테이지 리셋 또는 챕터 복귀

### 복구 대상

* Father 위치
* Child 경로 인덱스
* 퍼즐 상태(스위치, 문 등)
* 턴 카운터 / 되감기 잔여 수

---

## 6. 난이도별 정책

### Easy

* Child Blocked 시 실패 없음
* Child 정지 또는 반동 처리
* 턴 정상 종료

### Normal

* Child Blocked 시 즉시 실패
* Rewind 모드 진입
* 횟수 소진 시 스테이지 리셋

### Hard

* Child Blocked 즉시 스테이지 리셋
* 옵션에 따라 챕터 1-1 복귀

---

## 7. UI (프로토타입 기준)

* HUD

  * 현재 턴
  * 난이도
  * 되감기 잔여 횟수
  * 챕터 / 스테이지 번호
* 입력 잠금 표시
* Rewind UI

  * 이전 턴
  * 다음 턴
  * 확인(커밋)
  * 취소(리셋)
* 실패 / 클리어 팝업

---

## 8. 연출 (최소 구현)

* Stage 전환 시 Fade / Slide
* ChapterVisualProfile 적용
* Child 경로 연출은 단순 Fade 처리

---

## 9. 디버그 & 툴

* 디버그 패널

  * 즉시 실패 / 클리어
  * 난이도 강제 변경
  * 스테이지 강제 로드
* Rewind 스냅샷 최대 개수 제한
* 로그 카테고리 분리

  * GameFlow / Turn / Rewind / Stage

---

## 10. 완료 기준 (Definition of Done)

* 메인 메뉴에서 스테이지 시작 가능
* Father 1입력 = Child 1스텝 동기화
* Normal 난이도에서 Rewind 정상 동작
* Easy / Hard 정책 차이 확인 가능
* 챕터 시작 → 엔딩까지 1회 완주 가능

---

## 📌 비고

* 본 프로젝트는 **기능 검증용 프로토타입**이다.
* 아트, 사운드, 최적화는 이후 단계에서 진행한다.