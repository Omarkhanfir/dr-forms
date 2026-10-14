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
        [SerializeField, Min(1.2f)] private float safePlayerDistance = 5;
        [SerializeField] private Vector2 arenaMinimum = new Vector2(-10, -10);
        [SerializeField] private Vector2 arenaMaximum = new Vector2(10, 10);
        [SerializeField, Min(0.6f)] private float boundaryMargin = 1;
        private readonly List<PrototypeTarget> living = new List<PrototypeTarget>();
        private PrototypePlayerHealth player;
        private PrototypeControls controls;
        private PrototypeQuestions questions;
        private bool awaitingQuestion;
        private static int currentRoom;
        private readonly Transform[] doors = new Transform[3];
        private readonly BoxCollider[] doorColliders = new BoxCollider[3];
        private readonly float[] doorOpen = new float[3];
        private GameObject route;
        private readonly Renderer[] exitLamps=new Renderer[3];
        public int RoomIndex => currentRoom;
        public bool ExitUnlocked => questions.AnswerCount >= currentRoom + 1;
        public float DoorOpening => doorOpen[currentRoom];
        public int EncounterStarts { get; private set; }
        public static float RoomCenter(int index) => index * 24;

        public int WaveNumber { get; private set; }
        public int WaveCount => enemyCounts.Length;
        public bool Victory { get; private set; }
        public bool OnBreak => false;
        public float BreakRemaining => 0;
        public Vector3[] LastSpawns { get; private set; } = new Vector3[0];
        public Vector3 SpawnPlayerPosition { get; private set; }
        public float SafePlayerDistance => safePlayerDistance;

        private void Awake()
        {
            player = GetComponent<PrototypePlayerHealth>();
            controls = GetComponent<PrototypeControls>();
            questions = GetComponent<PrototypeQuestions>();
            WaveNumber = currentRoom;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void NewRun() => currentRoom = 0;

        public void CompleteAnsweredWave()
        {
            if (!awaitingQuestion || questions.AnswerCount != WaveNumber || player.IsDefeated) return;
            awaitingQuestion = false;
            controls.ResumeAfterQuestion();
        }

        private void Start()
        {
            route = new GameObject("Three Room Dungeon Route");
            var floor = GameObject.Find("Arena Ground");
            floor.transform.position = new Vector3(0, -.25f, 24);
            floor.transform.localScale = new Vector3(22, .5f, 80);
            foreach (var name in new[] { "North Wall", "South Wall", "East Wall", "West Wall" })
                GameObject.Find(name).SetActive(false);
            // Remove the old practice obstacle from the forward route.
            var practice = GameObject.Find("Stationary Target");
            if (practice != null) practice.SetActive(false);
            for (int room = 0; room < 3; room++)
            {
                float z = RoomCenter(room);
                Barrier("Room left boundary", new Vector3(-10, .5f, z), new Vector3(1, 1, 20));
                Barrier("Room right boundary", new Vector3(10, .5f, z), new Vector3(1, 1, 20));
                foreach (float edge in new[] { z - 10, z + 10 })
                    foreach (float x in new[] { -6f, 6f })
                        Barrier("Doorway shoulder", new Vector3(x, .5f, edge), new Vector3(8, 1, 1));
                foreach (float x in new[] { -2.5f, 2.5f })
                {
                    Barrier("Passage boundary", new Vector3(x, .5f, z + 12), new Vector3(1, 1, 4));
                    Barrier("Door frame", new Vector3(x, 1.5f, z + 10), new Vector3(.6f, 3, .8f));
                }
                var door = Barrier("Locked room " + (room + 1) + " exit", new Vector3(0, 1.4f, z + 10), new Vector3(4, 2.8f, .6f), 3);
                exitLamps[room]=PrototypeCharacterVisual.Part(route.transform,"Laboratory exit status",PrimitiveType.Cube,new Vector3(1.65f,2.95f,z+9.6f),new Vector3(.3f,.18f,.12f),6).GetComponent<Renderer>();
                doors[room] = door.transform; doorColliders[room] = door.GetComponent<BoxCollider>();
                if (questions.AnswerCount > room) { doorOpen[room] = 1; door.transform.position += Vector3.up * 3.2f; doorColliders[room].enabled = false; }
                // Flat directional markers serve navigation rather than decoration.
                for (int n = 0; n < 3; n++)
                    PrototypeCharacterVisual.Part(route.transform, "Forward route marker", PrimitiveType.Cube,
                        new Vector3(0, .03f, z + 5 + n), new Vector3(1.2f - n * .3f, .04f, .3f), 8);
            }
            Barrier("Route start", new Vector3(0, .5f, -10), new Vector3(4, 1, 1));
            Barrier("Final passage left", new Vector3(-2.5f, .5f, 60), new Vector3(1, 1, 4));
            Barrier("Final passage right", new Vector3(2.5f, .5f, 60), new Vector3(1, 1, 4));
            Barrier("Route end", new Vector3(0, .5f, 62), new Vector3(6, 1, 1));
            var controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = new Vector3(0, 1, RoomCenter(currentRoom) - (currentRoom == 0 ? 0 : 7));
            controller.enabled = true;
            CourtyardArt.BuildRoom(route.transform, doors[0]);
            for(int room=1;room<3;room++)CourtyardArt.DressGate(doors[room]);
            Camera.main.GetComponent<PrototypeCameraFollow>().ResetComposition();
        }

        private GameObject Barrier(string name, Vector3 position, Vector3 scale, int color = 10)
        {
            // Keep the primitive's collider: visual Part intentionally destroys its collider.
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.transform.SetParent(route.transform, false);
            part.transform.position = position; part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = PrototypeCharacterVisual.Paint(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return part;
        }

        private void OnDestroy() { if (route != null) Destroy(route); }

        private void Update()
        {
            if (player.IsDefeated || Victory || !controls.CanPlay) return;
            for (int room = 0; room < 3; room++)
            {
                if(exitLamps[room]!=null)exitLamps[room].sharedMaterial=CourtyardArt.EnvironmentMaterial(questions.AnswerCount>room?"Green":"Violet");
                if (questions.AnswerCount <= room || doors[room] == null) continue;
                doorOpen[room] = Mathf.MoveTowards(doorOpen[room], 1, Time.deltaTime / .85f);
                doors[room].position = new Vector3(0, 1.4f + doorOpen[room] * 3.2f, RoomCenter(room) + 10);
                doorColliders[room].enabled = doorOpen[room] < 1;
            }
            if (transform.position.z < RoomCenter(currentRoom) - 10 || transform.position.z > RoomCenter(currentRoom) + 10) PrototypeEnemyBolt.Clear();
            if (awaitingQuestion) return;
            if (ExitUnlocked)
            {
                if (DoorOpening < 1 || Mathf.Abs(transform.position.x) > 2 || transform.position.z < RoomCenter(currentRoom) + (currentRoom == 2 ? 12 : 14)) return;
                if (currentRoom == 2)
                {
                    Victory = true; PrototypeAudio.Play(3); Time.timeScale = 0;
                    controls.SetGameplayEnabled(false); PrototypeEnemyBolt.Clear();
                    foreach (var projectile in FindObjectsByType<PrototypeProjectile>()) projectile.Cancel();
                    return;
                }
                currentRoom++;
                WaveNumber = currentRoom;
            }
            if (living.Count == 0 && WaveNumber == currentRoom) { SpawnWave(); return; }
            living.RemoveAll(target => target == null || target.Health <= 0);
            if (living.Count > 0) return;
            awaitingQuestion = true;
            questions.OpenForWave(WaveNumber);
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
                    candidates.Add(new Vector3(x, 1, z + RoomCenter(currentRoom)));
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
                enemy.ConfigureForWave(ranged, spawnIndex == positions.Count - 1);
                enemy.name = "Wave " + (WaveNumber + 1) + (ranged ? " Bloom specimen" : " Moss specimen");
                enemy.gameObject.SetActive(true);
                living.Add(enemy.GetComponent<PrototypeTarget>());
            }
            WaveNumber++;
            EncounterStarts++;
        }
    }
}
