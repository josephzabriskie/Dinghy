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

    public bool Validate(ActionReq ar, CState[,] pGrid, CState[,] eGrid, Vector2 gridSize){
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
    int CountState(CState state, CState[,] grid){
        int count = 0;
        foreach(CState s in grid){
            if (s == state){
                count++;
            }
        }
        return count;
    }

    ///////////////////////////
    //Validate builds during multiTower phase
    bool MultiTowerValid(ActionReq ar, CState[,] pGrid, Vector2 gridSize){
        Dictionary<pAction, CState> mapping = new Dictionary<pAction, CState>{
            {pAction.buildOffenceTower, CState.towerOffence},
            {pAction.buildDefenceTower, CState.towerDefence},
            {pAction.buildIntelTower, CState.towerIntel}};
        return !GridContainsState(pGrid, mapping[ar.a]) && DefBuildValid(ar, pGrid, gridSize) && !NextToStates(pGrid, ar.loc[0], mapping.Values.ToList(), gridSize);
    }

    //////////////////////////
    //Main validators
    bool DefBuildValid(ActionReq ar, CState[,] pGrid, Vector2 gridSize){
        //Checks: targets player grid, has only one listed coord
        bool resl = TargetsSelf(ar) && LocCountEq(ar, 1) && StateIs(pGrid, ar.loc[0], CState.empty, gridSize);
        //Debug.Log("DefBuildValid returning " + resl.ToString());
        return resl;
    }

    bool FireValid(ActionReq ar, Vector2 gridSize){
        //Checks that player not targeting self,  only one loc specified, target is in our grid
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1) && LocInGrid(ar.loc[0], gridSize);
        //Debug.Log("Attack Valid returned " + resl.ToString());
        return resl;
    }

    bool ScoutValid(ActionReq ar, CState[,] eGrid, Vector2 gridSize){
        //Checks: target enemy, has only 1 loc, target is hidden
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1) && StateIs(eGrid, ar.loc[0], CState.hidden, gridSize);
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

    public bool StateIn(CState[,] grid, Vector2 loc, List<CState> states, Vector2 gridSize){
        bool resl = states.Contains(grid[(int)loc.x, (int)loc.y]);
        //Debug.Log("StateIn returning: " + resl);
        return resl;
    }

    public bool StateIs(CState[,] grid, Vector2 loc, CState state, Vector2 gridSize){
        bool resl = grid[(int)loc.x, (int)loc.y] == state;
        //Debug.Log("StateIs returning: " + resl + ": " + grid[(int)loc.x, (int)loc.y] + "?" + state.ToString());
        return resl;
    }

    bool NextToState(CState[,] grid, Vector2 loc, CState state, Vector2 gridSize){
        for(int x = -1; x <= 1; x++){
            for(int y = -1; y <= 1; y++){
                Vector2 testpos = new Vector2(loc.x + x, loc.y + y);
                if(!LocInGrid(testpos, gridSize)){
                    continue;
                }
                if(StateIs(grid, testpos, state, gridSize)){
                    return true;
                }
            }
        }
        return false;
    }

    bool NextToStates(CState[,] grid, Vector2 loc, List<CState> states, Vector2 gridSize){
        foreach (CState state in states){
            if(NextToState(grid, loc, state, gridSize)){
                //Debug.Log("NextToStates ret true: " + state.ToString());
                return true;
            }
        }
        //Debug.Log("NextToStates ret false");
        return false;
    }

    bool LocInGrid(Vector2 loc, Vector2 gridSize){
        bool resl = (loc.x >= 0) && (loc.y >= 0) && (loc.x < gridSize.x) && (loc.y < gridSize.y);
        //Debug.Log("LocInGrid returning: " + resl + ". " + loc.x.ToString() + "," + loc.y.ToString());
        return resl;
    }

    bool GridContainsState(CState[,] grid, CState state){
        int i = 0;
        foreach(CState s in grid){
            i++;
            if(s == state){
                //Debug.Log("GridContainsState ret true: " + state.ToString());
                return true;
            }
        }
        //Debug.Log("GridContainsState ret false: " + state.ToString());
        return false;
    }
}