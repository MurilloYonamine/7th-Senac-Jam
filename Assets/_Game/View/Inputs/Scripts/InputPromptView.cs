using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

namespace Seventh.View.Inputs
{
    public enum InputDeviceType
    {
        KeyboardAndMouse,
        Xbox,
        PlayStation
    }

    [System.Serializable]
    public struct PromptSprites
    {
        [SerializeField] private InputActionReference _actionReference;
        [SerializeField] private Image _image;
        [SerializeField] private TMP_Text _text;
        public Sprite[] Normal;
        public Sprite[] Pressed;

        public InputActionReference ActionReference => _actionReference;
        public Image Image => _image;
        public TMP_Text Text => _text;
    }

    public class InputPromptView : MonoBehaviour
    {
        [Header("Teclado & Mouse Inputs")]
        [SerializeField] private PromptSprites[] _keyboardInputs;

        [Header("Xbox Inputs")]
        [SerializeField] private PromptSprites[] _xboxInputs;

        [Header("PlayStation Inputs")]
        [SerializeField] private PromptSprites[] _playstationInputs;

        private InputDeviceType _currentDeviceType = InputDeviceType.KeyboardAndMouse;
        private HashSet<InputAction> _subscribedActions = new HashSet<InputAction>();

        private void Start()
        {
            UpdatePromptVisuals();
        }

        private void OnEnable()
        {
            // Listen for global action executions to detect active devices dynamically
            InputSystem.onActionChange += OnActionGlobalChange;
            SubscribeToAllActions();
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionGlobalChange;
            UnsubscribeFromAllActions();
        }

        private void SubscribeToAllActions()
        {
            _subscribedActions.Clear();

            AddActionsFromList(_keyboardInputs);
            AddActionsFromList(_xboxInputs);
            AddActionsFromList(_playstationInputs);

            foreach (var action in _subscribedActions)
            {
                action.started += OnActionChangedCallback;
                action.performed += OnActionChangedCallback;
                action.canceled += OnActionChangedCallback;

                // Ensure the referenced action is enabled
                if (!action.enabled)
                {
                    action.Enable();
                }
            }
        }

        private void UnsubscribeFromAllActions()
        {
            foreach (var action in _subscribedActions)
            {
                action.started -= OnActionChangedCallback;
                action.performed -= OnActionChangedCallback;
                action.canceled -= OnActionChangedCallback;
            }
            _subscribedActions.Clear();
        }

        private void AddActionsFromList(PromptSprites[] list)
        {
            if (list == null) return;
            foreach (var item in list)
            {
                if (item.ActionReference != null && item.ActionReference.action != null)
                {
                    _subscribedActions.Add(item.ActionReference.action);
                }
            }
        }

        private void OnActionGlobalChange(object obj, InputActionChange change)
        {
            // Detect device switch whenever any action is started/performed in the system
            if (change == InputActionChange.ActionStarted || change == InputActionChange.ActionPerformed)
            {
                if (obj is InputAction action && action.activeControl != null)
                {
                    UpdateDeviceType(action.activeControl.device);
                }
            }
        }

        private void OnActionChangedCallback(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx.control.device);
            UpdatePromptVisuals();
        }

        private void UpdateDeviceType(InputDevice device)
        {
            if (device == null) return;

            InputDeviceType newType = InputDeviceType.Xbox; // Default fallback for gamepads

            if (device is Keyboard || device is Mouse)
            {
                newType = InputDeviceType.KeyboardAndMouse;
            }
            else
            {
                string deviceName = device.name.ToLower();
                string product = device.description.product != null ? device.description.product.ToLower() : "";
                string manufacturer = device.description.manufacturer != null ? device.description.manufacturer.ToLower() : "";

                // Check for PlayStation manufacturer, names, or product identifiers (Sony, Dualshock, Dualsense, PS4, PS5, etc.)
                if (deviceName.Contains("playstation") || deviceName.Contains("sony") || 
                    deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || 
                    deviceName.Contains("ps4") || deviceName.Contains("ps5") ||
                    product.Contains("playstation") || product.Contains("sony") || 
                    product.Contains("dualshock") || product.Contains("dualsense") || 
                    product.Contains("ps4") || product.Contains("ps5") ||
                    product.Contains("wireless controller") || 
                    manufacturer.Contains("sony"))
                {
                    newType = InputDeviceType.PlayStation;
                }
                else
                {
                    newType = InputDeviceType.Xbox;
                }
            }

            if (_currentDeviceType != newType)
            {
                _currentDeviceType = newType;
                UpdatePromptVisuals();
            }
        }

