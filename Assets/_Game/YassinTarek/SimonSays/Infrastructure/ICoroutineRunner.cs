using System.Collections;
using UnityEngine;

namespace YassinTarek.SimonSays.Infrastructure
{
    public interface ICoroutineRunner
    {
        Coroutine StartRoutine(IEnumerator routine);
        void StopRoutine(Coroutine coroutine);
    }
}
