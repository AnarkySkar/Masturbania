using UnityEngine;

namespace Metroidvania
{
    public class PlatformerShadow : MonoBehaviour
    {
        private const float k_PixelSize = 1.0f / 32.0f;

        [SerializeField] private float m_distance;
        [SerializeField] private int m_width, m_height, m_round;
        [SerializeField] private Color m_color;
        [SerializeField] private LayerMask m_groundLayer;

        private Pixel[] _pixels;
        private float _distanceFactor;
        private Vector2 _basePosition;
        private float _halfWidth;
        private ContactFilter2D _contactFilter;
        private readonly RaycastHit2D[] _raycastHit = new RaycastHit2D[1];
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            
            // Check if prefab child exists
            if (transform.childCount == 0)
            {
                Debug.LogError("[PLATFORMER SHADOW] No child prefab found! Disabling component.");
                enabled = false;
                return;
            }
            
            var prefabChild = transform.GetChild(0);
            if (prefabChild == null)
            {
                Debug.LogError("[PLATFORMER SHADOW] Child 0 is null! Disabling component.");
                enabled = false;
                return;
            }
            
            var prefab = prefabChild.GetComponent<SpriteRenderer>();
            if (prefab == null)
            {
                Debug.LogError("[PLATFORMER SHADOW] Child prefab has no SpriteRenderer! Disabling component.");
                enabled = false;
                return;
            }
            
            _pixels = new Pixel[m_width];
            _halfWidth = m_width * k_PixelSize * 0.5f;
            for (int i = 0; i < m_width; i++)
            {
                var pixelRenderer = Instantiate(prefab, _transform);
                if (pixelRenderer == null)
                {
                    Debug.LogError($"[PLATFORMER SHADOW] Failed to instantiate pixel {i}! Disabling component.");
                    enabled = false;
                    return;
                }
                
                _pixels[i].renderer = pixelRenderer;
                _pixels[i].transform = pixelRenderer.transform;
                _pixels[i].renderer.gameObject.SetActive(true);
                _pixels[i].xOffset = i * k_PixelSize - _halfWidth;
            }
            _distanceFactor = 1.0f / m_distance;
            _contactFilter = default(ContactFilter2D);
            _contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            _contactFilter.SetLayerMask(m_groundLayer);
            _contactFilter.SetDepth(0.0f, 0.0f);
        }

        private void LateUpdate()
        {
            // Safety check - skip if not properly initialized
            if (_pixels == null || _transform == null)
            {
                return;
            }
            
            _basePosition = _transform.position;
            for (int i = 0; i < m_width; i++)
            {
                var pixel = _pixels[i];
                
                // Safety check for individual pixels
                if (pixel.renderer == null || pixel.transform == null)
                {
                    continue;
                }
                
                Vector2 rayOrigin = new Vector2(_basePosition.x + pixel.xOffset, _basePosition.y);
                int count = Physics2D.Raycast(rayOrigin, Vector2.down, _contactFilter, _raycastHit, m_distance);
                bool hitted = count > 0;
                if (pixel.renderer.enabled != hitted)
                    pixel.renderer.enabled = hitted;
                if (hitted)
                {
                    var hit = _raycastHit[0];
                    float round = CalculateRound(i);
                    float fadeOut = hit.distance * _distanceFactor;
                    pixel.transform.position = new Vector3(_basePosition.x + pixel.xOffset, hit.point.y);
                    pixel.transform.localScale = new Vector3(1, m_height - round);
                    pixel.renderer.color = new Color(m_color.r, m_color.g, m_color.b, (1 - fadeOut) * m_color.a);
                }
            }
        }

        private float CalculateRound(int i)
        {
            if (i < m_round)
                return (m_round - i) * 2;
            if (i >= m_width - m_round)
                return (i - (m_width - m_round - 1)) * 2;
            return 0f;
        }

        public struct Pixel
        {
            public SpriteRenderer renderer;
            public Transform transform;
            public float xOffset;
        }
    }
}