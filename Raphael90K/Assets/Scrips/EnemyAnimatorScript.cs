using UnityEngine;

public class EnemyAnimatorScript : MonoBehaviour
{
    Animator animator;
    private float timer = 0f;
    private float switchInterval = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void walk()
    {
        animator.SetBool("isWalking", true);
        animator.SetBool("isIdle", false);
        animator.SetBool("isCrouching", false);
        animator.SetBool("isWinking", false);
    }

    public void crouch()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("isCrouching", true);
        animator.SetBool("isWinking", false);
    }

    public void wave()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("isCrouching", false);
        animator.SetBool("isWinking", true);
    }

    public void idle()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", true);
        animator.SetBool("isCrouching", false);
        animator.SetBool("isWinking", false);
    }
}