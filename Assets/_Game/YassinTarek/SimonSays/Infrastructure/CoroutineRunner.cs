using System.Collections;
using UnityEngine;

namespace YassinTarek.SimonSays.Infrastructure
{
    public sealed class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public Coroutine StartRoutine(IEnumerator routine) => StartCoroutine(routine);

        public void StopRoutine(Coroutine c)
        {
            if (c != null)
                StopCoroutine(c);
        }
    }
}
