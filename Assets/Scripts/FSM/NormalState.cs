using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class NormalState : BaseActionState{
    public NormalState(PlayerController controller) : base(ActionState.Normal, controller){ }

    public override ActionState Update(float deltaTime){
        if (GameInput.Grab.Checked() && !player.Ducking){
            if (player.Speed.y <= 0 && Math.Sign(player.Speed.x) != (int)player.Facing){
                if (player.ClimbCheck((int)player.Facing)){
                    player.Ducking = false;
                    return ActionState.Climb;
                }

                //非下坠情况，需要考虑向上攀爬吸附
                if (player.MoveY > -1){
                    bool snapped = player.ClimbUpSnap();
                    if (snapped){
                        player.Ducking = false;
                        return ActionState.Climb;
                    }
                }
            }
        }

        //冲刺
        if (player.CanDash){
            return player.Dash();
        }

        //下蹲
        if (player.Ducking){
            if (player.OnGround && player.MoveY != -1){
                if (player.CanUnDuck){
                    player.Ducking = false;
                }
                else if (player.Speed.x == 0){
                    //根据角落位置，进行挤出操作
                }
            }
        }
        else if (player.OnGround && player.MoveY == -1 && player.Speed.y <= 0){
            player.Ducking = true;
            player.PlayDuck(true);
        }

        //水平面上移动,计算阻力
        if (player.Ducking && player.OnGround){
            player.Speed.x = Mathf.MoveTowards(player.Speed.x, 0, Constants.DuckFriction * deltaTime);
        }
        else{
            float mult = player.OnGround ? 1 : Constants.AirMult;
            //计算水平速度
            float max = player.Holding == null ? Constants.MaxRun : Constants.HoldingMaxRun;
            if (Mathf.Abs(player.Speed.x) > max && Mathf.Sign(player.Speed.x) == player.MoveX){
                //同方向加速
                player.Speed.x = Mathf.MoveTowards(player.Speed.x, max * this.player.MoveX,
                    Constants.RunReduce * mult * Time.deltaTime);
            }
            else{
                //反方向减速
                player.Speed.x = Mathf.MoveTowards(player.Speed.x, max * this.player.MoveX,
                    Constants.RunAccel * mult * Time.deltaTime);
            }
        }
        

        //计算竖直速度
        {
            //计算最大下落速度
            {
                float maxFallSpeed = Constants.MaxFall;
                float fastMaxFallSpeed = Constants.FastMaxFall;
                if (this.player.MoveY == -1 && this.player.Speed.y <= maxFallSpeed){
                    this.player.MaxFall = Mathf.MoveTowards(this.player.MaxFall, fastMaxFallSpeed,
                        Constants.FastMaxAccel * deltaTime);

                    //处理表现
                    this.player.PlayFallEffect(player.Speed.y);
                }
                else{
                    this.player.MaxFall = Mathf.MoveTowards(this.player.MaxFall, maxFallSpeed,
                        Constants.FastMaxAccel * deltaTime);
                }
            }

            if (!player.OnGround){
                float max = this.player.MaxFall; //最大下落速度
                //Wall Slide
                if ((player.MoveX == (int)player.Facing || (player.MoveX == 0 && GameInput.Grab.Checked())) &&
                    player.MoveY != -1){
                    //判断是否向下做Wall滑行
                    if (player.Speed.y <= 0 && player.WallSlideTimer > 0 &&
                        player.ClimbBoundsCheck((int)player.Facing) &&
                        player.CollideCheck(player.Position, Vector2.right * (int)player.Facing) && player.CanUnDuck){
                        player.Ducking = false;
                        player.WallSlideDir = (int)player.Facing;
                    }

                    if (player.WallSlideDir != 0){
                        //if (player.WallSlideTimer > Constants.WallSlideTime * 0.5f && ClimbBlocker.Check(level, this, Position + Vector2.UnitX * wallSlideDir))
                        //    player.WallSlideTimer = Constants.WallSlideTime * .5f;

                        max = Mathf.Lerp(Constants.MaxFall, Constants.WallSlideStartMax,
                            player.WallSlideTimer / Constants.WallSlideTime);
                        if ((player.WallSlideTimer / Constants.WallSlideTime) > .65f){
                            //播放滑行特效
                            player.PlayWallSlideEffect(Vector2.right * player.WallSlideDir);
                        }
                    }
                }

                float mult = (Math.Abs(player.Speed.y) < Constants.HalfGravThreshold && (GameInput.Jump.Checked()))
                    ? .5f
                    : 1f;
                //空中的情况,需要计算Y轴速度
                player.Speed.y = Mathf.MoveTowards(player.Speed.y, max, Constants.Gravity * mult * deltaTime);
            }

            //处理跳跃
            if (player.VarJumpTimer > 0){
                if (GameInput.Jump.Checked()){
                    //如果按住跳跃，则跳跃速度不受重力影响。
                    player.Speed.y = Math.Max(player.Speed.y, player.VarJumpSpeed);
                }
                else
                    player.VarJumpTimer = 0;
            }
        }

        if (GameInput.Jump.Pressed()){
            //土狼时间范围内,允许跳跃
            if (this.player.JumpCheck.AllowJump()){
                this.player.Jump();
            }
            else if (player.CanUnDuck){
                //如果右侧有墙
                if (player.WallJumpCheck(1)){
                    if (player.Facing == Facings.Right && GameInput.Grab.Checked())
                        player.ClimbJump();
                    else
                        player.WallJump(-1);
                }
                //如果左侧有墙
                else if (player.WallJumpCheck(-1)){
                    if (player.Facing == Facings.Left && GameInput.Grab.Checked())
                        player.ClimbJump();
                    else
                        player.WallJump(1);
                }
            }
        }

        if (player.OnGround){
            if (player.Speed.x == 0 && player.MoveY == 0){
                this.player.MyAnimator.Play(player.IdleID);
            }
            else if(player.Speed.x != 0){
                this.player.MyAnimator.Play(player.RunID);
            }
            else if ( player.MoveY == -1 && player.Speed.y <= 0){
                this.player.MyAnimator.Play(player.DuckID);
            }
            else{
                this.player.MyAnimator.Play(player.IdleID);
            }
        }
        else{
            this.player.MyAnimator.Play(player.JumpID);
        }
        

        return ActionState.Normal;
    }

    public override IEnumerator Coroutine(){
        throw new System.NotImplementedException();
    }

    public override void OnBegin(){
        this.player.MaxFall = Constants.MaxFall;
    }

    public override void OnEnd(){
        this.player.WallBoost?.ResetTime();
        this.player.WallSpeedRetentionTimer = 0;
        this.player.HopWaitX = 0;
    }

    public override bool IsCoroutine(){
        return false;
    }
}