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

    public bool Validate(ActionReq ar, CellStruct[,] pGrid, CellStruct[,] eGrid, Vector2 gridSize, List<Vector2> capitolLocs){
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
                return BuildTowerValid(ar, pGrid, gridSize, capitolLocs);
            case pAction.buildWall:
            case pAction.placeMine:
            case pAction.buildDefenceGrid:
            case pAction.buildReflector:
                return DefBuildValid(ar, pGrid, gridSize);
            case pAction.fireBasic:
            case pAction.fireAgain:
            case pAction.fireRow:
            case pAction.fireSquare:
            case pAction.firePiercing:
            case pAction.towerTakeover:
                return FireValid(ar, gridSize);
            case pAction.scout:
                return ScoutValid(ar, eGrid, gridSize);
            case pAction.placeMole:
                return MoleValid(ar);
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
        bool resl = TargetsSelf(ar) && LocCountEq(ar, 1) && BldgIs(pGrid, ar.loc[0], CBldg.empty) && !Destroyed(pGrid, ar.loc[0]);
        //Debug.Log("DefBuildValid returning " + resl.ToString());
        return resl;
    }

    bool BuildTowerValid(ActionReq ar, CellStruct[,] pGrid, Vector2 gridSize, List<Vector2> capLocs){
        //Does the more complicated checking for valid tower placement
        List<CBldg> bldgs = new List<CBldg>{CBldg.towerOffence, CBldg.towerIntel, CBldg.towerDefence};
        bool resl = DefBuildValid(ar, pGrid, gridSize) && AdjacentToCountBldgs(pGrid, ar.loc[0], bldgs, gridSize, 1) && TowerPlacementOnEnd(pGrid, ar.loc[0], new List<Vector2>(), gridSize)
                && CheckChainAlive(pGrid, ar.loc[0], gridSize, capLocs) && CheckChainLengthValid(pGrid, ar.loc[0], gridSize, capLocs);
        //Debug.Log("BuildTowerValid returning " + resl.ToString());
        GetTowerChainsSunk(pGrid, gridSize, capLocs);
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
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1) && BldgIs(eGrid, ar.loc[0], CBldg.hidden);
        //Debug.Log("ScoutValid returned " + resl.ToString());
        return resl;
    }

    bool MoleValid(ActionReq ar){
        bool resl = !TargetsSelf(ar) && LocCountEq(ar, 1);
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

    public bool BldgIn(CellStruct[,] grid, Vector2 loc, List<CBldg> bldgs){
        bool resl = bldgs.Contains(grid[(int)loc.x, (int)loc.y].bldg);
        //Debug.Log("BldgIn returning: " + resl);
        return resl;
    }

    public bool BldgIs(CellStruct[,] grid, Vector2 loc, CBldg bldg){
        bool resl = grid[(int)loc.x, (int)loc.y].bldg == bldg;
        //Debug.Log("BldgIs returning: " + resl + ": " + grid[(int)loc.x, (int)loc.y] + "?" + bldg.ToString());
        return resl;
    }

    public bool Destroyed(CellStruct[,] grid, Vector2 loc){
        return grid[(int)loc.x, (int)loc.y].destroyed;
    }

    bool NextToBldg(CellStruct[,] grid, Vector2 loc, CBldg bldg, Vector2 gridSize){
        for(int x = -1; x <= 1; x++){
            for(int y = -1; y <= 1; y++){
                Vector2 testpos = new Vector2(loc.x + x, loc.y + y);
                if(!LocInGrid(testpos, gridSize)){
                    continue;
                }
                if(BldgIs(grid, testpos, bldg)){
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

    List<Vector2> GetAdjacentLocs(Vector2 loc, Vector2 gridSize){
        List<Vector2> l = new List<Vector2>();
        for(int x = -1; x<= 1; x += 2){ // Add x mod values
            Vector2 newpos = new Vector2(loc.x + x, loc.y);
            l.Add(newpos);
        }
        for(int y = -1; y <= 1; y += 2){ // Add y mod values
            Vector2 newpos = new Vector2(loc.x, loc.y + y);
            l.Add(newpos);
        }
        List<Vector2> resl = new List<Vector2>();
        foreach(Vector2 vec in l){
            if(LocInGrid(vec, gridSize)){
                resl.Add(vec);
            }
        }
        return resl;
    }

    //Check immediately left/right, up/down
    bool AdjacentToBldg(CellStruct [,] grid, Vector2 loc, CBldg bldg, Vector2 gridSize){
        bool resl = false;
        List<Vector2> adjLocs = GetAdjacentLocs(loc, gridSize);
        foreach(Vector2 vec in adjLocs){
            if(BldgIs(grid, loc, bldg)){
                resl = true;
            }
        }
        return resl;
    }

    //Return true if we're adjacent to any of the buildings in the list
    bool AdjacentToBldgsCellStruct(CellStruct [,] grid, Vector2 loc, List<CBldg> bldgs, Vector2 gridSize){
        bool resl = false;
        foreach(CBldg bldg in bldgs){
            if(AdjacentToBldg(grid, loc, bldg, gridSize)){
                resl = true;
            }
        }
        return resl;
    }

    //Return the count of matching buildings adjacent to us
    int CountBldgAdjacent(CellStruct [,] grid, Vector2 loc, CBldg bldg, Vector2 gridSize){
        //Debug.Log("CountBldgAdjacent of bldg: " + bldg.ToString());
        int count = 0;
        List<Vector2> adjLocs = GetAdjacentLocs(loc, gridSize);
        foreach(Vector2 vec in adjLocs){
            //Debug.Log("CountBldgAdjacent loc: " + vec.ToString());
            if(BldgIs(grid, vec, bldg)){
                count++;
            }
        }
        //Debug.Log("CountBldgAdjacent returning: " + count.ToString());
        return count;
    }

    //Given location and grid, are we adjacent to exactly N bldgs in given list?
    bool AdjacentToCountBldgs(CellStruct [,] grid, Vector2 loc, List<CBldg> bldgs, Vector2 gridSize, int inCount){
        int foundCount = 0;
        foreach(CBldg bldg in bldgs){
            foundCount += CountBldgAdjacent(grid, loc, bldg, gridSize);
        }
        bool resl = foundCount == inCount; 
        //Debug.Log("AdjacentToCountBldgs returning: " + resl + ". inCnt: " + inCount.ToString() + ". found: " + foundCount.ToString());
        return resl;
    }

    bool TowerPlacementOnEnd(CellStruct [,] pGrid, Vector2 loc, List<Vector2> ignoreLocs, Vector2 gridSize){
        //Recursively make sure that we don't ever have more than one adjacent building
        //Debug.Log(string.Format("TowerPlacementOnEnd: loc {0}", loc));
        List<CBldg> towers = new List<CBldg>{CBldg.towerOffence, CBldg.towerIntel, CBldg.towerDefence};
        bool resl = true;
        List<Vector2> adjTowers = GetAdjLocationsOfBldgs(pGrid, loc, towers, gridSize);
        adjTowers = adjTowers.Except(ignoreLocs).ToList();
        if(adjTowers.Count == 0){ // We're at the end of a line return true
            //Debug.Log("At end of line!");
            resl = true;
        }
        else if(adjTowers.Count == 1){ // We're stil. traveling along the tower group, recursive call on the next one
            //Debug.Log("Not at end yet!");
            ignoreLocs.Add(adjTowers[0]);
            resl = TowerPlacementOnEnd(pGrid, adjTowers[0], ignoreLocs, gridSize);
        }
        else if(adjTowers.Count > 1){ // Found position where there's two paths we can go, meaning placement is fucked. Return false
            //Debug.Log("Determined placement is bad");
            resl = false;
        }
        else{
            //Debug.LogError("Got here? count: " + adjTowers.Count);
        }
        return resl;
    }

    List<Vector2> GetAdjLocationsOfBldgs(CellStruct[,] grid, Vector2 loc, List<CBldg> bldgs, Vector2 gridSize){
        List<Vector2> resl = new List<Vector2>();
        List<Vector2> adjLocs = GetAdjacentLocs(loc, gridSize);
        foreach(Vector2 vec in adjLocs){
            if(BldgIn(grid, vec, bldgs)){
                resl.Add(vec);
            }
        }
        return resl;
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
    
    bool CheckChainAlive(CellStruct[,] pGrid, Vector2 loc, Vector2 gridSize, List<Vector2> capLocs){
        Dictionary<Vector2, bool> chainsAlive = this.GetTowerChainsAlive(pGrid, gridSize, capLocs);
        Vector2 chainAttached = GetChainAttached(pGrid, loc, gridSize, capLocs);
        bool resl = chainsAlive[chainAttached];
        Debug.Log("CheckChainAlive: returning: " + resl.ToString());
        return resl;
    }   

    bool CheckChainLengthValid(CellStruct[,] pGrid, Vector2 loc, Vector2 gridSize, List<Vector2> capLocs){
        int maxDiff = 3;
        Dictionary<Vector2, int> chainsLen = GetTowerChainsLength(pGrid, gridSize, capLocs);
        Dictionary<Vector2, bool> chainsAlive =  GetTowerChainsAlive(pGrid, gridSize, capLocs);
        Vector2 attachedCapitol = GetChainAttached(pGrid, loc, gridSize, capLocs);
        bool resl = true;
        foreach(Vector2 cap in capLocs){
            if(attachedCapitol != cap && chainsAlive[cap]){ // Check this only if not looking at the chain we're adding to and that the chain is not destroyed
                resl &= maxDiff >= (chainsLen[attachedCapitol] + 1 - chainsLen[cap]); // If we added 1 to this location's chain, would we be over our limit
            }
        }
        Debug.Log("CheckChainLengthValid: returning: " + resl.ToString());
        return resl;
    }

    //Return dict of capitol location and length of chain. Length of -1 if any destroyed tower attached
    Dictionary<Vector2, int> GetTowerChainsLength(CellStruct[,] pGrid, Vector2 gridSize, List<Vector2> capLocs){
        Dictionary<Vector2, List<Vector2>> towerChains = GetTowerChains(pGrid, gridSize, capLocs);
        Dictionary<Vector2, int> resl = new Dictionary<Vector2, int>();
        foreach(Vector2 cap in capLocs){
            resl.Add(cap, towerChains[cap].Count);
        }
        return resl;
    }

    //Recursively check to see if a towerchain is sunk// public as this is nice as a utility for playboard
    public Dictionary<Vector2, bool> GetTowerChainsSunk(CellStruct [,] pGrid, Vector2 gridSize, List<Vector2> capLocs){
        Dictionary<Vector2, List<Vector2>> towerChains = GetTowerChains(pGrid, gridSize, capLocs);
        Dictionary<Vector2, bool> resl = new Dictionary<Vector2, bool>();
        foreach(Vector2 cap in capLocs){
            resl.Add(cap, towerChains[cap].All(loc => pGrid[(int)loc.x, (int)loc.y].destroyed));
        }
        return resl;
    }

    //Recursively check to see if a towerchain is alive (no destroyed cells)
    Dictionary<Vector2, bool> GetTowerChainsAlive(CellStruct [,] pGrid, Vector2 gridSize, List<Vector2> capLocs){
        Dictionary<Vector2, List<Vector2>> towerChains = GetTowerChains(pGrid, gridSize, capLocs);
        Dictionary<Vector2, bool> resl = new Dictionary<Vector2, bool>();
        foreach(Vector2 cap in capLocs){
            resl.Add(cap, !towerChains[cap].Any(loc => pGrid[(int)loc.x, (int)loc.y].destroyed));
        }
        return resl;
    }

    Dictionary<Vector2, List<Vector2>> GetTowerChains(CellStruct[,] pGrid, Vector2 gridSize, List<Vector2> capLocs){
        List<CBldg> towers = new List<CBldg>{CBldg.towerOffence, CBldg.towerIntel, CBldg.towerDefence};
        Dictionary<Vector2, List<Vector2>> resl = new Dictionary<Vector2, List<Vector2>>();
        foreach(Vector2 loc in capLocs){
            //Look for ends and count on the way
            List<Vector2> chainLocs = new List<Vector2>(){loc};
            List<Vector2> adjTowers = GetAdjLocationsOfBldgs(pGrid, loc, towers, gridSize);
            if(adjTowers.Count > 2){
                Debug.LogError("This tower capitol has more than two offshoots, what do? Found: " + adjTowers.Count.ToString());
                return null;
            }
            foreach(Vector2 towerLoc in adjTowers){
                List<Vector2> ignoreLocs = new List<Vector2>(){loc};
                chainLocs.AddRange( _accumulateChainLocs(pGrid, towerLoc, gridSize, ignoreLocs));
                //Debug.Log("Calculated that chain at: " + loc.ToString() + " is length " + count.ToString());
            }
            resl.Add(loc, chainLocs);
        }
        return resl;
    }

    //Recursive function only to be used in GetTowerChains
    List<Vector2> _accumulateChainLocs(CellStruct [,] pGrid, Vector2 loc, Vector2 gridSize, List<Vector2> ignoreLocs){
        List<CBldg> towers = new List<CBldg>{CBldg.towerOffence, CBldg.towerIntel, CBldg.towerDefence};
        List<Vector2> adjTowers = GetAdjLocationsOfBldgs(pGrid, loc, towers, gridSize);
        adjTowers = adjTowers.Except(ignoreLocs).ToList();
        List<Vector2> outList = new List<Vector2>(){loc};
        if(adjTowers.Count == 0){ // We're at the end!
            Debug.Log("At the end of the chain, loc: " + loc.ToString());
        }
        else if(adjTowers.Count == 1){ //Not at end yet, recurse
            Debug.Log("Not the end of the chain, loc: " + loc.ToString() + ", next: " + adjTowers[0].ToString());
            ignoreLocs.Add(loc);
            outList.AddRange(_accumulateChainLocs(pGrid, adjTowers[0], gridSize, ignoreLocs));
        }
        else{
            Debug.LogError("Found that count of adjacent towers was > 2 while recursing? found: " + adjTowers.Count.ToString());
        }
        return outList;
    }

    //Given a location return the capitol of the chain that this would be attached to. Return Null if any errors
    //We assume that you've already checked that this location is at the end of a chain.
    //We just walk down the chain until we find a capitol location.
    Vector2 GetChainAttached(CellStruct[,] pGrid, Vector2 loc, Vector2 gridSize, List<Vector2> capLocs){
        List<CBldg> towers = new List<CBldg>{CBldg.towerOffence, CBldg.towerIntel, CBldg.towerDefence};
        Vector2 resl = new Vector2(-1,-1);
        List<Vector2> adjTowers = GetAdjLocationsOfBldgs(pGrid, loc, towers, gridSize);
        List<Vector2> ignoreLocs = new List<Vector2>(){loc};
        const int bailAt = 100;
        int count = 0; // Use this in case someone tries to use it incorrectly. Just bail if we loop 100 times
        while(adjTowers.Count == 1 && !capLocs.Contains(adjTowers[0]) && bailAt > count){
            count++;
            Vector2 temploc = adjTowers[0];
            adjTowers = GetAdjLocationsOfBldgs(pGrid, temploc, towers, gridSize).Except(ignoreLocs).ToList();
            adjTowers = adjTowers.Except(ignoreLocs).ToList();
            ignoreLocs.Add(temploc);
        }
        if(capLocs.Contains(adjTowers[0])){
            resl = adjTowers[0];
        }
        return resl;
    }
}