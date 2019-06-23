using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;
using PlayerActions;

public abstract class ActionResolution : MonoBehaviour
{
//I'm struggling with how to construct this in a inheritance way. The actionresolution is aimed at resolving one or a list of similar actions through ui elements.
// I want to standardize the strategy of
// 1.   instantiate one of these
// 2.   Init it with data it needs
// 3.   coroutine yields to IEResolve until it's done
// 4.   Class must destory itself.

//Right now the only thing that can be similar is IEResolve and Init, hoping that more of what can be shared becomes clear
    protected List<ActionReq> actions;
    protected PlayBoard2D pb;
    protected CellStruct[,] pGrid;
    protected CellStruct[,] eGrid;
    public bool running = false;
    public abstract void Init(List<ActionReq> actions, PlayBoard2D pb, CellStruct[,] pGrid, CellStruct[,] eGrid);
    public void argInit(List<ActionReq> actions, PlayBoard2D pb, CellStruct[,] pGrid, CellStruct[,] eGrid){
        this.actions = actions;
        this.pb = pb;
        this.pGrid = pGrid;
        this.eGrid = eGrid;
    }

    public abstract IEnumerator IEResolve();
}
