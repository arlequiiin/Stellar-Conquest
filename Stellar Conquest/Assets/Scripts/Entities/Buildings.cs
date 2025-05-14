using UnityEngine;

public abstract class Buildings : Entity {

    protected override void Start() {
        base.Start();
        Debug.Log($"{gameObject.name} построено");
    }

    protected override void Die() {
        Debug.Log($"Здание {gameObject.name} уничтожено");
        base.Die();
    }
}
