# 게임 개요/목표/방법/규칙/조작 정리

## 게임 개요
- 본 게임은 **턴 기반 2D 퍼즐** 구조로 동작한다.
- 플레이어는 `Father`를 움직여 보드 상태를 바꾸고, 그 결과로 `Child`가 경로를 안전하게 전진하도록 만든다.
- 턴 처리의 핵심은 `Input → FatherAction → ChildStep → Resolve → Snapshot → End` 순서다.
- `Child`가 목표 경로 스텝에 도달하면 클리어, 막히면 난이도 정책에 따라 계속 진행/리와인드/리셋으로 분기한다.

## 1) 게임 목표
- 스테이지마다 정의된 `ChildGoalPathStep`에 `Child`를 도달시키는 것이 목표다.
- `Resolve` 단계에서 `Child.PathPos == ChildGoalPathStep`이면 즉시 `StageCleared`로 처리된다.

## 2) 게임 방법 (플레이 루프)
1. **입력(Input)**
   - 입력은 Input 단계에서만 소비된다.
   - 유효 입력이 들어오면 새 턴을 시작하고 입력 잠금이 켜진다.
2. **Father 행동(FatherAction)**
   - Father가 이동을 시도한다.
   - 이동 실패(벽/범위/점유 등) 시 해당 입력은 턴으로 확정되지 않으며, 턴 진행을 롤백하고 Input으로 복귀한다.
3. **Child 이동(ChildStep)**
   - Father 행동이 성공한 턴에 Child가 경로를 1스텝 이동 시도한다.
4. **판정(Resolve)**
   - 목표 도달을 우선 판정한다.
   - 목표 미도달이면 Child 막힘 여부를 판정하고 난이도 규칙에 따라 결과를 결정한다.
5. **스냅샷(Snapshot) / 종료(End)**
   - 턴 종료 시 스냅샷을 저장해 Rewind 기준점을 만든다.

## 3) 게임 규칙

### 3-1. 턴 규칙
- 원칙: **1회 유효 입력 = 1턴 처리**.
- FatherAction~Resolve 구간에서는 입력 잠금이 적용된다.
- Father 이동 실패 턴은 진행 취소되므로 Child는 움직이지 않는다.
- Child는 기본적으로 경로를 순환(loop)하며 다음 인덱스로 전진한다.

### 3-2. 승리/실패 규칙
- **승리 조건**: Child가 목표 경로 스텝에 도달.
- **실패 트리거**: Child가 다음 스텝으로 이동하지 못해 `ChildBlocked`가 되는 경우.

### 3-3. 난이도별 규칙
- **Easy**
  - `FailOnChildBlocked = false`.
  - Child가 막혀도 스테이지 실패 처리 없이 `Continue`로 진행.
- **Normal**
  - `FailOnChildBlocked = true`, `HardResetStage = false`.
  - Child가 막히면 `StageFailed_Rewind`로 처리되어 Rewind 진입 흐름을 탄다.
- **Hard**
  - `FailOnChildBlocked = true`, `HardResetStage = true`.
  - Child가 막히면 `StageFailed_Reset`으로 즉시 스테이지 리셋 분기로 이동한다.
  - 설정(`IronmanHardReturnToChapterStart`)에 따라 챕터 시작(1-1) 복귀가 가능하다.

## 4) Rewind 규칙
- Rewind는 턴 스냅샷 기반 복구 시스템이다.
- 진입 시 최신 스냅샷 인덱스를 커서 기준점으로 복원한다.
- `Prev`/`Next`로 스냅샷 커서를 이동해 상태를 확인한다.
- `Commit` 시:
  - 현재 커서 이후(미래) 스냅샷을 삭제한다.
  - Rewind를 종료하고 잔여 횟수를 1 소모한다.
- `Cancel` 시:
  - Rewind 진입 시점 스냅샷으로 되돌리고 종료한다.
  - 잔여 횟수는 소모하지 않는다.
- 잔여 횟수 0에서 자동 실패 진입(`FailureAuto`)이 발생하면 스테이지 재시작 요청으로 처리된다.

## 5) 조작 방법

### 5-1. 기본 이동
- **키보드**: `W/A/S/D` 또는 방향키 `↑/←/↓/→`
- **게임패드**: `Left Stick`

### 5-2. Rewind 조작
- **진입(Enter)**: `R` / 게임패드 `Y(북쪽 버튼)`
- **이전 턴(Prev)**: `Q` 또는 `[` / 게임패드 `LB`
- **다음 턴(Next)**: `E` 또는 `]` / 게임패드 `RB`
- **확정(Commit)**: `Space` 또는 `Enter` / 게임패드 `A(남쪽 버튼)`
- **취소(Cancel)**: `Esc` / 게임패드 `B(동쪽 버튼)`

### 5-3. 디버그 입력
- 플레이 상태에서 `C` 키를 누르면 강제로 StageClear 상태 전환이 트리거된다(프로토타입 디버그용).

## 6) 스테이지 진행 규칙
- 메인 메뉴에서 시작 시 진행도는 `Chapter 0 / Stage 0`으로 초기화된다.
- 스테이지 클리어 시 다음 분기:
  - 같은 챕터에 다음 스테이지가 있으면 `NextStage`
  - 챕터 마지막 스테이지면 `NextChapter`
  - 마지막 챕터의 마지막 스테이지면 엔딩 처리 후 메인 메뉴 복귀