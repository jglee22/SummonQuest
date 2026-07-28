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
- **저장**: JSON 단일 파일 (`character_save.json`) + 백업 (`.bak`)

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
        CD[CharacterData]
        SD[StageData]
        SP[StageProgress]
        MD[MonsterData]
        SK[SkillData]
        GC[GameConfig]
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
| `PlayerInventory` | 보유 캐릭터 데이터, 출전/각성, 저장 |
| `GachaManager` | 가챠 뽑기, 결과 연출 |
| `BattleManager` | 턴제 전투 로직 (데미지, 상태이상, 승패) |
| `BattleUIController` | 전투 UI 표시/로그/결과 패널/버튼 상태 |
| `BattleRewardHandler` | 전투 보상(경험치/골드) 지급 |
| `StageManager` | 스테이지 로드/진행/해금 |
| `StageData` | 스테이지 설계 데이터 (SO, 불변) |
| `StageProgress` | 스테이지 런타임 진행 상태 (해금/클리어) |
| `SaveManager` | JSON 저장/로드, 버전/백업/예외 처리 |
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
BattleManager       → 전투 로직 (턴, 데미지, 상태이상, 승패)
BattleUIController  → UI (로그, 결과 패널, 버튼)
BattleRewardHandler → 보상 지급 (경험치/골드)
```

### 3. SaveManager 강화

- `saveVersion` 필드로 저장 포맷 버전 관리
- 저장 전 `character_save.bak` 백업 생성
- 로드 실패 시 백업 파일 자동 복구 시도
- try-catch로 JSON 파싱/쓰기 예외 처리

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
  "stageProgress": [{ "stageIndex", "isCleared", "clearCount" }],
  "totalPlayTime": 0,
  "totalBattles": 0,
  "totalGachaPulls": 0,
  "selectedCharacterId": "Char_1"
}
```

## Resources 구조

```
Assets/Resources/
├── CharacterData/   # 캐릭터 SO
├── StageData/       # 스테이지 SO
├── MonsterData/     # 몬스터 SO
└── GameConfig.asset # 게임 밸런스 설정
```

## 실행 방법

1. Unity 6000.0.52f1 LTS로 프로젝트 열기
2. `Assets/Scenes/Main.unity` 실행
3. 가챠 → 캐릭터 확인 → 전투 시작 → 스테이지 선택 → 전투 진행

## 스크린샷 / GIF

`Docs/` 폴더에 플레이 영상을 추가하면 포트폴리오 완성도가 올라갑니다.

| 파일 | 내용 |
|------|------|
| `Docs/gacha.gif` | 가챠 1연/10연 결과 |
| `Docs/character.gif` | 캐릭터 목록 / 상세 / 강화 |
| `Docs/battle.gif` | 스테이지 선택 → 전투 → 클리어 |

### GIF 녹화 방법

1. Unity Game 뷰에서 Play
2. Windows: Xbox Game Bar (`Win + G`) 또는 OBS Studio로 Game 뷰 녹화
3. GIF 변환: [ezgif.com](https://ezgif.com/video-to-gif) 등 사용
4. `Docs/` 폴더에 저장 후 README에 링크

```markdown
![가챠](Docs/gacha.gif)
![캐릭터](Docs/character.gif)
![전투](Docs/battle.gif)
```

## 포트폴리오 메모

- ScriptableObject 기반 **데이터(설계)** 와 **진행 상태** 분리
- Manager 간 역할 분리 (`PlayerInventory` / `BattleUIController` / `BattleRewardHandler`)
- JSON 통합 저장 + 버전/백업으로 데이터 안정성 확보
