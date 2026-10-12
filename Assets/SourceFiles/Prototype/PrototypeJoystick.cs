using UnityEngine;
using UnityEngine.EventSystems;

namespace DrForms.Prototype
{
    public sealed class PrototypeJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public PrototypeControls Owner { private get; set; }
        public RectTransform Knob { private get; set; }
        public Vector2 Value { get; private set; }
        public float Radius => ((RectTransform)transform).rect.width * 0.35f;
        private int? pointerId;

        public void OnPointerDown(PointerEventData data)
        {
            if (!Owner.TouchMode || !Owner.HasFocus || pointerId.HasValue) return;
            pointerId = data.pointerId;
            SetPosition(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (pointerId != data.pointerId || !Owner.HasFocus) return;
            if (data.position.x < 0 || data.position.y < 0
                || data.position.x >= Screen.width || data.position.y >= Screen.height)
            {
                ResetInput();
                return;
            }
            SetPosition(data);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (pointerId == data.pointerId) ResetInput();
        }

        private void SetPosition(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
                data.position, data.pressEventCamera, out var local)) return;
            var raw = Vector2.ClampMagnitude(local / Radius, 1);
            const float deadZone = 0.12f;
            Value = raw.magnitude <= deadZone ? Vector2.zero
                : raw.normalized * ((raw.magnitude - deadZone) / (1 - deadZone));
            Knob.anchoredPosition = raw * Radius;
        }

        public void ResetInput()
        {
            pointerId = null;
            Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }

        private void OnDisable() => ResetInput();
    }
}