        private void UpdatePromptVisuals()
        {
            PromptSprites[] activeInputs = _keyboardInputs;
            PromptSprites[] inactive1 = _xboxInputs;
            PromptSprites[] inactive2 = _playstationInputs;

            switch (_currentDeviceType)
            {
                case InputDeviceType.KeyboardAndMouse:
                    activeInputs = _keyboardInputs;
                    inactive1 = _xboxInputs;
                    inactive2 = _playstationInputs;
                    break;
                case InputDeviceType.Xbox:
                    activeInputs = _xboxInputs;
                    inactive1 = _keyboardInputs;
                    inactive2 = _playstationInputs;
                    break;
                case InputDeviceType.PlayStation:
                    activeInputs = _playstationInputs;
                    inactive1 = _keyboardInputs;
                    inactive2 = _xboxInputs;
                    break;
            }

            // Update active inputs
            if (activeInputs != null)
            {
                foreach (var input in activeInputs)
                {
                    // Update text description
                    if (input.Text != null)
                    {
                        input.Text.gameObject.SetActive(true);
                        if (input.ActionReference != null && input.ActionReference.action != null)
                        {
                            input.Text.text = input.ActionReference.action.name;
                        }
                    }

                    // Update sprite prompt
                    if (input.Image != null)
                    {
                        input.Image.gameObject.SetActive(true);

                        bool actionPressed = false;
                        if (input.ActionReference != null && input.ActionReference.action != null)
                        {
                            actionPressed = input.ActionReference.action.phase == InputActionPhase.Started || 
                                            input.ActionReference.action.phase == InputActionPhase.Performed;
                        }

                        Sprite[] sprites = actionPressed ? input.Pressed : input.Normal;
                        if (sprites != null && sprites.Length > 0)
                        {
                            int spriteIndex = 0;
                            // If it's a multi-sprite input (e.g. 4 directional sprites for movement), resolve based on dominant direction
                            if (sprites.Length > 1 && input.ActionReference != null && input.ActionReference.action != null)
                            {
                                try
                                {
                                    var vecValue = input.ActionReference.action.ReadValue<Vector2>();
                                    if (vecValue.sqrMagnitude > 0.15f)
                                    {
                                        if (Mathf.Abs(vecValue.x) > Mathf.Abs(vecValue.y))
                                        {
                                            spriteIndex = vecValue.x > 0 ? 3 : 1; // Right (3), Left (1)
                                        }
                                        else
                                        {
                                            spriteIndex = vecValue.y > 0 ? 0 : 2; // Up (0), Down (2)
                                        }
                                    }
                                }
                                catch
                                {
                                    // Fallback for non-Vector2 types
                                }
                            }
                            spriteIndex = Mathf.Clamp(spriteIndex, 0, sprites.Length - 1);
                            input.Image.sprite = sprites[spriteIndex];
                        }
                    }
                }
            }

            // Deactivate inactive inputs (only if they aren't shared with the active ones)
            DeactivateUniqueElements(inactive1, activeInputs);
            DeactivateUniqueElements(inactive2, activeInputs);
        }

        private void DeactivateUniqueElements(PromptSprites[] elementsToDisable, PromptSprites[] activeElements)
        {
            if (elementsToDisable == null) return;

            foreach (var elem in elementsToDisable)
            {
                // Deactivate text if not shared
                if (elem.Text != null && !IsTextShared(elem.Text, activeElements))
                {
                    elem.Text.gameObject.SetActive(false);
                }

                // Deactivate image if not shared
                if (elem.Image != null && !IsImageShared(elem.Image, activeElements))
                {
                    elem.Image.gameObject.SetActive(false);
                }
            }
        }

        private bool IsTextShared(TMP_Text text, PromptSprites[] activeElements)
        {
            if (activeElements == null) return false;
            foreach (var active in activeElements)
            {
                if (active.Text == text) return true;
            }
            return false;
        }

        private bool IsImageShared(Image image, PromptSprites[] activeElements)
        {
            if (activeElements == null) return false;
            foreach (var active in activeElements)
            {
                if (active.Image == image) return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdatePromptVisuals();
        }
#endif
    }
}
