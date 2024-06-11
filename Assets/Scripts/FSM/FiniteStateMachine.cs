using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState{
    Normal, //通常情况
    Dash, //冲刺
    Climb, //攀爬
    
    
    Size //用于计算所有状态机的个数
}

public abstract class BaseActionState{
    protected ActionState state;
    protected PlayerController player;
    public ActionState State{
        get => state;
    }

    protected BaseActionState(ActionState state, PlayerController context){
        this.state = state;
        this.player = context;
    }

    /// <summary>
    /// 每一帧调用
    /// </summary>
    /// <param name="deltaTime">上一帧到当前帧的时间间隔</param>
    /// <returns></returns>
    public abstract ActionState Update(float deltaTime);
    public abstract IEnumerator Coroutine();
    public abstract void OnBegin();
    public abstract void OnEnd();
    public abstract bool IsCoroutine();
}

/// <summary>
/// 有限状态机
/// </summary>
public class FiniteStateMachine<T> where T : BaseActionState{
    private T[] states;

    private int currentState = -1; //当前状态
    private int prevSatate = -1; //上一帧状态
    private Coroutine currentCoroutine;

    public int State{
        get => currentState;
        set{
            if (currentState == value){
                return;
            }
            this.prevSatate = currentState;
            this.currentState = value;
            if (this.prevSatate != -1){
                //执行上一个状态的结束函数
                this.states[this.prevSatate].OnEnd();
            }
            this.states[currentState].OnBegin();
            if (this.states[this.currentState].IsCoroutine()){
                this.currentCoroutine.Replace(this.states[this.currentState].Coroutine());
                return;
            }

            this.currentCoroutine.Cancel();
        }
    }

    public FiniteStateMachine(int size){
        this.states = new T[size];
        this.currentCoroutine = new Coroutine();
    }

    public void AddState(T state){
        this.states[(int)state.State] = state;
    }

    public void Update(float deltaTime){
        State = (int)this.states[currentState].Update(deltaTime);
        if (this.currentCoroutine.Active){
            this.currentCoroutine.Update(deltaTime);
        }
    }
}