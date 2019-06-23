using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CellTypes;
using PlayerActions;

public class FireMultiRes : ActionResolution
{
    public float delay;
    public GameObject fireRes;
    List<FireResolution> shots = new List<FireResolution>();

    public override void Init(List<ActionReq> actions, PlayBoard2D pb, CellStruct[,] pGrid, CellStruct[,] eGrid){
        base.argInit( actions, pb, pGrid, eGrid);
        float setDelay = 0;
        foreach(ActionReq ar in actions){
            GameObject go = Instantiate(fireRes);
            FireResolution fres = go.GetComponent<FireResolution>();
            fres.Init(new List<ActionReq>(){ar}, pb, pGrid, eGrid);
            fres.delay = setDelay;
            setDelay += delay;
            shots.Add(fres);
        }
    }

    public override IEnumerator IEResolve(){
        running = true;
        //Launch each shot
        foreach(FireResolution fres in shots){
            StartCoroutine(fres.IEResolve());
        }
        int seconds = 20; // Safety max out on IE
        while(seconds-- != 0){
            if(shots.All(res=>!res.running)){
                break; // All are done running
            }
            yield return new WaitForSeconds(1);
        }
        Debug.Log("Done with firemulti Res");
        running = false;
        Destroy(this);
    }
}
