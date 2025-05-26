using UnityEngine;

public class Bullet : MonoBehaviour {
    public float speed = 10f;
    private Entity target;
    private float damage;

    public void Init(Entity target, float damage) {
        this.target = target;
        this.damage = damage;
    }

    void Update() {
        if (target == null) {
            Destroy(gameObject);
            return;
        }

        // Движение к цели
        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Разворачиваем пулю по направлению движения (2D TopDown)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Проверка попадания
        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f) {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
