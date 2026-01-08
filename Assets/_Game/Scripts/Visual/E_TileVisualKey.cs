// E_TileVisualKey.cs
public enum E_TileVisualKey
{
    // base tiles
    Floor = 0,
    Wall = 1,
    Obstacle = 2,
    Hole = 3,
    HoleFilled = 4,
    Goal = 5,

    // outer / ring
    Path = 10,
    InnerOuterGap = 11, // InnerBase ~ ChildPath 사이 간격(빈 공간)

    // doors
    DoorOpen = 20,
    DoorClosed = 21,

    // switch
    SwitchOn = 30,
    SwitchOff = 31,

    // blocks
    GapFillerBlock = 40,
}
