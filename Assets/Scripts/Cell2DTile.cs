using UnityEngine;
using UnityEngine.Tilemaps;

// Tile that displays a Sprite when it is alone and a different Sprite when it is orthogonally adjacent to the same NeighourTile
[CreateAssetMenu]
public class Cell2DTile : TileBase
{
    public Sprite spriteA;
    public Sprite spriteB;
    public bool a;
    public GameObject testobj;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        Debug.Log("RefreshTile: spawn testobj");
        Instantiate(testobj, position, Quaternion.identity);

    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = a ? spriteA : spriteB;
    }
}