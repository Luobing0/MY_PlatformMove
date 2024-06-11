using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseActionState{
    private Vector2 DashDir;
    private Vector2 beforeDashSpeed;

    public DashState( PlayerController context) : base(ActionState.Dash, context){ }

    public override ActionState Update(float deltaTime){
        //冲刺残影
        if (player.DashTrailTimer > 0){
            player.DashTrailTimer -= deltaTime;
            if (player.DashTrailTimer <= 0)
                player.PlayTrailEffect((int)player.Facing);
        }

        if (DashDir.y == 0){
            //Super Jump
            if (player.CanUnDuck && GameInput.Jump.Pressed() && player.JumpCheck.AllowJump()){
                player.SuperJump();
                return ActionState.Normal;
            }
        }

        //Super Wall Jump 蹭墙跳
        if (DashDir.x == 0 && DashDir.y == 1){
            //向上Dash情况下，检测SuperWallJump
            if (GameInput.Jump.Pressed() && player.CanUnDuck){
                if (player.WallJumpCheck(1)){
                    player.SuperWallJump(-1);
                    return ActionState.Normal;
                }
                else if (player.WallJumpCheck(-1)){
                    player.SuperWallJump(1);
                    return ActionState.Normal;
                }
            }
        }
        else{
            //Dash状态下执行WallJump，并切换到Normal状态
            if (GameInput.Jump.Pressed() && player.CanUnDuck){
                if (player.WallJumpCheck(1)){
                    player.WallJump(-1);
                    return ActionState.Normal;
                }
                else if (player.WallJumpCheck(-1)){
                    player.WallJump(1);
                    return ActionState.Normal;
                }
            }

            //凌波微步
            if (GameInput.Jump.Pressed() && player.OnGround){
                Debug.Log("凌波微步");
                player.SuperJump();
                return ActionState.Normal;
            }
        }

        return state;
    }

    public override IEnumerator Coroutine(){
        //return current  这个null表示一帧的时间
        yield return null;
        var dir = player.LastAim;
        var newSpeed = dir * Constants.DashSpeed;
        //惯性
        if (Math.Sign(beforeDashSpeed.x) == Math.Sign(newSpeed.x) &&
            Math.Abs(beforeDashSpeed.x) > Math.Abs(newSpeed.x)){
            newSpeed.x = beforeDashSpeed.x;
        }

        player.Speed = newSpeed;

        DashDir = dir;
        if (DashDir.x != 0)
            player.Facing = (Facings)Math.Sign(DashDir.x);

        player.PlayDashFluxEffect(DashDir, true);

        player.PlayDashEffect(player.Position, dir);
        player.SpriteControl.Slash(true);
        player.PlayTrailEffect((int)player.Facing);
        player.DashTrailTimer = .08f;
        yield return Constants.DashTime;

        player.SpriteControl.Slash(false);
        player.PlayTrailEffect((int)player.Facing);
        if (this.DashDir.y >= 0){
            player.Speed = DashDir * Constants.EndDashSpeed;
            //player.Speed.x *= swapCancel.X;
            //player.Speed.y *= swapCancel.Y;
        }

        if (player.Speed.y > 0)
            player.Speed.y *= Constants.EndDashUpMult;

        this.player.SetState((int)ActionState.Normal);
    }

    public override void OnBegin(){
        // Debug.Log("冲刺开始");
        if (!(player.OnGround && player.MoveY < 0)){
            player.MyAnimator.Play(player.DashID);
        }

        player.launched = false;
        //顿帧
        player.EffectControl.Freeze(0.05f);

        player.WallSlideTimer = Constants.WallSlideTime;
        player.DashCooldownTimer = Constants.DashCooldown;
        player.DashRefillCooldownTimer = Constants.DashRefillCooldown;
        beforeDashSpeed = player.Speed;
        player.Speed = Vector2.zero;
        DashDir = Vector2.zero;
        player.DashTrailTimer = 0;
        player.DashStartedOnGround = player.OnGround;

    }

    public override void OnEnd(){
        player.PlayDashFluxEffect(DashDir, false);
        
    }

    public override bool IsCoroutine(){
        return true;
    }
}