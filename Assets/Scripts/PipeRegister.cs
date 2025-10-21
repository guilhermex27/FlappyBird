using UnityEngine;

public class PipeRegister : MonoBehaviour
{
    void OnEnable() => PipeManager.RegisterPipe(transform);
    void OnDisable() => PipeManager.UnregisterPipe(transform);
}
