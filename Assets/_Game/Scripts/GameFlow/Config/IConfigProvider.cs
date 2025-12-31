// IConfigProvider.cs
public interface IConfigProvider
{
    GameConfig LoadGameConfig();

    // 챕터/스테이지 인덱스로 StageDefinition 접근
    StageDefinition GetStageDefinition(int chapterIndex, int stageIndex);

    // 필요하면 챕터 정의도 직접 주도록 확장 가능
}
