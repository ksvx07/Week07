using UnityEngine;

public class RingTrigger : MonoBehaviour
{
    [SerializeField] private Collider2D[] frontRings;
    [SerializeField] private Collider2D[] backRings;
    [SerializeField] private bool startWithFrontRings = true;

    public void OnRingFrontEnter()
    {
        foreach (var frontRing in frontRings)
        {
            frontRing.enabled = true;
        }
        foreach (var backRing in backRings)
        {
            backRing.enabled = false;
        }
    }

    public void OnRingBackEnter()
    {
        foreach (var frontRing in frontRings)
        {
            frontRing.enabled = false;
        }
        foreach (var backRing in backRings)
        {
            backRing.enabled = true;
        }
    }

    private void Start()
    {
        if (startWithFrontRings)
        {
            foreach (var frontRing in frontRings)
            {
                frontRing.enabled = true;
            }
            foreach (var backRing in backRings)
            {
                backRing.enabled = false;
            }
        }
        else
        {
            foreach (var frontRing in frontRings)
            {
                frontRing.enabled = false;
            }
            foreach (var backRing in backRings)
            {
                backRing.enabled = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.transform.position.x > transform.position.x)
            {
                foreach (var frontRing in frontRings)
                {
                    frontRing.enabled = true;
                }
                foreach (var backRing in backRings)
                {
                    backRing.enabled = false;
                }
            }
            else
            {
                foreach (var frontRing in frontRings)
                {
                    frontRing.enabled = false;
                }
                foreach (var backRing in backRings)
                {
                    backRing.enabled = true;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            foreach (var frontRing in frontRings)
            {
                frontRing.enabled = true;
            }
            foreach (var backRing in backRings)
            {
                backRing.enabled = true;
            }
        }
    }
}
