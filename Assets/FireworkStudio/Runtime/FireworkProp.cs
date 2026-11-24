using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireworkStudio
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class FireworkProp : MonoBehaviour
    {
        public FireworkPropKind kind = FireworkPropKind.Rocket;
        public FireworkPalette palette = FireworkPalette.Mixed;
        [Header("Firework show (old props default to Combined)")]
        public FireworkShowMode showMode = FireworkShowMode.Combined;
        [Tooltip("Used ONLY by Crackle Only. Does not change the combined show's terminal crackle.")]
        public CrackleOnlyOptions crackleOnly = new CrackleOnlyOptions();
        public Transform ignitionPoint;
        public Transform tailPoint;
        public Transform tipPoint;
        public float hoverAmplitude = 0.05f;
        public float hoverSpeed = 1.3f;
        public float rotateSpeed = 28f;
        [Header("Explosion Style")]
        public bool randomPattern = true;
        public FireworkPattern fixedPattern = FireworkPattern.Flower;
        public bool randomPalette = true;
        public FireworkPalette fixedPalette = FireworkPalette.Pink;
        public bool useCustomColor = false;
        [ColorUsage(true, true)] public Color customColor = new Color(1f, 0.4f, 0.85f);
        [Header("Sky Launch")]
        [Min(12f)] public float minimumBurstHeightAboveLaunch = 35f;
        public float rocketSpeed = 26f;
        public float rocketFlightSeconds = 1.55f;
        public float sparklerBurnSeconds = 24f;
        [Header("Sparkler only")]
        public SparklerOptions sparklerOptions = new SparklerOptions();
        bool _sparklerReturnPending;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
        Rigidbody _rb;
        Vector3 _displayPos;
        Quaternion _displayRot;
        bool _grabbed;
        bool _activated;
        float _nextRocketShot;
        public float rocketShotCooldown = 0.32f;
        public float minimumLaunchUp = 0.64f;
        public float rocketMinimumTravel = 20.0f;
        SparklerVisual _sparkler;
        RocketTrailVisual _rocketTrail;

        void Start()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            _rb = GetComponent<Rigidbody>();

            _displayPos = transform.position;
            _displayRot = transform.rotation;

            _rb.useGravity = false;
            _rb.isKinematic = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
            _grab.activated.AddListener(OnActivated);

            if (kind == FireworkPropKind.Sparkler)
            {
                var target = tipPoint != null ? tipPoint : transform;
                _sparkler = GetComponent<SparklerVisual>();
                if (_sparkler == null) _sparkler = gameObject.AddComponent<SparklerVisual>();
                _sparkler.autoPlay = false;
                _sparkler.Configure(target, sparklerOptions);
            }
            else
            {
                var target = tailPoint != null ? tailPoint : transform;
                _rocketTrail = gameObject.AddComponent<RocketTrailVisual>();
                _rocketTrail.tail = target;
                _rocketTrail.palette = palette;
                _rocketTrail.autoPlay = false;
            }
        }

        void OnEnable()
        {
            // After toggling this component in Play Mode, do not fight an existing XRI selection.
            if (kind == FireworkPropKind.Sparkler && _grab != null)
                _grabbed = _grab.isSelected;
        }

        void OnDisable()
        {
            if (kind != FireworkPropKind.Sparkler) return;
            StopAllCoroutines();
            if (_sparkler != null) _sparkler.Extinguish(true);
            _activated = false;
            _grabbed = false;
            _sparklerReturnPending = false;
        }

        void OnDestroy()
        {
            if (_grab != null)
            {
                _grab.selectEntered.RemoveListener(OnSelectEntered);
                _grab.selectExited.RemoveListener(OnSelectExited);
                _grab.activated.RemoveListener(OnActivated);
            }
        }

        void Update()
        {
            if (!_grabbed && !_activated && !(kind == FireworkPropKind.Sparkler && _sparklerReturnPending))
            {
                var pos = _displayPos;
                pos.y += Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
                transform.position = pos;
                transform.rotation = _displayRot * Quaternion.Euler(0f, Time.time * rotateSpeed, 0f);
            }
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            _grabbed = true;
            if (kind == FireworkPropKind.Sparkler)
            {
                _sparklerReturnPending = false;
                if (_sparkler != null) _sparkler.ResetMotionHistory();
                return; // XRI owns Rigidbody movement while the sparkler is selected.
            }
            _rb.isKinematic = false;
            _rb.useGravity = false;
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            _grabbed = false;
            if (kind == FireworkPropKind.Sparkler && !isActiveAndEnabled) return;
            if (kind == FireworkPropKind.Sparkler)
            {
                // Hold the release pose while burning. Do not fling or teleport an active ribbon.
                if (!_rb.isKinematic) { _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
                _rb.isKinematic = true;
                _sparklerReturnPending = true;
                if (_sparkler != null) _sparkler.ResetMotionHistory();
            }
            if (_activated && kind == FireworkPropKind.Rocket) return;
            StartCoroutine(ReturnToDisplayIfIdle());
        }

        IEnumerator ReturnToDisplayIfIdle()
        {
            yield return new WaitForSeconds(0.2f);
            if (_grabbed || _activated) yield break;
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
            if (kind == FireworkPropKind.Sparkler)
            {
                if (_sparkler != null) _sparkler.ResetMotionHistory();
                _sparklerReturnPending = false;
            }
            transform.position = _displayPos;
            transform.rotation = _displayRot;
        }

        void OnActivated(ActivateEventArgs args)
        {
            if (kind == FireworkPropKind.Sparkler && !isActiveAndEnabled) return;
            if (_grab == null || !_grab.isSelected) return;
            if (kind == FireworkPropKind.Sparkler)
            {
                if (_activated) return;
                _activated = true;
                StartCoroutine(BurnSparkler());
            }
            else if (Time.time >= _nextRocketShot)
            {
                _nextRocketShot = Time.time + Mathf.Max(0.15f, rocketShotCooldown);
                LaunchRocket();
            }
        }

        IEnumerator BurnSparkler()
        {
            float duration = SparklerOptions.Safe(sparklerBurnSeconds, 24f, 1f, 180f);
            if (_sparkler == null || !_sparkler.Ignite(duration))
            { _activated = false; yield break; }
            while (_sparkler != null && _sparkler.IsBurning) yield return null;
            if (_sparkler != null) _sparkler.Extinguish(false);
            // Let airborne embers and the hand trail fade before returning to the display.
            yield return new WaitForSeconds(0.65f);
            _activated = false;
            if (!_grabbed) StartCoroutine(ReturnToDisplayIfIdle());
        }

        void LaunchRocket()
        {
            // Spawn a separate projectile. Never hand the held XRI prop to the flight controller.
            Vector3 origin = ignitionPoint != null ? ignitionPoint.position : transform.position + Vector3.up * 0.2f;
            var camera = Camera.main;
            Vector3 horizontal = Vector3.ProjectOnPlane(camera != null ? camera.transform.forward : transform.forward, Vector3.up);
            if (horizontal.sqrMagnitude < 0.0001f)
                horizontal = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.forward;
            horizontal.Normalize();
            // The burst happens both ABOVE and AWAY FROM the user, not near the controller.
            Vector3 skyTarget = origin + horizontal * 42f + Vector3.up * Mathf.Max(32f, minimumBurstHeightAboveLaunch);
            var projectile = new GameObject("Firework Sky Projectile");
            projectile.transform.position = origin;
            projectile.transform.rotation = Quaternion.LookRotation((skyTarget - origin).normalized, Vector3.up);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Projectile Glow";
            visual.transform.SetParent(projectile.transform, false);
            visual.transform.localScale = Vector3.one * 0.055f;
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.TrailMaterial;
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.sharedMaterial = SharedVisuals.TrailMaterial;
            trail.time = 0.28f;
            trail.startWidth = 0.017f;
            trail.endWidth = 0.001f;
            trail.minVertexDistance = 0.02f;
            trail.numCapVertices = 3;
            trail.emitting = false;
            var cg = new Gradient();
            var chosenPalette = ChoosePalette();
            cg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(FireworkPaletteUtil.RandomColor(chosenPalette), 1) },
                new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = cg;
            var light = projectile.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.3f;
            light.intensity = 2.0f;
            light.color = useCustomColor ? customColor : FireworkPaletteUtil.RandomColor(chosenPalette);
            light.shadows = LightShadows.None;
            var flight = projectile.AddComponent<PrettyProjectileFlight>();
            flight.Init(origin, skyTarget, Mathf.Max(2.0f, rocketFlightSeconds),
                ChoosePattern(), chosenPalette, useCustomColor, customColor,
                showMode, crackleOnly, Quaternion.LookRotation((skyTarget - (camera != null ? camera.transform.position : origin)).normalized, Vector3.up));
            Debug.Log($"[Firework SKY] Launch source={name} start={origin:F2}, target={skyTarget:F2}, height={skyTarget.y - origin.y:F1}m, mode={showMode}, camera={((camera != null) ? camera.name : "none")}, scene={gameObject.scene.name}");
        }

        FireworkPalette ChoosePalette()
        {
            if (!randomPalette) return fixedPalette;
            FireworkPalette[] choices = { FireworkPalette.Cyan, FireworkPalette.Pink, FireworkPalette.Violet,
                FireworkPalette.Ice, FireworkPalette.Mixed, FireworkPalette.Gold, FireworkPalette.Green,
                FireworkPalette.Red, FireworkPalette.Rainbow, FireworkPalette.Orange, FireworkPalette.Amber,
                FireworkPalette.RoyalBlue, FireworkPalette.Teal, FireworkPalette.Magenta, FireworkPalette.Lime,
                FireworkPalette.Sunset, FireworkPalette.Aurora, FireworkPalette.Candy };
            return choices[Random.Range(0, choices.Length)];
        }

        FireworkPattern ChoosePattern()
        {
            if (!randomPattern) return fixedPattern;
            FireworkPattern[] patterns =
            {
                FireworkPattern.Ring,
                FireworkPattern.DoubleRing,
                FireworkPattern.Flower,
                FireworkPattern.Peony,
                FireworkPattern.Willow,
                FireworkPattern.CometBurst,
                FireworkPattern.Star,
                FireworkPattern.Heart,
                FireworkPattern.Spiral,
                FireworkPattern.Crown,
                FireworkPattern.Snowflake,
                FireworkPattern.Butterfly, FireworkPattern.Palm, FireworkPattern.Galaxy,
                FireworkPattern.Diamond, FireworkPattern.FourLeaf, FireworkPattern.Cross,
                FireworkPattern.Fan, FireworkPattern.DoubleFlower
            };
            return patterns[Random.Range(0, patterns.Length)];
        }

        public void ResetAfterLaunch()
        {
            _activated = false;
            _grabbed = false;
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
            transform.position = _displayPos;
            transform.rotation = _displayRot;
        }
    }

}
