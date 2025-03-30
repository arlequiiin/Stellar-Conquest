using UnityEngine;
using UnityEngine.AI;

public class SoldierBlueVisual : MonoBehaviour
{
    private Animator animator;
    private GameObject outlineObject;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        outlineObject = transform.Find("SoliderBlueVisual/OutlineRenderer").gameObject;
        outlineObject.SetActive(false);
    }

    void Update()
    {
        animator.SetBool("isMoving", agent.velocity.magnitude > 0.1f);
    }

    public void Select() {
        outlineObject.SetActive(true);
    }

    public void Deselect() {
        outlineObject.SetActive(false);
    }

    public void SetMoving(bool isMoving) {
        animator.SetBool("isMoving", isMoving);
    }

    public void Shoot() {
        animator.SetTrigger("isShooting");
    }

    public void Reload() {
        animator.SetTrigger("isReloading");
    }

    public void Die() {
        animator.SetBool("isDead", true);
    }
}
