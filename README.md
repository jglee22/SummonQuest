# SummonQuest - Unity 2D RPG

Unity 6000 LTS 기반 2D 수집형 RPG 포트폴리오 프로젝트입니다.  
가챠로 캐릭터를 모으고, 스테이지를 클리어하며, 턴제 전투로 성장하는 게임 루프를 구현했습니다.

## 주요 기능

- **가챠 시스템**: 1연/10연, 등급별 확률, 중복 보상
- **캐릭터 수집/강화**: 레벨업, 스킬, 즐겨찾기, 필터/정렬
- **스테이지**: ScriptableObject 기반 던전, 해금/클리어 진행
- **턴제 전투**: 자동 전투, 스킬/일반 공격, 연속 몬스터/보스, 속성 상성, 상태이상
- **출전 캐릭터 지정**: 보유 캐릭터 중 전투 참여 캐릭터 선택/저장
- **각성 시스템**: 중복 캐릭터 소모로 각성 단계/최대 레벨/공격력 상승
- **저장**: JSON 단일 파일 (`character_save.json`) + temp 검증 후 교체 + 백업 (`.bak`)

## 기술 스택

- Unity 6000.0.52f1 LTS
- C# / ScriptableObject
- DOTween
- TextMeshPro
- Singleton + Manager 패턴

## 시스템 구조

```mermaid
flowchart TB
    subgraph Data["데이터 계층"]
        CD[CharacterDatabase]
        CHD[CharacterData]
        SD[StageData]
        SP[StageProgress]
        MD[MonsterData]
        SK[SkillData]
        GC[GameConfig]
        GT[GachaTable]
    end

    subgraph Core["핵심 매니저"]
        GM[GameManager]
        PI[PlayerInventory]
        SM[SaveManager]
        CM[CurrencyManager]
        STM[StageManager]
    end

    subgraph Gameplay["게임플레이"]
        Gacha[GachaManager]
        Battle[BattleManager]
        BR[BattleRewardHandler]
        BUI[BattleUIController]
    end

    subgraph UI["UI"]
        UM[UIManager]
        NM[NotiManager]
        CharUI[CharacterListUI]
        StageUI[StageSelectionUI]
    end

    Gacha --> GT
    Gacha --> CD
    Gacha --> PI
    Gacha --> CM
    Battle --> BUI
    Battle --> BR
    Battle --> PI
    Battle --> STM
    Battle --> GC
    STM --> SD
    STM --> SP
    STM --> MD
    PI --> SM
    CM --> SM
    STM --> SM
    GM --> SM
    Gacha --> CharUI
    UM --> NM
```

## 클래스 역할

| 클래스 | 역할 |
|--------|------|
| `CharacterDatabase` | `Resources/CharacterData` 단일 원본 로드 (`GetById`, `All`) |
| `PlayerInventory` | 보유 캐릭터 데이터, 출전/각성, 저장 |
| `GachaManager` | 가챠 뽑기, 결과 연출 |
| `GachaTable` | 등급별 가챠 확률 (ScriptableObject) |
| `BattleManager` | 전투 흐름 조율 (턴, 승패, 보상 연동) |
| `BattleCombatResolver` | 공격/스킬 데미지, 스킬 선택 |
| `BattleStatusEffectProcessor` | DOT, 스턴, 버프, 턴 종료 처리 |
| `BattleState` | HP, 턴 수, 상태이상 등 전투 상태 |
| `BattleUIController` | 전투 UI 표시/로그/결과 패널/버튼 상태 |
| `BattleRewardHandler` | 전투 보상(경험치/골드) 지급 |
| `StageManager` | 스테이지 로드/진행/해금 (`stageId` 기준 저장) |
| `StageData` | 스테이지 설계 데이터 (SO, `stageId` 포함) |
| `StageProgress` | 스테이지 런타임 진행 상태 (해금/클리어) |
| `StageSelectionUI` | 스테이지 선택 UI (프리팹 기반) |
| `SaveManager` | JSON 저장/로드, temp→검증→교체, 버전/백업 |
| `GameConfig` | 가챠 비용, 전투 보상, HP/상성/각성/상태이상 설정 |
| `ElementHelper` | Fire > Wind > Earth > Water > Fire 상성 계산 |
| `NotiManager` | 알림 패널/텍스트 표시 |

## 구조 개선 (아키텍처)

### 1. StageData ↔ StageProgress 분리

