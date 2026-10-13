using UnityEngine;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(CharacterController), typeof(PrototypeControls))]
    public sealed class PrototypeMovement : MonoBehaviour
    {
        [SerializeField] private Transform movementCamera;
        [SerializeField] private Collider arenaFloor;
        [SerializeField, Min(0)] private float speed = 5;
        [SerializeField, Min(0.01f)] private float stopDistance = 0.12f;
        private CharacterController controller;
        private PrototypeControls controls;
        private Camera view;
        private float verticalSpeed;
        private Vector3 destination;
        private bool hasDestination, arrived;
        private Renderer body;
        private MaterialPropertyBlock visual;
        private Color normalColor;
        private bool showingAttack;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controls = GetComponent<PrototypeControls>();
            if (movementCamera == null && Camera.main != null) movementCamera = Camera.main.transform;
            view = movementCamera != null ? movementCamera.GetComponent<Camera>() : null;
            body = GetComponent<Renderer>();
            visual = new MaterialPropertyBlock();
            if (body != null) normalColor = body.sharedMaterial.GetColor("_BaseColor");
        }

        private void OnDisable()
        {
            hasDestination = false;
            controls?.Suspend();
            SetAttackVisual(false);
        }

        private void Update()
        {
            controls.PrepareInput();
            if (!controls.GameplayEnabled)
            {
                hasDestination = false;
                SetAttackVisual(false);
                return;
            }
            var horizontal = controls.TouchMode ? TouchVelocity() : PointerVelocity();
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var previous = transform.position;
            var collisions = controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            SetAttackVisual(controls.AttackActive);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (horizontal.sqrMagnitude > 0)
            {
                movingSeconds += Time.deltaTime;
                requestedDistance += horizontal.magnitude * Time.deltaTime;
                var displacement = transform.position - previous;
                displacement.y = 0;
                traveledDistance += displacement.magnitude;
            }
            if ((collisions & CollisionFlags.Sides) != 0) sideCollisionFrames++;
#endif
        }

        private Vector3 TouchVelocity()
        {
            hasDestination = false;
            var forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward.y = 0;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var input = Vector2.ClampMagnitude(controls.JoystickValue, 1);
            return (right * input.x + forward * input.y) * speed;
        }

        private Vector3 PointerVelocity()
        {
            if (!controls.ReadDesktopPointer(out var pointer, out var changed) || view == null || arenaFloor == null)
            {
                hasDestination = false;
                return Vector3.zero;
            }
            // A stationary cursor retains a fixed world target, independent of camera following.
            if (changed)
            {
                hasDestination = arenaFloor.Raycast(view.ScreenPointToRay(pointer), out var hit, 1000);
                if (hasDestination) destination = hit.point;
                var delta = destination - transform.position;
                delta.y = 0;
                if (delta.magnitude > stopDistance * 2) arrived = false;
            }
            if (!hasDestination) return Vector3.zero;
            var remaining = destination - transform.position;
            remaining.y = 0;
            var distance = remaining.magnitude;
            if (distance <= stopDistance) arrived = true;
            if (arrived || Time.deltaTime <= 0) return Vector3.zero;
            // Slow near the target and cap each step to prevent overshoot on long frames.
            var velocity = Mathf.Min(speed, distance * 8, (distance - stopDistance) / Time.deltaTime);
            return remaining.normalized * Mathf.Max(0, velocity);
        }

        private void SetAttackVisual(bool active)
        {
            if (body == null || visual == null || active == showingAttack) return;
            showingAttack = active;
            body.GetPropertyBlock(visual);
            visual.SetColor("_BaseColor", active ? new Color(1, 0.95f, 0.25f) : normalColor);
            body.SetPropertyBlock(visual);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private float movingSeconds;
        private float requestedDistance;
        private float traveledDistance;
        private int sideCollisionFrames;

        // Read-only browser verification; omitted from release builds.
        public void LogVerificationState()
        {
            var combat = GetComponent<PrototypeCombat>();
            var target = FindAnyObjectByType<PrototypeTarget>();
            var enemy = FindAnyObjectByType<PrototypeEnemy>();
            var playerHealth = GetComponent<PrototypePlayerHealth>();
            var waves = GetComponent<PrototypeWaves>();
            var questions = GetComponent<PrototypeQuestions>();
            Debug.Log("PROTOTYPE_STATE:" + JsonUtility.ToJson(new VerificationState
            {
                frame = Time.frameCount, realtime = Time.realtimeSinceStartup,
                artShadowResolution = CourtyardArt.ShadowResolution,
                artAnimation = GetComponent<CourtyardActor>() != null ? GetComponent<CourtyardActor>().CurrentAnimation : "",
                artClipTime = GetComponent<CourtyardActor>() != null ? GetComponent<CourtyardActor>().ClipTime : 0,
                artSkinnedParts = GetComponent<CourtyardActor>() != null ? GetComponent<CourtyardActor>().SkinnedParts : 0,
                menuSoundCenter = controls.MenuSoundCenter, menuRestartCenter = controls.MenuRestartCenter,
                rangedCount = System.Array.FindAll(FindObjectsByType<PrototypeEnemy>(), e => e.IsRanged && e.GetComponent<PrototypeTarget>().Health > 0).Length,
                telegraphCount = System.Array.FindAll(FindObjectsByType<PrototypeEnemy>(), e => e.Telegraphing).Length,
                rangedShots = System.Array.ConvertAll(FindObjectsByType<PrototypeEnemy>(), e => e.RangedShots),
                enemyBolts = PrototypeEnemyBolt.ActiveCount, projectilePool = combat != null ? combat.PoolSize : 0,
                started = controls.Started, manualPaused = controls.ManualPaused, muted = PrototypeAudio.Muted,
                playCenter = controls.PlayCenter, pauseCenter = controls.PauseCenter, muteCenter = controls.MuteCenter, resumeCenter = controls.ResumeCenter,
                grace = playerHealth != null && playerHealth.GraceActive,
                applicationFocus = Application.isFocused, bridgeFocus = PrototypeBrowserInput.Focused,
                timeScale = Time.timeScale, browserEpoch = PrototypeBrowserInput.Epoch,
                gameplayEnabled = controls.GameplayEnabled, canPlay = controls.CanPlay,
                inputReady = controls.InputReady, pointerPressed = PrototypeControls.AnyPointerPressed(),
                selectedAnswer = questions != null ? questions.Selected : -1,
                questionInputReady = questions != null && questions.InputReady,
                submitCenter = questions != null ? questions.SubmitCenter : Vector2.zero,
                questionPaused = questions != null && questions.Paused,
                answers = questions != null ? questions.Answers : new int[0],
                questionActiveSeconds = questions != null ? questions.ActiveSeconds : 0,
                answerCenters = questions != null ? questions.OptionCenters : new Vector2[0],
                projectilePositions = System.Array.ConvertAll(FindObjectsByType<PrototypeProjectile>(), shot => shot.transform.position),
                playerHealth = playerHealth != null ? playerHealth.Health : 0,
                wave = waves != null ? waves.WaveNumber : 0,
                room = waves != null ? waves.RoomIndex : 0,
                exitUnlocked = waves != null && waves.ExitUnlocked,
                doorOpening = waves != null ? waves.DoorOpening : 0,
                encounterStarts = waves != null ? waves.EncounterStarts : 0,
                victory = waves != null && waves.Victory,
                victoryVisible = controls.VictoryVisible,
                onBreak = waves != null && waves.OnBreak,
                breakRemaining = waves != null ? waves.BreakRemaining : 0,
                spawns = waves != null ? waves.LastSpawns : new Vector3[0],
                spawnPlayerPosition = waves != null ? waves.SpawnPlayerPosition : Vector3.zero,
                safePlayerDistance = waves != null ? waves.SafePlayerDistance : 0,
                playerDamageApplications = playerHealth != null ? playerHealth.DamageApplications : 0,
                defeated = playerHealth != null && playerHealth.IsDefeated,
                defeatVisible = controls.DefeatVisible,
                waitingForRelease = controls.WaitingForRelease,
                restartCenter = controls.RestartCenter,
                enemyAlive = enemy != null,
                enemyCount = FindObjectsByType<PrototypeEnemy>().Length,
                targetCount = FindObjectsByType<PrototypeTarget>().Length,
                enemyPosition = enemy != null ? enemy.transform.position : Vector3.zero,
                enemyHealth = enemy != null ? enemy.GetComponent<PrototypeTarget>().Health : 0,
                enemyHits = enemy != null ? enemy.ContactHits : 0,
                contactDamage = enemy != null ? enemy.ContactDamage : 0,
                contactCooldown = enemy != null ? enemy.ContactCooldown : 0,
                contactDistance = enemy != null ? enemy.ContactDistance : 0,
                lastContactTime = enemy != null ? enemy.LastContactTime : 0,
                previousContactTime = enemy != null ? enemy.PreviousContactTime : 0,
                shotsFired = combat != null ? combat.ShotsFired : 0,
                projectiles = combat != null ? combat.ActiveProjectiles : 0,
                damage = combat != null ? combat.Damage : 0,
                projectileSpeed = combat != null ? combat.ProjectileSpeed : 0,
                hitsApplied = combat != null ? combat.HitsApplied : 0,
                lastShotTime = combat != null ? combat.LastShotTime : 0,
                previousShotTime = combat != null ? combat.PreviousShotTime : 0,
                muzzleActive = combat != null && combat.MuzzleActive,
                hitFlash = target != null && target.HitFlashActive,
                healthFill = target != null ? target.DisplayedHealthFraction : 0,
                damageApplications = target != null ? target.DamageApplications : 0,
                effects = FindObjectsByType<PrototypeCombatEffect>().Length,
                trails = FindObjectsByType<TrailRenderer>().Length,
                range = combat != null ? combat.Range : 0,
                cooldownRemaining = combat != null ? combat.CooldownRemaining : 0,
                cooldown = combat != null ? combat.Cooldown : 0,
                targetHealth = target != null ? target.Health : 0,
                targetAlive = target != null,
                targetScreen = target != null ? Project(target.transform.position) : Vector3.zero,
                position = transform.position,
                destination = destination, hasDestination = hasDestination, arrived = arrived,
                fieldOfView = view != null ? view.fieldOfView : 50,
                camera = movementCamera != null ? movementCamera.position : Vector3.zero,
                forward = movementCamera != null ? movementCamera.forward : Vector3.forward,
                input = controls.JoystickValue, touchMode = controls.TouchMode, focused = controls.HasFocus,
                pointerInside = PrototypeBrowserInput.Inside, attackCount = controls.AttackCount, attackActive = controls.AttackActive,
                time = Time.time,
                movingSeconds = movingSeconds,
                requestedDistance = requestedDistance,
                traveledDistance = traveledDistance,
                sideCollisionFrames = sideCollisionFrames,
                screenWidth = Screen.width, screenHeight = Screen.height,
                playerFloorScreen = Project(new Vector3(transform.position.x, 0, transform.position.z)),
                eastFloorScreen = Project(new Vector3(10.7f, 0, transform.position.z)),
                joystickCenter = controls.JoystickCenter, joystickRadius = controls.JoystickScreenRadius,
                attackCenter = controls.AttackCenter, hudCenter = controls.HudCenter
            }));
        }

        private Vector3 Project(Vector3 world) => view != null ? view.WorldToScreenPoint(world) : Vector3.zero;

        [System.Serializable]
        private struct VerificationState
        {
            public string artAnimation;
            public float artClipTime;
            public int artSkinnedParts, artShadowResolution;
            public int frame, rangedCount, telegraphCount, enemyBolts, projectilePool;
            public int[] rangedShots;
            public float realtime;
            public Vector2 menuSoundCenter, menuRestartCenter;
            public bool started, manualPaused, muted, grace;
            public Vector2 playCenter, pauseCenter, muteCenter, resumeCenter;
            public bool applicationFocus, bridgeFocus, gameplayEnabled, canPlay, inputReady, pointerPressed;
            public float timeScale;
            public int browserEpoch;
            public bool questionInputReady;
            public int selectedAnswer;
            public Vector2 submitCenter;
            public bool questionPaused;
            public int[] answers;
            public float questionActiveSeconds;
            public Vector2[] answerCenters;
            public Vector3[] projectilePositions;
            public int wave, room, encounterStarts;
            public bool exitUnlocked;
            public float doorOpening;
            public bool victory, victoryVisible, onBreak;
            public float breakRemaining, safePlayerDistance;
            public Vector3[] spawns;
            public Vector3 spawnPlayerPosition;
            public int playerHealth, playerDamageApplications, enemyHealth, enemyHits, contactDamage, enemyCount, targetCount;
            public bool defeated, defeatVisible, waitingForRelease, enemyAlive;
            public Vector2 restartCenter;
            public Vector3 enemyPosition;
            public float contactCooldown, contactDistance, lastContactTime, previousContactTime;
            public int shotsFired, projectiles, damage, targetHealth;
            public float cooldownRemaining;
            public float range, cooldown, projectileSpeed, lastShotTime, previousShotTime, healthFill;
            public int hitsApplied, damageApplications, effects, trails;
            public bool muzzleActive, hitFlash;
            public bool targetAlive;
            public Vector3 targetScreen;
            public Vector3 position, destination, camera, forward, playerFloorScreen, eastFloorScreen;
            public Vector2 input, joystickCenter, attackCenter, hudCenter;
            public bool touchMode, focused, pointerInside, hasDestination, arrived, attackActive;
            public float fieldOfView;
            public float time, movingSeconds, requestedDistance, traveledDistance;
            public float joystickRadius;
            public int sideCollisionFrames, attackCount, screenWidth, screenHeight;
        }
#endif
    }
}
