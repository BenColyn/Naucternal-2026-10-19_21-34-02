using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMeleeCombat : MonoBehaviour
{
    public Transform attackOrigin;
    public LayerMask enemyLayer;

    [Header("Combat Stats")]
    public int attackDamage = 10;
    public float attackSpeed = 1.0f;
    public float attackRange = 0.5f;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
                
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmos()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
