using UnityEngine;

public class Player{
    private PlayerRenderer playerRenderer;
    public PlayerController playerController;

    private IGameContext gameContext;
    private bool lastFrameOnGround;

    public Player(IGameContext gameContext){
        this.gameContext = gameContext;
    }

    /// <summary>
    /// 加载玩家
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="startPos"></param>
    public void Reload(Bounds bounds, Vector2 startPos){
        playerRenderer = Object.Instantiate(Resources.Load<PlayerRenderer>("PlayerRenderer"));
        playerRenderer.Reload();
        Animator animator = playerRenderer.GetComponentInChildren<Animator>();

        //初始化
        playerController = new PlayerController(playerRenderer, gameContext.EffectControl, animator);
        playerController.Init(bounds, startPos);


        PlayerParams playerParams = Resources.Load<PlayerParams>("Config/PlayerParams");
        playerParams.SetReloadCallback(() => this.playerController.RefreshAbility());
        playerParams.ReloadParams();
    }

    public void Update(float deltaTime){
        playerController.Update(deltaTime);
        Render();
    }

    private void Render(){
        playerRenderer.Render(Time.deltaTime);

        Vector2 scale = playerRenderer.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (int)playerController.Facing;
        playerRenderer.transform.localScale = scale;
        playerRenderer.transform.position = playerController.Position;

        //if (!lastFrameOnGround && this.playerController.OnGround)
        //{
        //    this.playerRenderer.PlayMoveEffect(true, this.playerController.GroundColor);
        //}
        //else if (lastFrameOnGround && !this.playerController.OnGround)
        //{
        //    this.playerRenderer.PlayMoveEffect(false, this.playerController.GroundColor);
        //}
        //this.playerRenderer.UpdateMoveEffect();

        this.lastFrameOnGround = this.playerController.OnGround;
    }


    /// <summary>
    /// 更新摄像机
    /// </summary>
    /// <returns></returns>
    public Vector2 GetCameraPosition(){
        if (this.playerController == null){
            return Vector3.zero;
        }

        return playerController.GetCameraPosition();
    }

    
}