using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;

public partial class PlayerController{
    const float STEP = 0.1f; //碰撞检测步长，对POINT检测用
    const float DEVIATION = 0.02f; //碰撞检测误差

    private readonly Rect normalHitbox = new Rect(0, 0, 0.8f, 1.1f);
    private readonly Rect duckHitbox = new Rect(0, -0.25f, 0.8f, 0.6f); //蹲下box
    private readonly Rect normalHurtbox = new Rect(0f, -0.15f, 0.8f, 0.9f);
    private readonly Rect duckHurtbox = new Rect(8f, 4f, 0.8f, 0.4f);

    public Rect collider;

    public void AdjustPosition(Vector2 adjust){
        UpdateMoveX(adjust.x);
        UpdateMoveY(adjust.y);
    }

    //碰撞检测


    //更新每帧的X方向的位置
    protected void UpdateMoveX(float disX){
        float distance = disX;
        int correctTimes = 1;
        while (true){
            float moved = MoveXStepWithCollide(distance);
            //无碰撞退出循环
            this.Position += Vector2.right * moved;
            if (moved == distance || correctTimes == 0){
                //无碰撞，且校正次数为0
                break;
            }

            float tempDist = distance - moved;
            correctTimes--;
            if (!CorrectX(tempDist)){
                this.Speed.x = 0; //未完成校正，则速度清零
                //速度保持
                if (wallSpeedRetentionTimer <= 0){
                    wallSpeedRetained = this.Speed.x;
                    wallSpeedRetentionTimer = Constants.WallSpeedRetentionTime;
                }

                break;
            }

            distance = tempDist;
        }
    }

    //更新每帧的Y方向的位置
    protected void UpdateMoveY(float distY){
        Vector2 targetPosition = this.Position;
        //使用校正
        float distance = distY;
        int correctTimes = 1; //默认可以迭代位置10次
        bool collided = true;
        float speedY = Mathf.Abs(this.Speed.y);
        while (true){
            float moved = MoveYStepWithCollide(distance);
            //无碰撞退出循环
            this.Position += Vector2.up * moved;
            if (moved == distance || correctTimes == 0) //无碰撞，且校正次数为0
            {
                collided = false;
                break;
            }

            float tempDist = distance - moved;
            correctTimes--;
            if (!CorrectY(tempDist)){
                this.Speed.y = 0; //未完成校正，则速度清零
                break;
            }

            distance = tempDist;
            Debug.Log($"每帧时间的当前位置{distY}");
        }

        //落地时候，进行缩放
        if (collided && distY < 0){
            if (this.stateMachine.State != (int)ActionState.Climb){
                this.PlayLandEffect(this.SpritePosition, speedY);
            }
        }
    }


