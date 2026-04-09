using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YassinTarek.SimonSays.Infrastructure
{
    public sealed class SceneLoaderService : ISceneLoaderService
    {
        private bool _isLoading;

        public void LoadGameplay() => LoadAdditiveThenUnload("Gameplay", "MainMenu");
        public void LoadMainMenu() => LoadAdditiveThenUnload("MainMenu", "Gameplay");

        private async void LoadAdditiveThenUnload(string load, string unload)
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                await SceneManager.LoadSceneAsync(load, LoadSceneMode.Additive);
                var sceneToUnload = SceneManager.GetSceneByName(unload);
                if (sceneToUnload.IsValid())
                    await SceneManager.UnloadSceneAsync(sceneToUnload);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneLoaderService] Failed to load {load}: {e}");
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
