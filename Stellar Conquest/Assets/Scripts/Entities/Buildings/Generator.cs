using UnityEngine;

public class Generator : Buildings {
    [SerializeField] private float powerPerSecond = 10f;
    private float timer;

    protected override void Start() {
        base.Start();
        Debug.Log($"{gameObject.name} начал производить энергию");
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
        Debug.Log($"{gameObject.name} разрушен и больше не производит энергию");
        base.Die();
    }
}
