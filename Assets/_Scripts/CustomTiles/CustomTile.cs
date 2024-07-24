using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class CustomTile : TileBase {
    public Vector3Int localCoordinate;
    public Vector3 worldPosition;
    public Tilemap tilemapMember;
    public bool hasTile;
    public TileBase tileStored;
    public string tileName;

    public int xCellIndex;
    public int yCellIndex;

    public bool hasObject;
    public GameObject objectStored;
}