using UnityEngine;

public class Generator : Buildings {
    [SerializeField] private float powerPerSecond = 10f;
    private float timer;

    protected override void Start() {
        base.Start();
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= 1f) {
            timer -= 1f;
            ResourceManager.Instance.AddEnergy(powerPerSecond);
            Debug.Log($"{gameObject.name} сгенерировал {powerPerSecond} энергии");
        }
    }

    protected override void Die() {
        base.Die();
    }
}
