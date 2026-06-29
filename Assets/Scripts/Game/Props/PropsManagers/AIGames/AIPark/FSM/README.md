# AI医院NPC状态系统

## 系统概述

AI医院NPC状态系统是一个基于状态模式和责任链模式的NPC行为控制系统，用于管理医院场景中NPC的行为。系统通过服务器下发的剧本数据，控制NPC在不同时间点执行不同的行为，并支持玩家交互打断和恢复。

## 核心功能

1. **基于时间的章节切换**：根据预设的时间点，自动切换NPC的行为章节。
2. **状态缓存与恢复**：当NPC被玩家打断时，能够缓存当前状态，并在玩家交互结束后恢复。
3. **灵活的状态扩展**：可以方便地添加新的状态类型，扩展NPC的行为。
4. **JSON配置支持**：支持从JSON数据创建NPC剧本，便于服务器下发和配置。

## 系统架构

系统主要由以下组件组成：

- **AIChapterState**：章节状态基类，定义状态的基本接口。
- **AIChapterStateMachine**：状态机，管理状态的切换、缓存和恢复。
- **AIChapterHandler**：章节处理器，负责根据时间切换章节和处理玩家交互。
- **AIHospital_CharacterBehaviour**：NPC角色行为控制器，提供移动、动画等基本功能。
- **AIHospital_CharacterManager**：NPC管理器，负责创建和管理NPC。
- **具体状态类**：如CheckRoomState、TalkWithDoctorState等，实现具体的行为逻辑。

## 使用方法

### 1. 创建NPC

```csharp
// 创建NPC管理器
AIHospital_CharacterManager manager = new AIHospital_CharacterManager();

// 从JSON创建NPC
string json = @"{
    ""role"": ""护士"",
    ""plan"": [
        {
            ""location"": ""病房"",
            ""action"": ""查房"",
            ""startTime"": 0,
            ""endTime"": 60
        },
        // 更多章节...
    ]
}";

manager.CreateCharacterFromJson(json);
```

### 2. 玩家交互

```csharp
// 获取NPC引用
AIHospital_CharacterBehaviour npc = GetNPC();

// 玩家开始对话
npc.PlayerStartTalk();

// 玩家结束对话
npc.PlayerEndTalk();
```

### 3. 添加新状态

```csharp
// 创建新的状态类
public class NewState : AIChapterState
{
    public NewState(AIHospital_CharacterBehaviour character) : base(character) { }

    public override void OnEnter()
    {
        // 进入状态的逻辑
    }

    public override void OnExit()
    {
        // 退出状态的逻辑
    }

    public override void OnUpdate(float deltaTime)
    {
        // 状态更新逻辑
    }
}

// 在AIChapterHandler中添加状态创建逻辑
private AIChapterState CreateState(AIHospital_ChapterData chapter)
{
    switch (chapter.action)
    {
        // 现有状态...
        case ActionType.NewAction:
            return new NewState(character);
        default:
            return new IdleState(character);
    }
}
```

## 状态互斥系统的应用

本系统借鉴了状态互斥系统的设计思路，主要体现在以下几个方面：

1. **状态缓存与恢复机制**：当NPC被玩家打断时，系统会缓存当前状态，并在玩家交互结束后恢复，类似于状态互斥系统中的CacheState机制。

2. **状态切换逻辑**：系统通过AIChapterHandler处理状态切换，确保在任何时刻NPC都处于正确的状态，类似于状态互斥系统中的状态管理机制。

3. **责任链模式**：虽然本系统没有直接使用责任链处理互斥关系，但状态的创建和管理采用了类似的设计思路，使系统更加灵活和可扩展。

## 注意事项

1. 在实际项目中，需要根据具体场景配置NPC的目标位置和动画参数。
2. 系统目前使用简化的移动方式，实际项目中应该使用导航系统或其他移动方式。
3. 动画参数需要与实际的Animator配置匹配。
4. 时间单位为秒，可以根据需要调整。 