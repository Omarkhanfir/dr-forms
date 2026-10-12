using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(PrototypeControls))]
    public sealed class PrototypePlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        public int Health { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsDefeated => Health <= 0;
        public int DamageApplications { get; private set; }
        private bool restarting;
        private void Awake() => Health = maximumHealth;

        public void TakeDamage(int damage)
        {
            if (IsDefeated || damage <= 0) return;
            DamageApplications++;
            Health = Mathf.Max(0, Health - damage);
            if (!IsDefeated) return;
            GetComponent<PrototypeControls>().SetGameplayEnabled(false);
            // Discard shots at defeat rather than allowing damage behind the defeat screen.
            foreach (var projectile in FindObjectsByType<PrototypeProjectile>()) projectile.Cancel();
        }

        public void Restart()
        {
            if (!IsDefeated || restarting) return;
            restarting = true;
            PrototypeControls.PrepareRestart();
            // Reload restores destroyed enemies/targets as well as every transient effect and input owner.
#if UNITY_EDITOR
            // The prototype intentionally is not part of the project's default build scene list.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
#endif
        }
    }
}
