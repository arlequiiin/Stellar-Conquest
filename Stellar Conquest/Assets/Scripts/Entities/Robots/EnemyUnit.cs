using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class EnemyUnit : Entity {
    public enum Role { Defender, Attacker }
    public Role CurrentRole;

    [HideInInspector] public EnemyManager manager;

    private NavMeshAgent agent;
    private Transform attackTarget;
    private Transform patrolTarget;

    public float visionRange = 6f;
    public LayerMask playerUnitsLayer;
    public LayerMask playerBuildingsLayer;

    [Header("Стрельба врага")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab; 
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private LayerMask obstacleLayer; 
    private AudioSource audioSource;

    private float lastAttackTime = 0f;

    private Entity currentTargetEntity;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    protected override void Awake() {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Start() { 
        base.Start(); 
        AssignRole();
    }

    private void Update() {
        if (!IsAlive) {
            return;
        }
        bool isMoving = agent.velocity.magnitude > 0.1f;
        if (animator != null) {
            animator.SetBool("IsMoving", isMoving);
        }

        if (isMoving && agent.velocity.magnitude > 0.01f && spriteRenderer != null) {
            Vector2 direction = agent.velocity.normalized;
            FlipSprite(direction);
        }

        Collider2D colTarget = FindTargetInVision();
        if (colTarget != null) {
            Entity targetEntity = colTarget.GetComponent<Entity>();
            if (targetEntity != null) {
                currentTargetEntity = targetEntity;

                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);

                if (dist > attackRange) {
                    agent.SetDestination(targetEntity.transform.position);
                    if (animator != null) {
                        animator.SetBool("IsFiring", false);
                    }
                }
                else {
                    agent.ResetPath();

                    if (spriteRenderer != null) {
                        Vector2 directionToTarget = (targetEntity.transform.position - transform.position).normalized;
                        FlipSprite(directionToTarget);
                    }

                    if (!CanSeeTarget(targetEntity)) {
                        // agent.SetDestination(targetEntity.transform.position);
                        // agent.isStopped = false;
                        return; 
                    }
                    if (Time.time >= lastAttackTime + attackCooldown) {
                        ShootAtTarget(targetEntity);
                        lastAttackTime = Time.time;
                    }
                }
                return;
            }
        }

        currentTargetEntity = null;
        if (animator != null) {
            animator.SetBool("IsFiring", false);
        }

        if (CurrentRole == Role.Defender)
            DefenderLogic();
        else
            AttackerLogic();
    }

    protected bool CanSeeTarget(Entity target) {
        if (target == null) return false;

        Vector2 startPoint = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 endPoint = (Vector2)target.transform.position;

        Vector2 direction = (endPoint - startPoint).normalized;
        float distance = Vector2.Distance(startPoint, endPoint);

        RaycastHit2D hit = Physics2D.Raycast(startPoint, direction, distance, obstacleLayer);
        if (hit.collider == null) {
            return true; 
        }
        else {       
            return hit.collider.gameObject == target.gameObject;
        }
    }

    void AssignRole() {
        CurrentRole = manager.GetRoleForUnit(this);
        if (CurrentRole == Role.Defender)
            patrolTarget = manager.GetPatrolPoint();
        else
            attackTarget = manager.playerBase;
    }

    void DefenderLogic() {
        if (patrolTarget == null || Vector3.Distance(transform.position, patrolTarget.position) < 1f)
            patrolTarget = manager.GetPatrolPoint();
        agent.SetDestination(patrolTarget.position);
    }

    void AttackerLogic() {
        if (attackTarget == null)
            attackTarget = manager.playerBase;
        if (Vector3.Distance(transform.position, attackTarget.position) < 1.5f)
            attackTarget = manager.GetAttackDirection();
        agent.SetDestination(attackTarget.position);
    }

    Collider2D FindTargetInVision() {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, visionRange, playerUnitsLayer);

        foreach (var target in targets) {
            Units unit = target.GetComponent<Units>();
            if (unit != null && unit.IsAlive) {
                return target;
            }
        }

        foreach (var target in targets) {
            Buildings building = target.GetComponent<Buildings>();
            if (building != null && building.IsCompleted) {
                return target;
            }
        }

        return null;
    }


    void ShootAtTarget(Entity target) {
        if (bulletPrefab == null || firePoint == null) return;
        if (animator != null) {
            animator.SetBool("IsFiring", true);
        }

        var bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) {
            bullet.Init(target, attackDamage);
        }

        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    void FlipSprite(Vector2 direction) {
        if (spriteRenderer == null) return;

        if (direction.x > 0.1f) {
            spriteRenderer.flipX = false;
        }
        else if (direction.x < -0.1f) {
            spriteRenderer.flipX = true;
        }
    }

    public override void TakeDamage(float amount) {
        base.TakeDamage(amount);
    }

    protected override void Die() {
        base.Die();

        if (agent != null) {
            agent.enabled = false;
        }
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsFiring", false);
        if (animator != null) {
            animator.SetTrigger("Die"); 
        }

        manager.RemoveEnemy(this);
    }
}
