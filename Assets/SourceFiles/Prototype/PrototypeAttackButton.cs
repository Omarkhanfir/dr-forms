using UnityEngine;
using UnityEngine.EventSystems;

namespace DrForms.Prototype
{
    public sealed class PrototypeAttackButton : MonoBehaviour, IPointerDownHandler
    {
        public PrototypeControls Owner { private get; set; }
        public void OnPointerDown(PointerEventData data)
        {
            if (Owner.TouchMode && data.button == PointerEventData.InputButton.Left) Owner.RequestAttack();
        }
    }
}
