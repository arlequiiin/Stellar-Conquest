using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {
    [Header("����� ������")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public int maxEnemies = 10;
    public float spawnInterval = 5f;
    public int minDefenders = 6;

    [Header("���������� � ��������� �����")]
    public Transform[] patrolPoints;      // ����� ��� ��������������
    public Transform[] attackDirections;  // ����� ��� ����������� ����� �� ���� ������

    [Header("������� ������ ������")]
    public Transform playerBase; // ��������� ������ ������ (������� � ����������)

    private List<EnemyUnit> allEnemies = new List<EnemyUnit>();

    void Start() {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine() {
        while (true) {
            if (allEnemies.Count < maxEnemies) {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                var obj = Instantiate(prefab, point.position, Quaternion.identity);
                var unit = obj.GetComponent<EnemyUnit>();
                if (unit != null) {
                    allEnemies.Add(unit);
                    unit.manager = this;
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void RemoveEnemy(EnemyUnit unit) {
        allEnemies.Remove(unit);
    }

    // �����, ������� ���������� ������ ��� ��������� ����� ����
    public EnemyUnit.Role GetRoleForUnit(EnemyUnit unit) {
        int defenders = 0;
        foreach (var en in allEnemies)
            if (en != null && en.CurrentRole == EnemyUnit.Role.Defender) defenders++;

        if (defenders < minDefenders)
            return EnemyUnit.Role.Defender;
        else
            return EnemyUnit.Role.Attacker;
    }

    // �������� ����� ��� �����
    public Transform GetAttackDirection() {
        return attackDirections[Random.Range(0, attackDirections.Length)];
    }

    // �������� ���������� �����
    public Transform GetPatrolPoint() {
        return patrolPoints[Random.Range(0, patrolPoints.Length)];
    }
}
