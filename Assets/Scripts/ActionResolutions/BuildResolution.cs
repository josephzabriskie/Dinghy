using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;
using PlayerActions;

public class BuildResolution : ActionResolution
{
    //Public
    public float delay;
    private float buildDelay = 1.5f;    

    void Awake(){
        //Init();
    }

    public override void Init(List<ActionReq> actions, PlayBoard2D pb, CellStruct[,] pGrid, CellStruct[,] eGrid){
        base.argInit( actions, pb, pGrid, eGrid);
    }

    public override IEnumerator IEResolve(){
        Debug.Log("Starting Flight's IEResolve");
        running = true;
        ActionReq a = actions[0];
        GameGrid2D gg = a.t == pb.pobj.playerId ? pb.playerGrid : pb.enemyGrid; // Select grid to get coord's
        Cell2D cell = gg.GetCell(a.loc[0]);
        CellStruct cs = a.t == pb.pobj.playerId ? pGrid[(int)a.loc[0].x,(int)a.loc[0].y] : eGrid[(int)a.loc[0].x,(int)a.loc[0].y]; //TODO change this garbage
        float time = 0;
        while(time<=delay){ //Delay first
            time += Time.deltaTime;
            yield return null;
        }
        time = 0; //Delay artificially for a bit;
        cell.OnBuild();
        cell.SetCellStruct(cs);
        while(time <= buildDelay){
            time += Time.deltaTime;
            yield return null;
        }
        running = false;
        Destroy(gameObject);
    }
}
