using UnityEngine;

public abstract class Buildings : Entity {

    protected override void Start() {
        base.Start();
        Debug.Log($"{gameObject.name} ���������");
    }

    protected override void Die() {
        Debug.Log($"������ {gameObject.name} ����������");
        base.Die();
    }
}
