using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CellTypes;
using PlayerActions;
using ActionProc;

public class Validator {
    ActionProcState apc;

    public Validator(ActionProcState actionProc){
        apc = actionProc;
    }

    public void SetAPC(ActionProcState newAPC){
        this.apc = newAPC;
    }

    public bool Validate(ActionReq ar, CellStruct[,] pGrid, CellStruct[,] eGrid, Vector2 gridSize){
        switch(this.apc){
        case ActionProcState.reject:
            return false;
        case ActionProcState.multiTower:
            Debug.Log("In ActionProcState multiTower");
            switch(ar.a){
            case pAction.buildOffenceTower:
            case pAction.buildDefenceTower:
            case pAction.buildIntelTower:
                return MultiTowerValid(ar, pGrid, gridSize);
            default: // Other actions not permitted
                return false;
            }
        case ActionProcState.basicActions:
            Debug.Log("In ActionProcState basicActions");
            //Debug.Log("Validate " + ar.ToString());
            switch(ar.a){
            case pAction.buildOffenceTower:
            case pAction.buildDefenceTower:
            case pAction.buildIntelTower:
            case pAction.buildWall:
            case pAction.placeMine:
            case pAction.buildDefenceGrid:
                return DefBuildValid(ar, pGrid, gridSize);
            case pAction.fireBasic:
            case pAction.fireAgain:
            case pAction.fireRow:
            case pAction.fireSquare:
                return FireValid(ar, gridSize);
            case pAction.scout:
                return ScoutValid(ar, eGrid, gridSize);
            case pAction.blockingShot:
            case pAction.hellFire:
            case pAction.flare:
                return RandomShootActionValid(ar);
            case pAction.noAction:
                return false;
            default:
                Debug.LogError("Unhandled pAction: " + ar.a.ToString());
                break;
            }
            break;
        default:
            Debug.LogError("Unhandled APC: " + this.apc.ToString());
            break;
        }
        return false;
    }    

    //////////////////////////
    //GridSearchingFuncs
    int CountBldg(CBldg bldg, CBldg[,] grid){
        int count = 0;
        foreach(CBldg s in grid){
            if (s == bldg){
                count++;
            }
        }
        return count;
    }

    ///////////////////////////
    //Validate builds during multiTower phase
    bool MultiTowerValid(ActionReq ar, CellStruct[,] pGrid, Vector2 gridSize){
        Dictionary<pAction, CBldg> mapping = new Dictionary<pAction, CBldg>{
            {pAction.buildOffenceTower, CBldg.towerOffence},
            {pAction.buildDefenceTower, CBldg.towerDefence},
            {pAction.buildIntelTower, CBldg.towerIntel}};
        return !GridContainsBldg(pGrid, mapping[ar.a]) && DefBuildValid(ar, pGrid, gridSize) && !NextToBldgs(pGrid, ar.loc[0], mapping.Values.ToList(), gridSize);
    }

    //////////////////////////
    //Main validators
    bool DefBuildValid(ActionReq ar, CellStruct[,] pGrid, Vector2 gridSize){
        //Checks: targets player grid, has only one listed coord
        bool resl = TargetsSelf(ar) && LocCountEq(ar, 1) && BldgIs(pGrid, ar.loc[0], CBldg.empty, gridSize);
        //Debug.Log("DefBuildValid returning " + resl.ToString());
        return resl;
    }

    bool FireValid(ActionReq ar, Vector2 gridSize){
        //Checks that player not targeting self,  only one loc specified, target is in our grid
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1) && LocInGrid(ar.loc[0], gridSize);
        //Debug.Log("Attack Valid returned " + resl.ToString());
        return resl;
    }

    bool ScoutValid(ActionReq ar, CellStruct[,] eGrid, Vector2 gridSize){
        //Checks: target enemy, has only 1 loc, target is hidden
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1) && BldgIs(eGrid, ar.loc[0], CBldg.hidden, gridSize);
        //Debug.Log("ScoutValid returned " + resl.ToString());
        return resl;
    }

    bool RandomShootActionValid(ActionReq ar){
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 0);
        return resl;
    }

    //////////////////////////////
    // Library of validators
    bool TargetsSelf(ActionReq ar){
        bool resl = ar.t == ar.p;
        //Debug.Log("TargetsSelf returning: " + resl);
        return resl;
    }

    bool LocCountEq(ActionReq ar, int count){
        bool resl = ar.loc.Length == count;
        //Debug.Log("LocCountEq returning: " + resl);
        return resl;
    }

    public bool BldgIn(CellStruct[,] grid, Vector2 loc, List<CBldg> bldgs, Vector2 gridSize){
        bool resl = bldgs.Contains(grid[(int)loc.x, (int)loc.y].bldg);
        //Debug.Log("BldgIn returning: " + resl);
        return resl;
    }

    public bool BldgIs(CellStruct[,] grid, Vector2 loc, CBldg bldg, Vector2 gridSize){
        bool resl = grid[(int)loc.x, (int)loc.y].bldg == bldg;
        //Debug.Log("BldgIs returning: " + resl + ": " + grid[(int)loc.x, (int)loc.y] + "?" + bldg.ToString());
        return resl;
    }

    bool NextToBldg(CellStruct[,] grid, Vector2 loc, CBldg bldg, Vector2 gridSize){
        for(int x = -1; x <= 1; x++){
            for(int y = -1; y <= 1; y++){
                Vector2 testpos = new Vector2(loc.x + x, loc.y + y);
                if(!LocInGrid(testpos, gridSize)){
                    continue;
                }
                if(BldgIs(grid, testpos, bldg, gridSize)){
                    return true;
                }
            }
        }
        return false;
    }

    bool NextToBldgs(CellStruct[,] grid, Vector2 loc, List<CBldg> bldgs, Vector2 gridSize){
        foreach (CBldg bldg in bldgs){
            if(NextToBldg(grid, loc, bldg, gridSize)){
                //Debug.Log("NextToBldgs ret true: " + bldg.ToString());
                return true;
            }
        }
        //Debug.Log("NextToBldgs ret false");
        return false;
    }

    bool LocInGrid(Vector2 loc, Vector2 gridSize){
        bool resl = (loc.x >= 0) && (loc.y >= 0) && (loc.x < gridSize.x) && (loc.y < gridSize.y);
        //Debug.Log("LocInGrid returning: " + resl + ". " + loc.x.ToString() + "," + loc.y.ToString());
        return resl;
    }

    bool GridContainsBldg(CellStruct[,] grid, CBldg bldg){
        int i = 0;
        foreach(CellStruct cell in grid){
            i++;
            if(cell.bldg == bldg){
                //Debug.Log("GridContainsBldg ret true: " + bldg.ToString());
                return true;
            }
        }
        //Debug.Log("GridContainsBldg ret false: " + bldg.ToString());
        return false;
    }
}