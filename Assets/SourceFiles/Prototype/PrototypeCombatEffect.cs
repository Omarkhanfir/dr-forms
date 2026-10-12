using UnityEngine;
using UnityEngine.Rendering;

namespace DrForms.Prototype
{
    // One shared-material cube per burst: no particles, lights, or material instances.
    public sealed class PrototypeCombatEffect : MonoBehaviour
    {
        private float started, duration, size;
        public static void Spawn(Vector3 position, Material material, Color color, float size, float duration, string name)
        {
            var burst = GameObject.CreatePrimitive(PrimitiveType.Cube);
            burst.name = name;
            var collider = burst.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            burst.transform.position = position;
            burst.transform.localScale = Vector3.one * size;
            var renderer = burst.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
            var effect = burst.AddComponent<PrototypeCombatEffect>();
            effect.started = Time.time;
            effect.duration = duration;
            effect.size = size;
        }
        private void Update()
        {
            var progress = (Time.time - started) / duration;
            if (progress >= 1) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * size * (1 + progress) * (1 - progress);
            transform.Rotate(0, 180 * Time.deltaTime, 90 * Time.deltaTime);
        }
    }
}
