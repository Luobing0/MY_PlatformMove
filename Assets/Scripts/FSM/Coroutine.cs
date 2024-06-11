using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;


/// <summary>
/// 自写协程
/// </summary>
public class Coroutine{
    //使用栈管理枚举器，被迭代过的直接出栈
    private Stack<IEnumerator> enumerators;

    //用于控制协程中的等待时间
    private float waitTimer;
    private bool ended;

    public bool Finished{ get; private set; }
    public bool Active{ get; set; }

    public Coroutine(){
        this.enumerators = new Stack<IEnumerator>();
        this.Active = false;
    }

    public void Update(float deltaTime){
        this.ended = false;
        if (waitTimer > 0){
            //正在等待一个时间间隔
            waitTimer -= deltaTime;
            return;
        }

        //枚举器中还有对象，继续迭代
        if (this.enumerators.Count > 0){
            //出栈，取出当前对象
            IEnumerator enumerator = enumerators.Peek();
            if (enumerator.MoveNext() && !ended){
                //重新赋值时间间隔
                if (enumerator.Current is int){
                    waitTimer = (float)((int)enumerator.Current);
                }

                if (enumerator.Current is float){
                    this.waitTimer = (float)enumerator.Current;
                    return;
                }

                if (enumerator.Current is IEnumerator){
                    this.enumerators.Push(enumerator.Current as IEnumerator);
                    return;
                }
            }
        }
    }

    public void Cancel(){
        this.Active = false;
        this.Finished = true;
        this.waitTimer = 0f;
        this.enumerators.Clear();
        this.ended = true;
    }

    public void Replace(IEnumerator functionCall){
        this.Active = true;
        this.Finished = false;
        this.waitTimer = 0f;
        this.enumerators.Clear();
        this.enumerators.Push(functionCall);
        this.ended = true;
    }
}