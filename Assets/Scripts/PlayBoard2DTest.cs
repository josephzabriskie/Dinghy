using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class PlayBoard2DTest : MonoBehaviour {

    Tilemap board;
    public Tile grass;
    public Tile dividerSection;
    public Tile dividerEndL;
    public Tile dividerEndR;
    public TileBase testCustomTile;
    public Vector2Int boardSize;
    Vector2Int MAXBOARDSIZE = new Vector2Int(20,20);
    Vector2Int MINBOARDSIZE = new Vector2Int(2,2);
    Vector2Int localPlayerOffset;
    Vector2Int enemyPlayerOffset;

    void Start(){
        Debug.Log("Starting playboard Test");
        if (MINBOARDSIZE.x > boardSize.x || MINBOARDSIZE.y > boardSize.y){
            Debug.LogErrorFormat("Invalid board size: Set {0}, min {1}", boardSize, MINBOARDSIZE);
            return;
        }
        if (MAXBOARDSIZE.x < boardSize.x || MAXBOARDSIZE.y < boardSize.y){
            Debug.LogErrorFormat("Invalid board size: Set {0}, max {1}", boardSize, MINBOARDSIZE);
            return;
        }
        board = transform.Find("MainMap").GetComponent<Tilemap>();
        int rightMostPos = boardSize.x / 2; //We count on integer division here
        int leftMostPos = rightMostPos - (boardSize.x - 1);
        int highestPos = boardSize.y;
        int lowestPos = -boardSize.y;
        //Now fill in the rest of the board
        for(int x = leftMostPos; x <= rightMostPos; x++){
            for(int y = lowestPos; y <= highestPos; y++){
                board.SetTile(new Vector3Int(x, y, 0), grass);
            }
        }
        //Put in the divider starting with caps on the ends
        board.SetTile(new Vector3Int(leftMostPos,0,0), dividerEndL);
        board.SetTile(new Vector3Int(rightMostPos,0,0), dividerEndR);
        //And the dividers between the caps
        for(int i = leftMostPos + 1; i < rightMostPos; i++){
            board.SetTile(new Vector3Int(i,0,0), dividerSection);
        }
        board.SetTile(new Vector3Int(0,0,0), testCustomTile);
    }
}
	