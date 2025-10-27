using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private Image FadeOutImage;
    [SerializeField] private GameObject EndingUI;
    [SerializeField] private Button EndGame;

    private void Awake()
    {
        EndGame.onClick.AddListener(() => Application.Quit());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(FadeOutAndShowEnding());
        }
    }

    private IEnumerator FadeOutAndShowEnding()
    {
        float fadeDuration = 2f;
        float elapsedTime = 0f;
        Color color = FadeOutImage.color;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            FadeOutImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        FadeOutImage.color = color;
        
        EndingUI.SetActive(true);
    }
}
