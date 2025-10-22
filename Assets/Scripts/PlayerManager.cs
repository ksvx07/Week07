using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    // Hack ;Input ����
    private int currentPlayer = 0;
    private int selectPlayer = 0;
    private int highlightPlayer = 0;
    private bool isSelectUIActive = false;  

    [SerializeField] private int startPlayer = 0;
    [SerializeField] private List<GameObject> players;
    [SerializeField] private List<Image> pannels;
    [SerializeField] private Color originColor;
    [SerializeField] private Color highLightColor;


    [SerializeField] private CameraController camControlelr;
    [SerializeField] private GameObject selectPlayerPanel;

    public GameObject _currentPlayerPrefab { get; private set; }
    private PlayerInput inputActions;

    public bool IsHold { get; private set; }
    public bool IsTimeSlow { get; private set; }
    private Vector3 _MaxScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private float _selectPanelSpeed = 60f;
    private Coroutine pannelActive;

    public static PlayerManager Instance;

    private void Awake()
    {
        if (null == Instance)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        inputActions = new PlayerInput();

        selectPlayer = startPlayer;
        currentPlayer = selectPlayer;
        highlightPlayer = selectPlayer;
        _currentPlayerPrefab = players[currentPlayer];
        ActiveStartPlayer(startPlayer);
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();

        inputActions.UI.SwitchHold.performed += OnSwithPlayerHold; 

        inputActions.UI.SwitchHold.canceled += OnSwitchPlayerCancled;

        inputActions.UI.SelectPlayer.performed += ChangeSelectPlayer;
    }

    private void OnDisable()
    {

        inputActions.UI.SwitchHold.performed -= OnSwithPlayerHold; 

        inputActions.UI.SwitchHold.canceled -= OnSwitchPlayerCancled;

        inputActions.UI.SelectPlayer.performed -= ChangeSelectPlayer;
        inputActions.UI.Disable();
    }

    private void ChangeSelectPlayer(InputAction.CallbackContext context)
    {
        if (!isSelectUIActive) return;

        Vector2 inputVector = context.ReadValue<Vector2>();

        // 입력이 너무 작으면 무시 (데드존)
        if (inputVector.magnitude < 0.5f) return;

        // 가장 강한 축을 기준으로 방향 결정
        if (Mathf.Abs(inputVector.y) > Mathf.Abs(inputVector.x))
        {
            // 세로 축이 더 강함
            if (inputVector.y > 0) // 위쪽
            {
                selectPlayer = 0;
            }
            else // 아래쪽
            {
                selectPlayer = 2;
            }
        }
        else
        {
            // 가로 축이 더 강함
            if (inputVector.x > 0) // 오른쪽
            {
                selectPlayer = 1;
            }
            else // 왼쪽
            {
                selectPlayer = 3;
            }
        }

        HighLightSelectPlayer(highlightPlayer, selectPlayer);
        highlightPlayer = selectPlayer;
    }

    private void SlowTimeScale()
    {
        if (IsTimeSlow) return;
        IsTimeSlow = true;
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OriginalTimeScale()
    {
        IsTimeSlow = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void AcitveSelectUI()
    {
        HighLightSelectPlayer(highlightPlayer, selectPlayer);
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(_currentPlayerPrefab.transform.position);
        selectPlayerPanel.GetComponent<RectTransform>().position = screenPosition;

        if (pannelActive != null)
        {
            StopCoroutine(pannelActive);
        }
        pannelActive = StartCoroutine(ScaleOverTime());
        selectPlayerPanel.SetActive(true);
        isSelectUIActive = true; 
    }

    private void DeActiveSelectUI()
    {
        if (pannelActive != null)
        {
            StopCoroutine(pannelActive);
        }
        IsHold = false;
        selectPlayerPanel.SetActive(false);
        isSelectUIActive = false;
    }


    public void OnSwithPlayerHold(InputAction.CallbackContext context)
    {
        SlowTimeScale();

        if (context.phase == InputActionPhase.Performed)
        {
            if (!isSelectUIActive)
            {
                IsHold = true;
                AcitveSelectUI();
            }
        }
    }

    public void OnSwitchPlayerCancled(InputAction.CallbackContext context)
    {
        if (isSelectUIActive)
        {
            DeActiveSelectUI();
            ActiveSelectPlayer(currentPlayer, selectPlayer);
        }
    }

    public void OnPlayerDead()
    {
        if (isSelectUIActive)
        {
            DeActiveSelectUI();
            ActiveSelectPlayer(currentPlayer, selectPlayer);
        }
    }

    private void HighLightSelectPlayer(int oldPlayer, int newPlayer)
    {
        pannels[oldPlayer].color = originColor;
        pannels[newPlayer].color = highLightColor;
    }
    private void ActiveStartPlayer(int starstPlayer)
    {
        _currentPlayerPrefab = players[starstPlayer];
        _currentPlayerPrefab.SetActive(true);
        currentPlayer = selectPlayer;
    }

    public void PlayerSetActive(bool isAcitve)
    {
        _currentPlayerPrefab.SetActive(isAcitve);
    }
    private void ActiveSelectPlayer(int oldPlayer, int newPlayer)
    {
        OriginalTimeScale();

        HighLightSelectPlayer(oldPlayer, newPlayer);
        if (oldPlayer == 2 && newPlayer == 2) return;

        GameObject oldPlayerPrefab = players[oldPlayer];
        Transform lastPos = oldPlayerPrefab.transform;
        Vector2 lastVelocity = oldPlayerPrefab.GetComponent<Rigidbody2D>().linearVelocity;
        oldPlayerPrefab.SetActive(false);

        _currentPlayerPrefab = players[newPlayer];
        _currentPlayerPrefab.transform.position = lastPos.position;
        _currentPlayerPrefab.SetActive(true);
        _currentPlayerPrefab.GetComponent<IPlayerController>().OnEnableSetVelocity(lastVelocity.x, lastVelocity.y);

        currentPlayer = selectPlayer;
        highlightPlayer = currentPlayer;
    }

    IEnumerator ScaleOverTime()
    {
        selectPlayerPanel.SetActive(true);
        selectPlayerPanel.transform.localScale = Vector3.zero;

        Vector3 initialScale = selectPlayerPanel.transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _selectPanelSpeed)
        {
            selectPlayerPanel.transform.position = _currentPlayerPrefab.transform.position;

            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _selectPanelSpeed);

            selectPlayerPanel.transform.localScale = Vector3.Lerp(initialScale, _MaxScale, t);

            yield return null;
        }
        while (true)
        {
            selectPlayerPanel.transform.position = _currentPlayerPrefab.transform.position;
            yield return null;
        }
    }

}
