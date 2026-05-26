using UnityEngine;
using UnityEngine.UI;

namespace OriginXR.Battle
{
    public class BossController : MonoBehaviour
    {
        public Animator animator;
        public Slider hpSlider;
        public string attackTrigger = "Attack";

        public void Attack()
        {
            if (animator != null) animator.SetTrigger(attackTrigger);
        }

        public void SetHP(float ratio)
        {
            if (hpSlider != null) hpSlider.value = ratio;
        }
    }
}
