using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EndingGame : MonoBehaviour
{
    [SerializeField] private GameObject endingGameUI;
    [SerializeField] private Button quitBtn;

    private void Start()
    {
        quitBtn.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        endingGameUI.SetActive(false); // 시작 시 UI 비활성화
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Time.timeScale = 0f; // 게임 일시정지
            endingGameUI.SetActive(true); // 엔딩 UI 활성화
        }
    }
}
