using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState{
    Normal, //ͨ�����
    Dash, //���
    Climb, //����
    
    
    Size //���ڼ�������״̬���ĸ���
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
    /// ÿһ֡����
    /// </summary>
    /// <param name="deltaTime">��һ֡����ǰ֡��ʱ����</param>
    /// <returns></returns>
    public abstract ActionState Update(float deltaTime);
    public abstract IEnumerator Coroutine();
    public abstract void OnBegin();
    public abstract void OnEnd();
    public abstract bool IsCoroutine();
}

/// <summary>
/// ����״̬��
/// </summary>
public class FiniteStateMachine<T> where T : BaseActionState{
    private T[] states;

    private int currentState = -1; //��ǰ״̬
    private int prevSatate = -1; //��һ֡״̬
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
                //ִ����һ��״̬�Ľ�������
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