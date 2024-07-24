using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using ExtensionMethods;
using System.Linq;

public class TileDestroyer : MonoBehaviour {
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GridLayout grid;
    [SerializeField] private TileBase tileToPlace;

    [SerializeField] private Vector3 mousePosition;
    [SerializeField] private Vector3Int centerTilePosition;

    [SerializeField] private List<Vector3Int> NeighborPositions;

    [SerializeField] private Vector2Int verticalSearchRange = new Vector2Int(-1, 1);
    [SerializeField] private Vector2Int horizontalSearchRange = new Vector2Int(-1, 1);
    [SerializeField] private Vector2 radiusSearchRange = new Vector2(0, 1);

    public SearchType searchType;

    void Awake() {
        tilemap = GetComponent<Tilemap>();
        grid = this.GetComponentInHierarchy<Grid>();
    }

    void Start() {
        if (mainCamera == null) mainCamera = this.GetMainCamera();
        Application.targetFrameRate = 144;
    }

    void Update() {
        mousePosition = mainCamera.ScreenToWorld(Mouse.current.position.ReadValue());

        if (Input.GetMouseButtonDown(2)) {
            PlaceTile(mousePosition);
        }

        if (Input.GetMouseButtonDown(1)) {
            DestroyTile(mousePosition);
        }
    }

    public void PlaceTile(Vector3 position) {
        SetTile(position, tileToPlace);
    }

    public void DestroyTile(Vector3 position) {
        SetTile(position);
    }

    public void SetTile(Vector3 position, TileBase tileToSet = null) {
        centerTilePosition = grid.WorldToCell(position);

        if ((tileToSet == null && tilemap.HasTile(centerTilePosition)) || (tileToSet != null && !tilemap.HasTile(centerTilePosition))) {
            tilemap.SetTile(centerTilePosition, tileToSet);
        }

        NeighborPositions = new List<Vector3Int>();

        switch (searchType) {
            case SearchType.SearchCenterOnly :
                break;
            case SearchType.SearchInSquare :
                SearchInSquare(centerTilePosition);
                break;
            case SearchType.SearchInRadius :
                SearchInRadius(centerTilePosition);
                break;
            case SearchType.SearchInCone :
                SearchInCone(centerTilePosition);
                break;
            case SearchType.SearchInCross :
                SearchInCross(centerTilePosition);
                break;
            case SearchType.SearchInX :
                SearchInX(centerTilePosition);
                break;
            case SearchType.SearchInStar :
                SearchInStar(centerTilePosition);
                break;
        }

        if (NeighborPositions.Count > 0) {
            foreach (Vector3Int pos in NeighborPositions) {
                if ((tileToSet == null && tilemap.HasTile(pos)) || (tileToSet != null && !tilemap.HasTile(pos))) {
                    tilemap.SetTile(pos, tileToSet);
                }
            }
        }

        NeighborPositions = new List<Vector3Int>();
    }

    /// <summary>
    /// Searches tiles around the center in a squared shape.
    /// </summary>
    public void SearchInSquare(Vector3Int centerTilePosition) {
        var horizontalReach = horizontalSearchRange.x.AbsoluteValue() + horizontalSearchRange.y.AbsoluteValue();
        var verticalReach = verticalSearchRange.x.AbsoluteValue() + verticalSearchRange.y.AbsoluteValue();

        if (horizontalReach > 0 && verticalReach > 0) {
            for (int y = verticalSearchRange.y; y >= verticalSearchRange.x; y--) {
                for (int x = horizontalSearchRange.x; x <= horizontalSearchRange.y; x++) {
                    Vector3Int tilePos = centerTilePosition + (Vector3Int.right * x) + (Vector3Int.up * y);
                    if (tilePos != centerTilePosition) NeighborPositions.Add(tilePos);
                }
            }
        }
    }

    /// <summary>
    /// Searches tiles around the center in a circular shape.
    /// </summary>
    public void SearchInRadius(Vector3Int centerTilePosition) {
        float radius = radiusSearchRange.y;
        float squaredRadius = radius * radius;

        for (int y = verticalSearchRange.y; y >= verticalSearchRange.x; y--) {
            for (int x = horizontalSearchRange.x; x <= horizontalSearchRange.y; x++) {
                Vector3Int tilePos = centerTilePosition + (Vector3Int.right * x) + (Vector3Int.up * y);
                float dx = centerTilePosition.x - tilePos.x;
                float dy = centerTilePosition.y - tilePos.y;
                
                float squaredDistance = (dx * dx) + (dy * dy);

                if (squaredDistance <= squaredRadius &&  tilePos != centerTilePosition) {
                    if (tilePos != centerTilePosition) NeighborPositions.Add(tilePos);
                }
            }
        }
    }

    public void SearchInCone(Vector3Int centerTilePosition) {
    }

    public void SearchInCross(Vector3Int centerTilePosition) {
        var horizontalReach = horizontalSearchRange.x.AbsoluteValue() + horizontalSearchRange.y.AbsoluteValue();
        var verticalReach = verticalSearchRange.x.AbsoluteValue() + verticalSearchRange.y.AbsoluteValue();

        for (int y = verticalSearchRange.y; y >= verticalSearchRange.x; y--) {
            for (int x = horizontalSearchRange.x; x <= horizontalSearchRange.y; x++) {
                if ((x != 0 && y == 0) || (x == 0 && y != 0)) {
                    Vector3Int tilePos = centerTilePosition + (Vector3Int.right * x) + (Vector3Int.up * y);
                    if (tilePos != centerTilePosition) NeighborPositions.Add(tilePos);
                }
            }
        }
    }

    public void SearchInX(Vector3Int centerTilePosition) {
        for (int y = verticalSearchRange.y; y >= verticalSearchRange.x; y--) {
            for (int x = horizontalSearchRange.x; x <= horizontalSearchRange.y; x++) {
                if (y.AbsoluteValue() == x.AbsoluteValue()) {
                    Vector3Int tilePos = centerTilePosition + (Vector3Int.right * x) + (Vector3Int.up * y);
                    if (tilePos != centerTilePosition) NeighborPositions.Add(tilePos);
                }
            }
        }
    }

    public void SearchInStar(Vector3Int centerTilePosition) {
        SearchInCross(centerTilePosition);
        SearchInX(centerTilePosition);
    }
}

public enum SearchType {
    SearchCenterOnly = 0,
    SearchInSquare = 1,
    SearchInRadius = 2,
    SearchInCone = 3,
    SearchInCross = 4,
    SearchInX = 5,
    SearchInStar = 6,
}
