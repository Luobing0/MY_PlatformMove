using System;
using System.Collections;
using UnityEngine;

public class ClimbState : BaseActionState{
    public ClimbState( PlayerController context) : base(ActionState.Climb, context){ }

    public override ActionState Update(float deltaTime){
        player.ClimbNoMoveTimer -= deltaTime;
        //跳跃
        if (GameInput.Jump.Pressed() && (!player.Ducking || player.CanUnDuck)){
            if (player.MoveX == -(int)player.Facing){
                player.WallJump(-(int)player.Facing);
            }
            else{
                player.ClimbJump();
            }

            return ActionState.Normal;
        }

        if (player.CanDash){
            return player.Dash();
        }

        //放开抓取键,则回到Normal状态
        if (!GameInput.Grab.Checked()){
            //Speed += LiftBoost;
            //Play(Sfxs.char_mad_grab_letgo);
            return ActionState.Normal;
        }

        //检测前面的墙面是否存在
        if (!player.CollideCheck(player.Position, Vector2.right * (int)player.Facing)){
            //Climbed over ledge?
            if (player.Speed.y < 0){
                ClimbHop(); //自动翻越墙面
            }
            return ActionState.Normal;
        }

        {
            //攀爬
            float target = 0;
            bool trySlip = false;
            if (player.ClimbNoMoveTimer <= 0){
                if (false) //(ClimbBlocker.Check(Scene, this, Position + Vector2.UnitX * (int)Facing))  
                {
                    //trySlip = true;
                }
                else if (player.MoveY == 1){
                    //往上爬
                    target = Constants.ClimbUpSpeed;
                    //向上攀爬的移动限制,顶上有碰撞或者SlipCheck
                    if (player.CollideCheck(player.Position, Vector2.up)){
                        player.Speed.y = Mathf.Min(player.Speed.y, 0);
                        target = 0;
                        trySlip = true;
                    }
                    //如果在上面0.6米处存在障碍，且前上方0.1米处没有阻碍，依然不允许向上
                    else if (player.ClimbHopBlockedCheck() && player.SlipCheck(0.1f)){
                        player.Speed.y = Mathf.Min(player.Speed.y, 0);
                        target = 0;
                        trySlip = true;
                    }
                    //如果前上方没有阻碍, 则进行ClimbHop
                    else if (player.SlipCheck()){
                        //Hopping
                        ClimbHop();
                        return ActionState.Normal;
                    }
                }
                else if (player.MoveY == -1){
                    //往下爬
                    target = Constants.ClimbDownSpeed;
                    if (player.OnGround){
                        player.Speed.y = Mathf.Max(player.Speed.y, 0); //落地时,Y轴速度>=0
                        target = 0;
                    }
                    else{
                        //创建WallSlide粒子效果
                        player.PlayWallSlideEffect(Vector2.right * (int)player.Facing);
                    }
                }
                else{
                    trySlip = true;
                }
            }
            else{
                trySlip = true;
            }

            //滑行
            if (trySlip && player.SlipCheck()){
                target = Constants.ClimbSlipSpeed;
            }

            player.Speed.y = Mathf.MoveTowards(player.Speed.y, target, Constants.ClimbAccel * deltaTime);
        }
        //TrySlip导致的下滑在碰到底部的时候,停止下滑
        if (player.MoveY != -1 && player.Speed.y < 0 && !player.CollideCheck(player.Position, new Vector2((int)player.Facing, -1))){
            player.Speed.y = 0;
        }

        //TODO Stamina 耐力
        return state;
    }


    //自动翻阅墙面
    private void ClimbHop(){
        //获取目标的落脚点
        bool hit = player.CollideCheck(player.Position, Vector2.right * (int)player.Facing);
        if (hit){
            player.HopWaitX = (int)player.Facing;
            player.HopWaitXSpeed = (int)player.Facing * Constants.ClimbHopX;
        }
        else{
            player.HopWaitX = 0;
            player.Speed.x = (int)player.Facing * Constants.ClimbHopX;
        }

        player.Speed.y = MathF.Max(player.Speed.y, Constants.ClimbHopY);
        player.ForceMoveX = 0;
        player.ForceMoveXTimer = Constants.ClimbHopForceTime;
    }

    public override IEnumerator Coroutine(){
        throw new System.NotImplementedException();
    }

    public override void OnBegin(){
        player.MyAnimator.Play(player.ClimbID);
        
        player.Speed.x = 0;
        player.Speed.y *= Constants.ClimbGrabYMult;
        //TODO 其他参数
        player.WallSlideTimer = Constants.WallSlideTime;
        player.WallBoost?.ResetTime();
        player.ClimbNoMoveTimer = Constants.ClimbNoMoveTime;

        //两个像素的吸附功能
        player.ClimbSnap();
        //TODO 表现
    }

    public override void OnEnd(){
        //TODO 
    }

    public override bool IsCoroutine(){
        return false;
    }
}