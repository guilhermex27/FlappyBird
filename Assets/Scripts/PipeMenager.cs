using UnityEngine;
using System.Collections.Generic;

public class PipeManager : MonoBehaviour
{
    public static List<Transform> pipes = new List<Transform>();

    public static void RegisterPipe(Transform pipe)
    {
        pipes.Add(pipe);
    }

    public static void UnregisterPipe(Transform pipe)
    {
        pipes.Remove(pipe);
    }

    public static Transform GetClosestPipe(Vector3 birdPos)
    {
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var pipe in pipes)
        {
            if (pipe == null) continue;

            float dist = pipe.position.x - birdPos.x;
            if (dist > 0 && dist < minDist)
            {
                minDist = dist;
                closest = pipe;
            }
        }

        return closest;
    }
}
