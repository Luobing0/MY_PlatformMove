using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class PlayerController{
    #region ����

    private readonly int GroundMask;
    float varJumpTimer;
    float varJumpSpeed;
    int moveX;
    private float maxFall;
    private float fastMaxFall;

    //�����ȴʱ���������Ϊ0ʱ�������ٴγ��
    private float dashCooldownTimer;

    //��������̵ĵ���ʱ
    private float dashRefillCooldownTimer;

    //��̴���
    public int dashes;

    //��һ�γ��
    public int lastDashes;

    private float wallSpeedRetentionTimer; // ����������ˣ������������ʱ���� ����ڴ˼�ʱ����û�л��У��򱣳�ˮƽ�ٶ�
    private float wallSpeedRetained;

    private bool onGround;
    public bool OnGround => this.onGround;
    private bool wasOnGround;
    private Color groundColor = Color.white;
    public Color GroundColor => this.groundColor;

    public bool DashStartedOnGround{ get; set; }

    public int ForceMoveX{ get; set; }
    public float ForceMoveXTimer{ get; set; }

    public int HopWaitX; // ���������һ���ƶ��Ĺ����ϣ��벶׽�����Աߣ�ֱ���㵽�����Ϸ�
    public float HopWaitXSpeed;

    public bool launched;
    public float launchedTimer;
    public float WallSlideTimer{ get; set; } = Constants.WallSlideTime;
    public int WallSlideDir{ get; set; }
    public JumpCheck JumpCheck{ get; set; } //����ʱ��
    public WallBoost WallBoost{ get; set; } //ǽ�淴����
    private FiniteStateMachine<BaseActionState> stateMachine;

    public Animator MyAnimator;
    public ISpriteControl SpriteControl{ get; private set; }

    //��Ч������
    public IEffectControl EffectControl{ get; private set; }

    //��Ч������
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

    //��ʾ������ǽ״̬��0.1��ʱ��,�������ƶ���Ϊ������ҿ��巢������ǽ�Ķ���
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
    public Facings Facing{ get; set; } //��ǰ����

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
            //�����¶׶���
            PlayDuck(value);
        }
    } //��⵱ǰ�Ƿ����վ��

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

    //���ö�Ӧ������
    public void RefreshAbility(){
        JumpCheck = new JumpCheck(this, Constants.EnableJumpGrace);
        if (!Constants.EnableWallBoost){
            this.WallBoost = null;
        }
        else{
            this.WallBoost = this.WallBoost == null ? new WallBoost(this) : this.WallBoost;
        }
    }

    //��ʼ��
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
        //���¸�������б�����״̬
        {
            //Get ground
            wasOnGround = onGround;
            if (Speed.y <= 0){
                this.onGround = CheckGround(); //��ײ������
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

            //Wall Boost, ����������WallJump
            this.WallBoost?.Update(deltaTime);
            //��Ծ���
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
                //����
                this.moveX = Math.Sign(UnityEngine.Input.GetAxisRaw("Horizontal"));
            }

            //Facing
            if (moveX != 0 && this.stateMachine.State != (int)ActionState.Climb){
                Facing = (Facings)moveX;
            }

            //Aiming
            LastAim = GameInput.GetAimVector(Facing);

            //ײǽ�Ժ���ٶȱ��֣�Wall Speed Retention������ײ��
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
        //״̬�������߼�
        stateMachine.Update(deltaTime);
        //����λ��
        UpdateMoveX(Speed.x * deltaTime);
        UpdateMoveY(Speed.y * deltaTime);

        UpdateHair(deltaTime);

        UpdateCamera(deltaTime);
    }

    //������Ծ,��Ծʱ�򣬻����Ծǰ��һ��������ٶ�
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

    //SuperJump����ʾ�ڵ����ϻ�������ʱ���ڣ�Dash����Ծ��
    //��ֵ�����Jump���ƣ���ֵ���
    //�׷�״̬��SuperJump��Ҫ���⴦��
    //Dash->Jump->Dush
    public void SuperJump(){
        // Debug.Log("��������");
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
        //TODO ���ǵ��ݶ��ٶȵļӳ�
        launched = true;

        this.PlayJumpEffect(this.SpritePosition, Vector2.up);
    }

    //��ǽ������µģ���Ծ����Ҫ��Ҫ���ǵ�ǰ��Ծ����
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
        //TODO ���ǵ��ݶ��ٶȵļӳ�
        //Speed += LiftBoost;
        varJumpSpeed = Speed.y;

        //ǽ������Ч����
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

    //��ǽ��Dashʱ����ǰ��ס�ϣ���������ʱ��ִ��SuperWallJump
    public void SuperWallJump(int dir){
        Debug.Log("��ǽ��");
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