| | StageData (SO) | StageProgress (런타임) |
|--|----------------|------------------------|
| 역할 | 몬스터 구성, 보상, 난이도 | 해금/클리어/클리어 횟수 |
| 변경 | 에디터에서 설계 | 플레이 중 변경, JSON 저장 |

`StageManager`가 `StageData[]`(설계)와 `StageProgress[]`(진행)를 함께 관리합니다.

### 2. BattleManager 책임 분리

```
BattleManager                → 전투 흐름 조율 (턴 Invoke, 승패, 보상 연동)
BattleState                  → HP/턴/상태이상 목록
BattleCombatResolver         → 공격/스킬 데미지, 상성
BattleStatusEffectProcessor  → DOT, 스턴, 버프, 쿨/턴 종료
BattleUIController           → UI (로그, 결과 패널, 버튼)
BattleRewardHandler          → 보상 지급 (경험치/골드)
```

### 3. SaveManager 강화

- `saveVersion` 필드로 저장 포맷 버전 관리
- `character_save.tmp` 작성 → 검증 → `File.Replace`로 원자적 교체
- 저장 전 `character_save.bak` 백업 생성
- 로드 실패 시 백업 파일 자동 복구 시도
- try-catch로 JSON 파싱/쓰기 예외 처리

### 4. 데이터 단일 원본 / ID 기반 식별

- 캐릭터: `Resources/CharacterData` + `characterID`
- 스테이지: `Resources/StageData` + `stageId` (저장 시 인덱스와 함께 기록, 구세이브 호환)
- 가챠 확률: `Resources/GachaTable.asset`

## 게임 루프

```mermaid
flowchart LR
    A[가챠] --> B[캐릭터 수집]
    B --> C[스테이지 선택]
    C --> D[턴제 전투]
    D --> E[보상 획득]
    E --> F[저장]
    F --> A
```

1. 골드로 가챠 → 캐릭터 획득/중복 보상
2. 스테이지 선택 → 몬스터/보스 연속 전투
3. 승리 시 골드/경험치 → 캐릭터 강화/레벨업
4. `SaveManager`가 진행도 자동 저장

## 저장 구조

`Application.persistentDataPath/character_save.json`  
백업: `character_save.bak`

```json
{
  "saveVersion": 1,
  "ownedList": [{ "characterID", "level", "count", "exp", ... }],
  "playerGold": 0,
  "highestClearedStage": -1,
  "highestClearedStageId": "Stage_1",
  "stageProgress": [{ "stageId": "Stage_1", "stageIndex": 0, "isCleared", "clearCount" }],
  "totalPlayTime": 0,
  "totalBattles": 0,
  "totalGachaPulls": 0,
  "selectedCharacterId": "Char_1"
}
```

`totalGachaPulls`는 **가챠 실행 횟수**(1연/10연 각 1회)입니다.

## Resources 구조

```
Assets/Resources/
├── CharacterData/   # 캐릭터 SO (단일 원본)
├── StageData/       # 스테이지 SO (stageId)
├── MonsterData/     # 몬스터 SO
├── Prefabs/         # UI 프리팹 (StageSlot, StageSelectionPanel)
├── GameConfig.asset # 게임 밸런스 설정
└── GachaTable.asset # 등급별 가챠 확률
```

## 실행 방법

1. Unity 6000.0.52f1 LTS로 프로젝트 열기
2. `Assets/Scenes/Main.unity` 실행
3. 가챠 → 캐릭터 확인 → 스테이지 선택 → 전투 진행
4. `O` 키: 설정 패널 (플레이타임 표시 포함)

## 스크린샷 / GIF

스크린샷/GIF 추가 예정.

| 파일 | 내용 | 상태 |
|------|------|------|
| `Docs/gacha.gif` | 가챠 1연/10연 결과 | 추가 예정 |
| `Docs/character.gif` | 캐릭터 목록 / 상세 / 강화 | 추가 예정 |
| `Docs/battle.gif` | 스테이지 선택 → 전투 → 클리어 | 추가 예정 |

```markdown
![가챠](Docs/gacha.gif)
![캐릭터](Docs/character.gif)
![전투](Docs/battle.gif)
```

## 포트폴리오 메모

- ScriptableObject 기반 **데이터(설계)** 와 **진행 상태** 분리
- `characterID` / `stageId` 기반 저장으로 데이터 재배치에도 진행도 유지
- Manager + Battle 서브시스템 분리로 전투/가챠/저장 책임 명확화
- JSON 통합 저장 + temp 검증 + 백업으로 데이터 안정성 확보
- UI 런타임 생성 → Resources 프리팹 기반으로 전환 (스테이지 선택)
