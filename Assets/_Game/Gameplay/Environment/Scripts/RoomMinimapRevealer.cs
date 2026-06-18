using UnityEngine;

namespace Seventh.Gameplay.Environment
{
    [RequireComponent(typeof(RoomTrigger))]
    public class RoomMinimapRevealer : MonoBehaviour
    {
        private SpriteMask _spriteMask;
        private RoomTrigger _roomTrigger;

        private void Awake()
        {
            _roomTrigger = GetComponent<RoomTrigger>();
        }

        private void Start()
        {
            CreateMinimapMask();
            CheckInitialPlayerPresence();
        }

        private void CreateMinimapMask()
        {
            BoxCollider2D roomCollider = _roomTrigger.RoomCollider;
            if (roomCollider == null) return;

            // Cria o objeto da máscara
            GameObject maskObj = new GameObject("Minimap_Mask_" + _roomTrigger.RoomId);
            maskObj.transform.SetParent(transform);
            maskObj.transform.position = roomCollider.bounds.center;

            // Adiciona o componente SpriteMask
            _spriteMask = maskObj.AddComponent<SpriteMask>();
            
            // Cria um sprite dinâmico 1x1 branco para preencher a máscara
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            
            _spriteMask.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            // Redimensiona a máscara para o tamanho exato da sala
            maskObj.transform.localScale = new Vector3(roomCollider.bounds.size.x, roomCollider.bounds.size.y, 1f);

            // Começa desativada (invisível)
            _spriteMask.enabled = false;
        }

        private void CheckInitialPlayerPresence()
        {
            var player = FindAnyObjectByType<Seventh.Gameplay.Player.PlayerController>();
            if (player != null && _roomTrigger.RoomCollider != null)
            {
                if (_roomTrigger.RoomCollider.OverlapPoint(player.transform.position))
                {
                    RevealRoom();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Seventh.Gameplay.Player.PlayerController>() != null)
            {
                RevealRoom();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Seventh.Gameplay.Player.PlayerController>() != null)
            {
                RevealRoom();
            }
        }

        public void RevealRoom()
        {
            if (_spriteMask != null && !_spriteMask.enabled)
            {
                _spriteMask.enabled = true;
                Debug.Log($"[RoomMinimapRevealer] Revelou a máscara da sala {_roomTrigger.RoomId}");
            }
        }
    }
}