using UnityEngine;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    [SerializeField] private Button StartBtn;

    private void Awake()
    {
        StartBtn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("InGame"));
    }
}
