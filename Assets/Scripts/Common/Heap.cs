using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Heap<T> where T : IHeapItem<T>{
    private T[] items;
    private int currentItemCount;

    public Heap(int maxHeapSize){
        items = new T[maxHeapSize];
    }

    public void Add(T item){
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    /// <summary>
    /// �ϸ�
    /// </summary>
    /// <param name="item"></param>
    void SortUp(T item){
        int parentIndex = (item.HeapIndex - 1) / 2;
        while (true){
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0){
                Swap(item, parentItem);
            }
            else{
                break;
            }

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T itemA, T itemB){
        int indexA = itemA.HeapIndex;
        int indexB = itemB.HeapIndex;
        items[indexA] = itemB;
        items[indexB] = itemA;

        // 更新 HeapIndex 属性
        itemA.HeapIndex = indexB;
        itemB.HeapIndex = indexA;
    }

    void SortDown(T item){
        while (true){
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;
            // 检查左子节点是否存在
            if (childIndexLeft < currentItemCount){
                swapIndex = childIndexLeft;
                if (childIndexRight < currentItemCount && items[childIndexRight].CompareTo(items[childIndexLeft]) > 0){
                    // 检查右子节点是否存在并且比左子节点的值更大（或更小，根据堆的类型而定）
                    swapIndex = childIndexRight;
                }

                if (item.CompareTo(items[swapIndex]) > 0){
                    Swap(item, items[swapIndex]);
                }
                else{
                    return; // 当前节点已经满足堆的性质，退出循环
                }
            }
            else{
                return; // 当前节点没有子节点，退出循环
            }
        }
    }
}

public interface IHeapItem<T> : IComparable<T>{
    int HeapIndex{ get; set; }
}