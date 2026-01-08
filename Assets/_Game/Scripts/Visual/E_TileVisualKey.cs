// E_TileVisualKey.cs
public enum E_TileVisualKey
{
    None = 0,

    // base tiles
    Floor = 1,
    Wall = 2,
    Hole = 3,
    HoleFilled = 4,
    Obstacle = 5,

    // switch
    SwitchOn = 10,
    SwitchOff = 11,

    // blocks
    GapFillerBlock = 20,


    // outer / ring
    Path = 30,
    InnerOuterGap = 31, // InnerBase ~ ChildPath 사이 간격(빈 공간)
    Goal = 32,

    // doors
    DoorOpen = 33,
    DoorClosed = 34,

    ChildPathOuterBorder = 40,
}

public enum E_SortingOrder
{
    Tile = 0,
    Hole = 1,
    Path = 2,
    Block = 3,
}
