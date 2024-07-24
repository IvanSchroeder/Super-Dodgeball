using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfo : ScriptableObject {
    public TileBase mainTile;
    public TileBase hoveredTile;
    public TileBase selectedTile;
    public TileBase inRangeTile;

    public string message;
    public bool isTraversable = true;
}
