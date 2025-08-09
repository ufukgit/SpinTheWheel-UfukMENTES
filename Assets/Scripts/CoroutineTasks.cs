using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class CoroutineTasks
{
    public static IEnumerator Wait(Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) Debug.LogError(task.Exception);
    }

    public static IEnumerator Wait<T>(Task<T> task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) Debug.LogError(task.Exception);
    }
}