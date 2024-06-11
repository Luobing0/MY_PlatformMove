using UnityEngine;
using System;
using UnityEngine.Animations;

public partial class PlayerController{
    public readonly int DashID = Animator.StringToHash("dash");
    public readonly int DuckID = Animator.StringToHash("duck");
    public readonly int IdleID = Animator.StringToHash("idle");
    public readonly int RunID = Animator.StringToHash("run");
    public readonly int ClimbID = Animator.StringToHash("wallslide");
    public readonly int JumpID = Animator.StringToHash("jump");
}