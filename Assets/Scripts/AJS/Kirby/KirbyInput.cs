using UnityEngine;
using UnityEngine.InputSystem;
public class KirbyInput : MonoBehaviour
{
    #region References
    private PlayerInput _inputs;
    private KirbyController _playerMove;
    private KirbyJump _playerJump;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _inputs = new PlayerInput();
        _playerMove = GetComponent<KirbyController>();
        _playerJump = GetComponent<KirbyJump>();
    }
    private void OnEnable()
    {
        EnableInput(); // Input System Ȱ��ȭ
    }

    private void OnDisable()
    {
        DisableInput(); // Input System ��Ȱ��ȭ
    }
    #endregion

    #region Input Event Handler
    private void EnableInput()
    {
        _inputs.Player.Enable(); // ������ ��� �׼Ǹ� Ȱ��ȭ
        _inputs.Player.Move.performed += OnMoveInput;
        _inputs.Player.Move.canceled += OnMoveInput;
        _inputs.Player.Jump.performed += OnJumpClicked;
        _inputs.Player.Dash.performed += OnTurboClicked;
    }

    private void DisableInput()
    {
        _inputs.Player.Move.performed -= OnMoveInput;
        _inputs.Player.Move.canceled -= OnMoveInput;
        _inputs.Player.Jump.performed -= OnJumpClicked;
        _inputs.Player.Dash.performed -= OnTurboClicked;
        _inputs.Player.Disable(); // ��� �׼Ǹ� ��Ȱ��ȭ
        _playerMove.DirectionX = 0f;
    }

    #endregion

    void OnJumpClicked(InputAction.CallbackContext context)
    {
        _playerJump.OnJumpClicked();
    }
    void OnTurboClicked(InputAction.CallbackContext context)
    {
        _playerMove.OnTurboModePressed();
    }
    void OnMoveInput(InputAction.CallbackContext context)
    {
        if (PlayerManager.Instance.IsHold) return;

        _playerMove.OnMoveInput(context.ReadValue<Vector2>());
    }
}
