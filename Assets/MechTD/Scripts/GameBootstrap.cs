using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechTD
{
    /// <summary>
    /// Starts the game automatically when Play is pressed and whenever the scene is
    /// reloaded (restart), so nothing has to be wired up by hand in the editor.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Runs once after the first scene is loaded when Play is pressed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFirstScene() => EnsureGame();

        // Runs on every later scene load (e.g. pressing R to restart).
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) EnsureGame();
        }

        private static void EnsureGame()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            Debug.Log("[Steel Vanguard] Starting game");
            new GameObject("Steel Vanguard Game").AddComponent<GameManager>();
        }
    }
}
