using System.Collections.Generic;
using UnityEngine;

public enum TileTypes
{
    Empty, //0
    Floor, //1
    Wall, //2
    Door, //3
    LockedDoor, //4
    TrappedDoor, //5
    SecretDoor, //6
    Window, //7
    StairDownSmall, //8
    StairDownBig, //9
    StairUpSmall, //10
    StairUpBig, //11
}

public enum FileType
{
    TSV,
    PixelMap,
    SavedMap,
    FreeFormMap
}

public struct Dungeon
{
    public string name;//          A name for the save file
    public int[,] tiles;//         Int array representing the types of tiles at each position
}


public struct DungeonFile
{
    public static string path;
    public static bool fromSaveFile;
    public static FileType fileType;
    public static List<Color> pixelKey;
    public static List<string> donjonKey;
    public static int freeFormSize;
    public static int freeFormPixelsPerInch;
}