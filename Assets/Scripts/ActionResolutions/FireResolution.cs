using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;
using PlayerActions;

public class FireResolution : ActionResolution
{
    //Public
    SpriteRenderer projectile;
    [Range(0.1f, 5)]
    public float duration;
    public float scaleMax;
    public float scaleMin;
    public AnimationCurve curve;
    public Vector3 startpos;
    public Vector3 endpos;
    public float delay;
    [Range(-180f, 180f)]
    public float rotation;
    //Private
    private ParticleSystem hitParticle;
    private ParticleSystem trailParticle;
    private float orig_z;
    private Vector3 orig_scale;
    private GameObject rock; // the rock that we want spinning
    private SpriteRenderer rockSpriteRend;
    

    void Awake(){
        //Init();
    }

    public override void Init(List<ActionReq> actions, PlayBoard2D pb, CellStruct[,] pGrid, CellStruct[,] eGrid){
        base.argInit( actions, pb, pGrid, eGrid);
        projectile = GetComponent<SpriteRenderer>();
        orig_z = transform.position.z;
        orig_scale = transform.localScale;
        hitParticle = transform.Find("HitParticle").GetComponent<ParticleSystem>();
        trailParticle = GetComponent<ParticleSystem>();
        rock = transform.Find("Rock").gameObject;
        rockSpriteRend = rock.GetComponent<SpriteRenderer>();
        ActionReq a = actions[0];
        GameGrid2D gg = a.t == pb.pobj.playerId ? pb.playerGrid : pb.enemyGrid; // Select grid to get coord's
        Cell2D cell = gg.GetCell(a.loc[0]);
        this.startpos = a.p == pb.pobj.playerId ? pb.playerShotOrig : pb.enemyShotOrig;
        this.transform.position = startpos;
        this.endpos = cell.transform.position;        
    }

    public override IEnumerator IEResolve(){
        running = true;
        Debug.Log("Starting Flight's IEResolve");
        float time = 0;
        //Delay first
        while(time<=delay){
            time += Time.deltaTime;
            yield return null;
        }
        time = 0; // Now launch
        trailParticle.Play(false);
        while(time <= duration){
            float interpolant = time/duration;
            //Set new scale
            float scale = Mathf.Lerp(scaleMin, scaleMax, curve.Evaluate(interpolant));
            Vector3 newscale = new Vector3(orig_scale.x * scale, orig_scale.y * scale, orig_scale.z); // Scale object up and down based on curve, multipled by height scalar
            transform.localScale = newscale;
            //Set new position
            Vector3 newpos = Vector3.Lerp(startpos, endpos, interpolant);
            newpos.z = orig_z;
            transform.position = newpos;
            //Update rotation
            rock.transform.Rotate(new Vector3(0,0,rotation* Time.deltaTime));
            //Update time and yield
            time += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Projectile has landed");
        //the projectile has landed, play sound, update cell, etc.
        ActionReq a = actions[0];
        GameGrid2D gg = a.t == pb.pobj.playerId ? pb.playerGrid : pb.enemyGrid; // Select grid to get coord's
        Cell2D cell = gg.GetCell(a.loc[0]);
        CellStruct cs = a.t == pb.pobj.playerId ? pGrid[(int)a.loc[0].x,(int)a.loc[0].y] : eGrid[(int)a.loc[0].x,(int)a.loc[0].y]; //TODO change this garbage
        cell.OnHit();
        cell.SetCellStruct(cs);
        running = false; // Say we're done running when we hit the end location
        StartCoroutine(IEFade());
    }
    
    IEnumerator IEFade(){
        Debug.Log("fire res IE Fade started");
        hitParticle.Play();
        yield return null; // wait one frame here so hit particles can spawn
        float fadeTime = 0.4f;
        float time = 0f;
        while(time < fadeTime){
            float alpha = Mathf.Lerp(1f,0f, time/fadeTime);
            Color c  = rockSpriteRend.color;
            c.a = alpha;
            rockSpriteRend.color = c;
            time += Time.deltaTime;
            yield return null;
        }
        while(hitParticle.particleCount != 0 || trailParticle.particleCount != 0){
            //Note, if this object keeps moving more particles will spawn and never bail out...
            yield return null;
        }
        Debug.Log("fire res IE Fade Done");
        Destroy(gameObject);
    }
}
