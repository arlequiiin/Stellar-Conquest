using UnityEngine;
using UnityEngine.AI;

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
    private AudioSource audioSource;

    private float lastAttackTime = 0f;

    private Entity currentTargetEntity;

    protected override void Awake() {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    protected override void Start() { 
        base.Start(); 
        AssignRole();
    }

    private void Update() {
        Collider2D colTarget = FindTargetInVision();
        if (colTarget != null) {
            Entity targetEntity = colTarget.GetComponent<Entity>();
            if (targetEntity != null) {
                currentTargetEntity = targetEntity;

                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);

                if (dist > attackRange) {
                    agent.SetDestination(targetEntity.transform.position); 
                }
                else {
                    agent.ResetPath();
                    // Стреляем!
                    if (Time.time >= lastAttackTime + attackCooldown) {
                        ShootAtTarget(targetEntity);
                        lastAttackTime = Time.time;
                    }
                }
                return;
            }
        }

        currentTargetEntity = null;
        if (CurrentRole == Role.Defender)
            DefenderLogic();
        else
            AttackerLogic();
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
        Collider2D target = Physics2D.OverlapCircle(transform.position, visionRange, playerUnitsLayer);
        if (target != null)
            return target;

        Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, visionRange, playerBuildingsLayer);
        foreach (var col in candidates) {
            var building = col.GetComponent<Buildings>();
            if (building != null && building.IsCompleted)
                return col;
        }

        return null;
    }


    void ShootAtTarget(Entity target) {
        if (bulletPrefab == null || firePoint == null) return;

        var bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) {
            bullet.Init(target, attackDamage);
        }

        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }


    public override void TakeDamage(float amount) {
        base.TakeDamage(amount);
    }

    protected override void Die() {
        manager.RemoveEnemy(this);
        base.Die();
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = CurrentRole == Role.Defender ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
