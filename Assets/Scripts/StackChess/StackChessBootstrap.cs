using UnityEngine;

namespace StackChess
{
    public static class StackChessBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (Object.FindObjectOfType<StackChessPrototype>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Stack Chess Prototype");
            root.AddComponent<StackChessPrototype>();
        }
    }
}

