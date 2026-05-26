using UnityEngine;
using TMPro;

namespace OriginXR.Battle
{
    public class ComboEffectController : MonoBehaviour
    {
        public TextMeshProUGUI comboText;
        private int _combo;

        public void Add()
        {
            _combo++;
            if (comboText != null) { comboText.gameObject.SetActive(_combo >= 3); comboText.text = $"🔥 {_combo}连击!"; }
        }

        public void Reset()
        {
            _combo = 0;
            if (comboText != null) comboText.gameObject.SetActive(false);
        }
    }
}
