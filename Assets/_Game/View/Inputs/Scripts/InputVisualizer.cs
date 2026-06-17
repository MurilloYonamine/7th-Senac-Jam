using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia a exibição dos sprites de entrada, trocando conforme o dispositivo
/// e dando feedback visual (flash) quando a ação é executada.
/// </summary>
public class InputVisualizer : MonoBehaviour
{
    // ----- Referências às Input Actions -----
    [Header("Input Actions")]
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference menuAction;

    // ----- Imagens UI para exibir os sprites -----
    [Header("UI Images")]
    [SerializeField] private Image dashImage;
    [SerializeField] private Image attackImage;
    [SerializeField] private Image menuImage;
    [SerializeField] private Image moveImage;     // imagem única que muda conforme direção

    // ----- Sprites para Teclado -----
    [Header("Keyboard Sprites")]
    [SerializeField] private Sprite dashKeyboard;
    [SerializeField] private Sprite attackKeyboard;
    [SerializeField] private Sprite menuKeyboard;
    [SerializeField] private Sprite moveUpKeyboard;
    [SerializeField] private Sprite moveDownKeyboard;
    [SerializeField] private Sprite moveLeftKeyboard;
    [SerializeField] private Sprite moveRightKeyboard;
    [SerializeField] private Sprite moveIdleKeyboard;

    // ----- Sprites para Gamepad -----
    [Header("Gamepad Sprites")]
    [SerializeField] private Sprite dashGamepad;
    [SerializeField] private Sprite attackGamepad;
    [SerializeField] private Sprite menuGamepad;
    [SerializeField] private Sprite moveUpGamepad;
    [SerializeField] private Sprite moveDownGamepad;
    [SerializeField] private Sprite moveLeftGamepad;
    [SerializeField] private Sprite moveRightGamepad;
    [SerializeField] private Sprite moveIdleGamepad;

    // ----- Configurações de Flash -----
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.1f;

    // Controle de estado atual
    private bool usingGamepad = false;
    private Vector2 lastMoveDirection = Vector2.zero;
    private Coroutine flashCoroutine;

    // Dicionário para facilitar a troca de sprites da movimentação
    private Dictionary<Vector2, Sprite> moveSpritesKeyboard;
    private Dictionary<Vector2, Sprite> moveSpritesGamepad;

    private void Awake()
    {
        // Inicializa os dicionários de movimentação
        moveSpritesKeyboard = new Dictionary<Vector2, Sprite>
        {
            { Vector2.up, moveUpKeyboard },
            { Vector2.down, moveDownKeyboard },
            { Vector2.left, moveLeftKeyboard },
            { Vector2.right, moveRightKeyboard },
            { Vector2.zero, moveIdleKeyboard }
        };

        moveSpritesGamepad = new Dictionary<Vector2, Sprite>
        {
            { Vector2.up, moveUpGamepad },
            { Vector2.down, moveDownGamepad },
            { Vector2.left, moveLeftGamepad },
            { Vector2.right, moveRightGamepad },
            { Vector2.zero, moveIdleGamepad }
        };
    }

    private void OnEnable()
    {
        // Assina os eventos de perform das ações
        if (dashAction != null) dashAction.action.performed += OnDashPerformed;
        if (attackAction != null) attackAction.action.performed += OnAttackPerformed;
        if (menuAction != null) menuAction.action.performed += OnMenuPerformed;
        if (moveAction != null) moveAction.action.performed += OnMovePerformed;

        // Define o dispositivo inicial (verifica se há gamepad conectado)
        UpdateDeviceType();
        UpdateAllSprites();
    }

    private void OnDisable()
    {
        // Remove os eventos
        if (dashAction != null) dashAction.action.performed -= OnDashPerformed;
        if (attackAction != null) attackAction.action.performed -= OnAttackPerformed;
        if (menuAction != null) menuAction.action.performed -= OnMenuPerformed;
        if (moveAction != null) moveAction.action.performed -= OnMovePerformed;

    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (usingGamepad)
            {
                usingGamepad = false;
                UpdateAllSprites();
            }
        }

        if (moveAction != null)
        {
            Vector2 currentMove = SnapDirection(moveAction.action.ReadValue<Vector2>());
            if (currentMove != lastMoveDirection)
            {
                lastMoveDirection = currentMove;
                UpdateMoveSprite();
                TriggerFlash(moveImage);
            }
        }
    }

    // ------------------- Eventos de Input -------------------

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        UpdateDeviceType(ctx);
        UpdateAllSprites();
        TriggerFlash(dashImage);
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        UpdateDeviceType(ctx);
        UpdateAllSprites();
        TriggerFlash(attackImage);
    }

    private void OnMenuPerformed(InputAction.CallbackContext ctx)
    {
        UpdateDeviceType(ctx);
        UpdateAllSprites();
        TriggerFlash(menuImage);
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        UpdateDeviceType(ctx);
        UpdateAllSprites();
        // Opcional: flash na imagem de movimento (se quiser que pisque ao mexer)
        // TriggerFlash(moveImage);
    }

    // ------------------- Detecção de Dispositivo -------------------

    private void UpdateDeviceType()
    {
        usingGamepad = Gamepad.current != null;
    }

    private void UpdateDeviceType(InputAction.CallbackContext ctx)
    {
        usingGamepad = ctx.control != null && ctx.control.device is Gamepad;
    }

    // ------------------- Atualização dos Sprites -------------------

    private void UpdateAllSprites()
    {
        UpdateDashSprite();
        UpdateAttackSprite();
        UpdateMenuSprite();
        UpdateMoveSprite();
    }

    private void UpdateDashSprite()
    {
        dashImage.sprite = usingGamepad ? dashGamepad : dashKeyboard;
    }

    private void UpdateAttackSprite()
    {
        attackImage.sprite = usingGamepad ? attackGamepad : attackKeyboard;
    }

    private void UpdateMenuSprite()
    {
        menuImage.sprite = usingGamepad ? menuGamepad : menuKeyboard;
    }

    private void UpdateMoveSprite()
    {
        var dict = usingGamepad ? moveSpritesGamepad : moveSpritesKeyboard;
        if (dict.TryGetValue(lastMoveDirection, out Sprite sprite))
        {
            moveImage.sprite = sprite;
        }
        else
        {
            // Fallback para idle
            moveImage.sprite = dict[Vector2.zero];
        }
    }

    // ------------------- Lógica de Flash -------------------

    private void TriggerFlash(Image targetImage)
    {
        if (targetImage == null) return;

        // Cancela qualquer flash anterior
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine(targetImage));
    }

    private IEnumerator FlashCoroutine(Image targetImage)
    {
        Color originalColor = targetImage.color;
        targetImage.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        targetImage.color = originalColor;
        flashCoroutine = null;
    }

    // ------------------- Utilitários -------------------

    private Vector2 SnapDirection(Vector2 input)
    {
        // Decide a direção predominante (eixos principais)
        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);

        if (absX < 0.1f && absY < 0.1f)
            return Vector2.zero;

        if (absX > absY)
            return new Vector2(Mathf.Sign(input.x), 0);
        else
            return new Vector2(0, Mathf.Sign(input.y));
    }
}