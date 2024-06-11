using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum EGameState{
    Load, //加载中
    Play, //游戏中
    Pause, //游戏暂停
    Fail, //游戏失败
}

public class GameManager : MonoBehaviour, IGameContext{
    public static GameManager Instance;

    [SerializeField] public Level level;

    //场景特效管理器
    [SerializeField] private SceneEffectManager sceneEffectManager;
    [SerializeField] private SceneCamera gameCamera;

    [SerializeField] private EGizmoDrawType drawType = EGizmoDrawType.None;
    //玩家
    Player player;

    EGameState gameState;
    public IEffectControl EffectControl{ get=>this.sceneEffectManager;}
    public ISoundControl SoundControl{ get; }

    private void Awake(){
        Instance = this;
        gameState = EGameState.Load;
        player = new Player(this);
    }

    IEnumerator Start(){
        yield return null;

        //加载玩家
        player.Reload(level.Bounds, level.StartPosition);
        this.gameState = EGameState.Play;
        yield return null;
    }

    public void Update(){
        float deltaTime = Time.unscaledDeltaTime;
        if (UpdateTime(deltaTime)){
            if (this.gameState == EGameState.Play){
                GameInput.Update(deltaTime);
                //更新玩家逻辑数据
                player.Update(deltaTime);
                //更新摄像机
                gameCamera.SetCameraPosition(player.GetCameraPosition());
            }
        }
    }

    #region 冻帧

    private float freezeTime;

    //更新顿帧数据，如果不顿帧，返回true
    public bool UpdateTime(float deltaTime){
        if (freezeTime > 0f){
            freezeTime = Mathf.Max(freezeTime - deltaTime, 0f);
            return false;
        }

        if (Time.timeScale == 0){
            Time.timeScale = 1;
        }

        return true;
    }

    //冻帧
    public void Freeze(float freezeTime){
        this.freezeTime = Mathf.Max(this.freezeTime, freezeTime);
        if (this.freezeTime > 0){
            Time.timeScale = 0;
        }
        else{
            Time.timeScale = 1;
        }
    }

    #endregion

#if UNITY_EDITOR
    void OnDrawGizmos(){
        if (player != null && drawType == EGizmoDrawType.Normal){
            player.playerController.Draw(EGizmoDrawType.Normal);
        }
    }
#endif
    

    public void CameraShake(Vector2 dir, float duration){
        this.gameCamera.Shake(dir, duration);
    }
}