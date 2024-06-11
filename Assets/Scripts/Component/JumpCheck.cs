using System;
using System.Collections;
using UnityEngine;

public class JumpCheck{
    private float timer;
    private PlayerController controller;

    public float Timer => timer;

    //下落重力
    private bool jumpGrace;

    public JumpCheck(PlayerController playerController, bool jumpGrace){
        this.controller = playerController;
        this.ResetTime();
        this.jumpGrace = jumpGrace;
    }

    public void ResetTime(){
        this.timer = 0;
    }

    public void Update(float deltaTime){
        //Jump Grace
        if (controller.OnGround){
            //dreamJump = false;
            timer = Constants.JumpGraceTime;
        }
        else{
            if (timer > 0){
                timer -= deltaTime;
            }
        }
    }

    public bool AllowJump(){
        return jumpGrace ? timer > 0 : controller.OnGround;
    }
}