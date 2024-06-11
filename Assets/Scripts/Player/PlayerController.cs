using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class PlayerController{
    #region 变量

    private readonly int GroundMask;
    float varJumpTimer;
    float varJumpSpeed;
    int moveX;
    private float maxFall;
    private float fastMaxFall;

    //冲刺冷却时间计数器，为0时，可以再次冲刺
    private float dashCooldownTimer;

    //重新填充冲刺的倒计时
    private float dashRefillCooldownTimer;

    //冲刺次数
    public int dashes;

    //上一次冲刺
    public int lastDashes;

    private float wallSpeedRetentionTimer; // 如果你碰壁了，就启动这个计时器。 如果在此计时器内没有滑行，则保持水平速度
    private float wallSpeedRetained;

    private bool onGround;
    public bool OnGround => this.onGround;
    private bool wasOnGround;
    private Color groundColor = Color.white;
    public Color GroundColor => this.groundColor;

    public bool DashStartedOnGround{ get; set; }

    public int ForceMoveX{ get; set; }
    public float ForceMoveXTimer{ get; set; }

    public int HopWaitX; // 如果你爬到一个移动的固体上，请捕捉到它旁边，直到你到达它上方
    public float HopWaitXSpeed;

    public bool launched;
    public float launchedTimer;
    public float WallSlideTimer{ get; set; } = Constants.WallSlideTime;
    public int WallSlideDir{ get; set; }
    public JumpCheck JumpCheck{ get; set; } //土狼时间
    public WallBoost WallBoost{ get; set; } //墙面反弹力
    private FiniteStateMachine<BaseActionState> stateMachine;

    public Animator MyAnimator;
    public ISpriteControl SpriteControl{ get; private set; }

    //特效控制器
    public IEffectControl EffectControl{ get; private set; }

    //音效控制器
    public ISoundControl SoundControl{ get; private set; }
    public ICamera camera{ get; private set; }

    public bool CanDash{
        get{ return GameInput.Dash.Pressed() && dashCooldownTimer <= 0 && this.dashes > 0; }
    }

    public float WallSpeedRetentionTimer{
        get{ return this.wallSpeedRetentionTimer; }
        set{ this.wallSpeedRetentionTimer = value; }
    }

    public Vector2 Speed;

    public object Holding => null;

    public Vector2 Position{ get; private set; }

    //表示进入爬墙状态有0.1秒时间,不发生移动，为了让玩家看清发生了爬墙的动作
    public float ClimbNoMoveTimer{ get; set; }
    public float VarJumpSpeed => this.varJumpSpeed;

    public float VarJumpTimer{
        get{ return this.varJumpTimer; }
        set{ this.varJumpTimer = value; }
    }

    public int MoveX => moveX;
    public int MoveY => Math.Sign(Input.GetAxisRaw("Vertical"));

    public float MaxFall{
        get => maxFall;
        set => maxFall = value;
    }

    public float DashCooldownTimer{
        get => dashCooldownTimer;
        set => dashCooldownTimer = value;
    }

    public float DashRefillCooldownTimer{
        get => dashRefillCooldownTimer;
        set => dashRefillCooldownTimer = value;
    }

    public Vector2 LastAim{ get; set; }
    public Facings Facing{ get; set; } //当前朝向

    public bool Ducking{
        get{ return this.collider == this.duckHitbox || this.collider == this.duckHurtbox; }
        set{
            if (value){
                this.collider = this.duckHitbox;
                return;
            }
            else{
                this.collider = this.normalHitbox;
            }
            //播放下蹲动画
            PlayDuck(value);
        }
    } //检测当前是否可以站立

    public bool CanUnDuck{
        get{
            if (!Ducking)
                return true;
            Rect lastCollider = this.collider;
            this.collider = normalHitbox;
            bool noCollide = !CollideCheck(this.Position, Vector2.zero);
            this.collider = lastCollider;
            return noCollide;
        }
    }

    #endregion

    public bool RefillDash(){
        if (this.dashes < Constants.MaxDashes){
            this.dashes = Constants.MaxDashes;
            return true;
        }
        else
            return false;
    }

    public ActionState Dash(){
        //wasDashB = Dashes == 2;
        this.dashes = Math.Max(0, this.dashes - 1);
        GameInput.Dash.ConsumeBuffer();
        return ActionState.Dash;
    }

    public void SetState(int state){
        this.stateMachine.State = state;
    }

    public PlayerController(ISpriteControl spriteControl, IEffectControl effectControl,Animator animator){
        this.SpriteControl = spriteControl;
        this.EffectControl = effectControl;
        this.MyAnimator = animator;

        this.stateMachine = new FiniteStateMachine<BaseActionState>((int)ActionState.Size);
        this.stateMachine.AddState(new NormalState(this));
        this.stateMachine.AddState(new DashState(this));
        this.stateMachine.AddState(new ClimbState(this));
        this.GroundMask = LayerMask.GetMask("Ground");

        this.Facing = Facings.Right;
        this.LastAim = Vector2.right;
    }

    //设置对应的能力
    public void RefreshAbility(){
        JumpCheck = new JumpCheck(this, Constants.EnableJumpGrace);
        if (!Constants.EnableWallBoost){
            this.WallBoost = null;
        }
        else{
            this.WallBoost = this.WallBoost == null ? new WallBoost(this) : this.WallBoost;
        }
    }

    //初始化
    public void Init(Bounds bounds, Vector2 startPosition){
        stateMachine.State = (int)ActionState.Normal;
        lastDashes = dashes = 1;
        Position = startPosition;
        collider = normalHitbox;

        SpriteControl.SetSpriteScale(NORMAL_SPRITE_SCALE);
        this.bounds = bounds;
        this.cameraPosition = CameraTarget;
    }

    public void Update(float deltaTime){
        //更新各个组件中变量的状态
        {
            //Get ground
            wasOnGround = onGround;
            if (Speed.y <= 0){
                this.onGround = CheckGround(); //碰撞检测地面
            }
            else{
                this.onGround = false;
            }

            //Wall Slide
            if (this.WallSlideDir != 0){
                this.WallSlideTimer = Math.Max(this.WallSlideTimer - deltaTime, 0);
                this.WallSlideDir = 0;
            }

            if (this.onGround && this.stateMachine.State != (int)ActionState.Climb){
                this.WallSlideTimer = Constants.WallSlideTime;
            }

            //Wall Boost, 不消耗体力WallJump
            this.WallBoost?.Update(deltaTime);
            //跳跃检查
            this.JumpCheck?.Update(deltaTime);

            //Dash
            {
                if (dashCooldownTimer > 0)
                    dashCooldownTimer -= deltaTime;
                if (dashRefillCooldownTimer > 0){
                    dashRefillCooldownTimer -= deltaTime;
                }
                else if (onGround){
                    RefillDash();
                }
            }

            //Var Jump
            if (varJumpTimer > 0){
                varJumpTimer -= deltaTime;
            }

            //Force Move X
            if (ForceMoveXTimer > 0){
                ForceMoveXTimer -= deltaTime;
                this.moveX = ForceMoveX;
            }
            else{
                //输入
                this.moveX = Math.Sign(UnityEngine.Input.GetAxisRaw("Horizontal"));
            }

            //Facing
            if (moveX != 0 && this.stateMachine.State != (int)ActionState.Climb){
                Facing = (Facings)moveX;
            }

            //Aiming
            LastAim = GameInput.GetAimVector(Facing);

            //撞墙以后的速度保持，Wall Speed Retention，用于撞开
            if (wallSpeedRetentionTimer > 0){
                if (Math.Sign(Speed.x) == -Math.Sign(wallSpeedRetained))
                    wallSpeedRetentionTimer = 0;
                else if (!CollideCheck(Position, Vector2.right * Math.Sign(wallSpeedRetained))){
                    Speed.x = wallSpeedRetained;
                    wallSpeedRetentionTimer = 0;
                }
                else
                    wallSpeedRetentionTimer -= deltaTime;
            }

            //Hop Wait X
            if (this.HopWaitX != 0){
                if (Math.Sign(Speed.x) == -HopWaitX || Speed.y < 0)
                    this.HopWaitX = 0;
                else if (!CollideCheck(Position, Vector2.right * this.HopWaitX)){
                    Speed.x = this.HopWaitXSpeed;
                    this.HopWaitX = 0;
                }
            }

            //Launch Particles
            if (launched){
                var sq = Speed.SqrMagnitude();
                if (sq < Constants.LaunchedMinSpeedSq)
                    launched = false;
                else{
                    var was = launchedTimer;
                    launchedTimer += deltaTime;

                    if (launchedTimer >= .5f){
                        launched = false;
                        launchedTimer = 0;
                    }
                    else if (Calculate.OnInterval(launchedTimer, was, 0.25f)){
                        EffectControl.SpeedRing(this.Position, this.Speed.normalized);
                    }
                }
            }
            else
                launchedTimer = 0;
        }
        //状态机更新逻辑
        stateMachine.Update(deltaTime);
        //更新位置
        UpdateMoveX(Speed.x * deltaTime);
        UpdateMoveY(Speed.y * deltaTime);

        UpdateHair(deltaTime);

        UpdateCamera(deltaTime);
    }

    //处理跳跃,跳跃时候，会给跳跃前方一个额外的速度
    public void Jump(){
        GameInput.Jump.ConsumeBuffer();
        this.JumpCheck?.ResetTime();
        this.WallSlideTimer = Constants.WallSlideTime;
        this.WallBoost?.ResetTime();
        this.varJumpTimer = Constants.VarJumpTime;
        this.Speed.x += Constants.JumpHBoost * moveX;
        this.Speed.y = Constants.JumpSpeed;
        this.varJumpSpeed = this.Speed.y;

        this.PlayJumpEffect(SpritePosition, Vector2.up);
    }

    //SuperJump，表示在地面上或者土狼时间内，Dash接跳跃。
    //数值方便和Jump类似，数值变大。
    //蹲伏状态的SuperJump需要额外处理。
    //Dash->Jump->Dush
    public void SuperJump(){
        // Debug.Log("超级跳！");
        GameInput.Jump.ConsumeBuffer();
        this.JumpCheck?.ResetTime();
        varJumpTimer = Constants.VarJumpTime;
        this.WallSlideTimer = Constants.WallSlideTime;
        this.WallBoost?.ResetTime();

        this.Speed.x = Constants.SuperJumpH * (int)Facing;
        this.Speed.y = Constants.JumpSpeed;
        //Speed += LiftBoost;
        if (Ducking){
            Ducking = false;
            this.Speed.x *= Constants.DuckSuperJumpXMult;
            this.Speed.y *= Constants.DuckSuperJumpYMult;
        }

        varJumpSpeed = Speed.y;
        //TODO 考虑电梯对速度的加成
        launched = true;

        this.PlayJumpEffect(this.SpritePosition, Vector2.up);
    }

    //在墙边情况下的，跳跃。主要需要考虑当前跳跃朝向
    public void WallJump(int dir){
        GameInput.Jump.ConsumeBuffer();
        Ducking = false;
        this.JumpCheck?.ResetTime();
        varJumpTimer = Constants.VarJumpTime;
        this.WallSlideTimer = Constants.WallSlideTime;
        this.WallBoost?.ResetTime();
        if (moveX != 0){
            this.ForceMoveX = dir;
            this.ForceMoveXTimer = Constants.WallJumpForceTime;
        }

        Speed.x = Constants.WallJumpHSpeed * dir;
        Speed.y = Constants.JumpSpeed;
        //TODO 考虑电梯对速度的加成
        //Speed += LiftBoost;
        varJumpSpeed = Speed.y;

        //墙壁粒子效果。
        if (dir == -1)
            this.PlayJumpEffect(this.RightPosition, Vector2.left);
        else
            this.PlayJumpEffect(this.LeftPosition, Vector2.right);
    }

    public void ClimbJump(){
        Debug.Log("WallBoost");
        if (!onGround){
            //Stamina -= ClimbJumpCost;

            //sweatSprite.Play("jump", true);
            //Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        }

        Jump();
        WallBoost?.Active();
    }

    //在墙边Dash时，当前按住上，不按左右时，执行SuperWallJump
    public void SuperWallJump(int dir){
        Debug.Log("蹭墙跳");
        GameInput.Jump.ConsumeBuffer();
        Ducking = false;
        this.JumpCheck?.ResetTime();
        varJumpTimer = Constants.SuperWallJumpVarTime;
        this.WallSlideTimer = Constants.WallSlideTime;
        this.WallBoost?.ResetTime();

        Speed.x = Constants.SuperWallJumpH * dir;
        Speed.y = Constants.SuperWallJumpSpeed;
        //Speed += LiftBoost;
        varJumpSpeed = Speed.y;
        launched = true;

        if (dir == -1)
            this.PlayJumpEffect(this.RightPosition, Vector2.left);
        else
            this.PlayJumpEffect(this.LeftPosition, Vector2.right);
    }
    
    public bool IsFall
    {
        get
        {
            return !this.wasOnGround && this.OnGround;
        }
    }
}