    private bool CorrectX(float distX){
        Vector2 origin = this.Position + collider.position;
        Vector2 direct = Math.Sign(distX) > 0 ? Vector2.right : Vector2.left;

        if (stateMachine.State == (int)ActionState.Dash){
            if (onGround && DuckFreeAt(Position + Vector2.right * distX)){
                Ducking = true;
                return true;
            }
            else if (Speed.y == 0 && Speed.x != 0){
                for (int i = 1; i <= Constants.DashCornerCorrection; i++){
                    for (int j = 1; j >= -1; j -= 2){
                        if (!CollideCheck(this.Position + new Vector2(0, j * i * 0.1f), direct, Mathf.Abs(distX))){
                            //检测人物区域的四个边角位置是否发生碰撞
                            this.Position += new Vector2(distX, j * i * 0.1f);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool CorrectY(float distY){
        Vector2 origion = this.Position + collider.position;
        Vector2 direct = Math.Sign(distY) > 0 ? Vector2.up : Vector2.down;

        if (this.Speed.y < 0){
            if ((this.stateMachine.State == (int)ActionState.Dash) && !DashStartedOnGround){
                if (this.Speed.x <= 0){
                    for (int i = -1; i >= -Constants.DashCornerCorrection; i--){
                        float step = (Mathf.Abs(i * 0.1f) + DEVIATION);

                        if (!CheckGround(new Vector2(-step, 0))){
                            this.Position += new Vector2(-step, distY);
                            return true;
                        }
                    }
                }

                if (this.Speed.x >= 0){
                    for (int i = 1; i <= Constants.DashCornerCorrection; i++){
                        float step = (Mathf.Abs(i * 0.1f) + DEVIATION);
                        if (!CheckGround(new Vector2(step, 0))){
                            this.Position += new Vector2(step, distY);
                            return true;
                        }
                    }
                }
            }
        }
        //向上移动
        else if (this.Speed.y > 0){
            //Y轴向上方向的Corner Correction
            {
                if (this.Speed.x <= 0){
                    for (int i = 1; i <= Constants.UpwardCornerCorrection; i++){
                        //0.1f为修正位置，表示玩家可以叫Colider的边缘有0.1f的误差
                        RaycastHit2D hit = Physics2D.BoxCast(origion + new Vector2(-i * 0.1f, 0), collider.size, 0,
                            direct, Mathf.Abs(distY) + DEVIATION, GroundMask);
                        if (!hit){
                            this.Position += new Vector2(-i * 0.1f, 0);
                            return true;
                        }
                    }
                }

                if (this.Speed.x >= 0){
                    for (int i = 1; i <= Constants.UpwardCornerCorrection; i++){
                        RaycastHit2D hit = Physics2D.BoxCast(origion + new Vector2(i * 0.1f, 0), collider.size, 0,
                            direct, Mathf.Abs(distY) + DEVIATION, GroundMask);
                        if (!hit){
                            this.Position += new Vector2(i * 0.1f, 0);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }


    /// <summary>
    /// 每一个Tick运动的步长，以及地面移动的碰撞检测
    /// </summary>
    /// <param name="distX">帧与帧之间间隔的步长（time * speed.x）</param>
    /// <returns></returns>
    private float MoveXStepWithCollide(float distX){
        Vector2 moved = Vector2.zero;
        Vector2 direct = Math.Sign(distX) > 0 ? Vector2.right : Vector2.left;
        Vector2 origin = Position + collider.position;
        RaycastHit2D hit =
            Physics2D.BoxCast(origin, collider.size, 0, direct, Mathf.Abs(distX) + DEVIATION, GroundMask);
        if (hit && hit.normal == -direct){
            //如果发生碰撞,则需要将移动距离限制为射线检测到的距离减去一个偏移量 
            moved += direct * Mathf.Max(hit.distance - DEVIATION, 0);
        }
        else{
            moved += Vector2.right * distX;
        }

        return moved.x;
    }

    /// <summary>
    ///  每一个Tick运动的步长 Y轴
    /// </summary>
    /// <param name="distY">帧与帧之间间隔的步长（time * speed.y）</param>
    /// <returns></returns>
    private float MoveYStepWithCollide(float distY){
        Vector2 moved = Vector2.zero;
        Vector2 direct = Math.Sign(distY) > 0 ? Vector2.up : Vector2.down;
        Vector2 origin = this.Position + collider.position;
        RaycastHit2D hit = Physics2D.BoxCast(origin, collider.size, 0, direct, Mathf.Abs(distY) + DEVIATION,
            GroundMask);
        
        if (hit && hit.normal == -direct){
            //如果发生碰撞,则移动距离
            moved += direct * Mathf.Max((hit.distance - DEVIATION), 0);
        }
        else{
            moved += Vector2.up * distY;
        }

        return moved.y;
    }

    #region 碰撞检测

    public bool CollideCheck(Vector2 position, Vector2 dir, float dist = 0){
        Vector2 origion = position + collider.position;
        return Physics2D.OverlapBox(origion + dir * (dist + DEVIATION), collider.size, 0, GroundMask);
    }

    //攀爬检查
    public bool ClimbCheck(int dir, float yAdd = 0){
        //获取当前的碰撞体
        Vector2 origion = this.Position + collider.position;
        if (Physics2D.OverlapBox(
                origion + Vector2.up * (float)yAdd +
                Vector2.right * dir * (Constants.ClimbCheckDist * 0.1f + DEVIATION), collider.size, 0, GroundMask)){
            return true;
        }

        return false;
    }

    //根据整个关卡的边缘框进行检测,确保人物在关卡的框内.（用cinemachine代替了）
    public bool ClimbBoundsCheck(int dir){
        return true;
        //return base.Left + (float)(dir * 2) >= (float)this.level.Bounds.Left && base.Right + (float)(dir * 2) < (float)this.level.Bounds.Right;
    }

    //墙壁上跳检测
    public bool WallJumpCheck(int dir){
        return ClimbBoundsCheck(dir) &&
               this.CollideCheck(Position, Vector2.right * dir, Constants.WallJumpCheckDist);
    }

    /**----不知何用
    public RaycastHit2D ClimbHopSolid{ get; set; }

    public RaycastHit2D CollideClimbHop(int dir){
        Vector2 origion = this.Position + collider.position;
        RaycastHit2D hit =
            Physics2D.BoxCast(Position, collider.size, 0, Vector2.right * dir, DEVIATION, GroundMask);
        return hit;
        //if (hit && hit.normal.x == -dir)
        //{

        //}
    }
    **/
    /// <summary>
    /// 用于抓住墙面的滑动时有没有碰撞到地面
    /// </summary>
    /// <param name="addY"></param>
    /// <returns></returns>
    public bool SlipCheck(float addY = 0){
        int dir = Facing == Facings.Right ? 1 : -1;
        Vector2 origin = Position + collider.position + Vector2.up * collider.size.y / 2f +
                         Vector2.right * dir * (collider.size.x / 2f + STEP);
        Vector2 point1 = origin + Vector2.up * (-0.4f + addY);

        //有一个点接触到地面，则滑动结束
        if (Physics2D.OverlapPoint(point1, GroundMask)){
            return false;
        }

        Vector2 point2 = origin + Vector2.up * (0.4f + addY);
        if (Physics2D.OverlapPoint(point2, GroundMask)){
            return false;
        }

        return true;
    }

    public bool ClimbUpSnap(){
        for (int i = 0; i < Constants.ClimbCheckDist; i++){
            //检测上方是否存在可以攀爬的墙壁，如果存在则瞬移i个像素
            float yOffset = i * 0.1f;
            if (!CollideCheck(this.Position, Vector2.up, yOffset) && ClimbCheck((int)Facing, yOffset + DEVIATION)){
                this.Position += Vector2.up * yOffset;
                return true;
            }
        }

        return false;
    }

    //攀爬水平方向上的吸附
    public void ClimbSnap(){
        Vector2 origin = this.Position + collider.position;
        Vector2 dir = Vector2.right * (int)this.Facing;
        RaycastHit2D hit = Physics2D.BoxCast(origin, collider.size, 0, dir,
            Constants.ClimbCheckDist * 0.1f + DEVIATION, GroundMask);
        if (hit){
            //如果发生碰撞,则移动距离
            this.Position += dir * Mathf.Max((hit.distance - DEVIATION), 0);
        }
        //for (int i = 0; i < Constants.ClimbCheckDist; i++)
        //{
        //    Vector2 dir = Vector2.right * (int)ctx.Facing;
        //    if (!ctx.CollideCheck(ctx.Position, dir))
        //    {
        //        ctx.AdjustPosition(dir * 0.1f);
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}
    }

    public bool ClimbHopBlockedCheck(){
        return false;
    }

    /// <summary>
    /// 检测当前位置是否发生碰撞，没有碰撞则可以下蹲，
    /// </summary>
    /// <param name="at"></param>
    /// <returns></returns>
    public bool DuckFreeAt(Vector2 at){
        Vector2 oldP = Position;
        Rect oldC = this.collider;
        Position = at;
        //切换为蹲下的碰撞体
        this.collider = duckHitbox;

        bool ret = !CollideCheck(this.Position, Vector2.zero);

        this.Position = oldP;
        this.collider = oldC;

        return ret;
    }

    private bool CheckGround(){
        return CheckGround(Vector2.zero);
    }

    //针对横向,进行碰撞检测
    private bool CheckGround(Vector2 offset){
        Vector2 origion = this.Position + collider.position + offset;

        // RaycastHit2D hit = Physics2D.Raycast(origion, Vector2.down, 1f, GroundMask);
        // if (hit && hit.normal == Vector2.up){
        //     return true;
        // }
        //
        // return false;
        //修改了个BUG，如果在墙边向下冲会检测到墙面，而不是地面
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origion, collider.size, 0, Vector2.down, DEVIATION, GroundMask);
        foreach (var hit in hits){
            if (hit && hit.normal == Vector2.up){
                return true;
            }
        }
        
        return false;
    }

    #endregion
}