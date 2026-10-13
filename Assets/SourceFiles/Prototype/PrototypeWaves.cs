using System.Collections.Generic;
using UnityEngine;

namespace DrForms.Prototype
{
    // Resolve clear/death after all combat updates; death wins a same-frame tie.
    [DefaultExecutionOrder(100)]
    public sealed class PrototypeWaves : MonoBehaviour
    {
        [SerializeField] private PrototypeEnemy enemyTemplate;
        [SerializeField] private int[] enemyCounts = { 2, 3, 4 };
        [SerializeField, Min(0)] private float breakSeconds = 3;
        [SerializeField, Min(1.2f)] private float safePlayerDistance = 5;
        [SerializeField] private Vector2 arenaMinimum = new Vector2(-10, -10);
        [SerializeField] private Vector2 arenaMaximum = new Vector2(10, 10);
        [SerializeField, Min(0.6f)] private float boundaryMargin = 1;
        private readonly List<PrototypeTarget> living = new List<PrototypeTarget>();
        private PrototypePlayerHealth player;
        private PrototypeControls controls;
        private float nextWaveTime;
        private PrototypeQuestions questions;
        private bool awaitingQuestion;
        private static int completedWaves;
        public int WaveNumber { get; private set; }
        public int WaveCount => enemyCounts.Length;
        public bool Victory { get; private set; }
        public bool OnBreak { get; private set; }
        public float BreakRemaining => OnBreak ? Mathf.Max(0, nextWaveTime - Time.time) : 0;
        public Vector3[] LastSpawns { get; private set; } = new Vector3[0];
        public Vector3 SpawnPlayerPosition { get; private set; }
        public float SafePlayerDistance => safePlayerDistance;

        private void Awake()
        {
            player = GetComponent<PrototypePlayerHealth>();
            controls = GetComponent<PrototypeControls>();
            questions = GetComponent<PrototypeQuestions>();
            WaveNumber = completedWaves;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void NewRun() => completedWaves = 0;

        public void CompleteAnsweredWave()
        {
            if (!awaitingQuestion || questions.AnswerCount != WaveNumber || player.IsDefeated) return;
            awaitingQuestion = false;
            completedWaves = WaveNumber;
            if (WaveNumber == WaveCount)
            {
                Victory = true;
                PrototypeAudio.Play(3);
                Time.timeScale = 0;
                controls.SetGameplayEnabled(false);
                foreach (var projectile in FindObjectsByType<PrototypeProjectile>()) projectile.Cancel();
                PrototypeEnemyBolt.Clear();
                return;
            }
            OnBreak = true;
            nextWaveTime = Time.time + breakSeconds;
            controls.ResumeAfterQuestion();
        }

        private void Update()
        {
            if (player.IsDefeated || Victory || !controls.CanPlay) return;
            if (awaitingQuestion) return;
            if (living.Count == 0 && WaveNumber == completedWaves && !OnBreak) { SpawnWave(); return; }
            living.RemoveAll(target => target == null || target.Health <= 0);
            if (living.Count > 0) return;
            if (!OnBreak)
            {
                awaitingQuestion = true;
                questions.OpenForWave(WaveNumber);
                return;
            }
            if (Time.time >= nextWaveTime) SpawnWave();
        }

        private void SpawnWave()
        {
            if (enemyTemplate == null || WaveNumber >= WaveCount) return;
            var positions = new List<Vector3>();
            // Test a shuffled grid so even a player near a wall has safe, non-overlapping spawns.
            var candidates = new List<Vector3>();
            var radius = enemyTemplate.GetComponent<CharacterController>().radius + 0.08f;
            var margin = Mathf.Max(boundaryMargin, radius);
            for (var x = arenaMinimum.x + margin; x <= arenaMaximum.x - margin; x += radius * 3)
                for (var z = arenaMinimum.y + margin; z <= arenaMaximum.y - margin; z += radius * 3)
                    candidates.Add(new Vector3(x, 1, z));
            var count = Mathf.Max(1, enemyCounts[WaveNumber]);
            while (candidates.Count > 0 && positions.Count < count)
            {
                var index = Random.Range(0, candidates.Count);
                var position = candidates[index];
                candidates.RemoveAt(index);
                var delta = position - player.transform.position;
                delta.y = 0;
                if (delta.magnitude < safePlayerDistance) continue;
                if (positions.Exists(other => Vector3.Distance(other, position) < radius * 2)) continue;
                // Lift the query's lower cap off the floor; touching the floor is a valid spawn.
                if (Physics.CheckCapsule(position + Vector3.down * (0.9f - radius),
                    position + Vector3.up * (1 - radius), radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                positions.Add(position);
            }
            // Never relax safety or create a partial wave if Inspector settings cannot fit it.
            if (positions.Count != count) return;
            LastSpawns = positions.ToArray();
            SpawnPlayerPosition = player.transform.position;
            for (var spawnIndex = 0; spawnIndex < positions.Count; spawnIndex++)
            {
                var enemy = Instantiate(enemyTemplate, positions[spawnIndex], Quaternion.identity);
                var ranged = WaveNumber >= 1 && spawnIndex == positions.Count - 1;
                enemy.ConfigureForWave(ranged);
                enemy.name = "Wave " + (WaveNumber + 1) + (ranged ? " Flower Mage" : " Mossling");
                enemy.gameObject.SetActive(true);
                living.Add(enemy.GetComponent<PrototypeTarget>());
            }
            WaveNumber++;
            OnBreak = false;
        }
    }
}
