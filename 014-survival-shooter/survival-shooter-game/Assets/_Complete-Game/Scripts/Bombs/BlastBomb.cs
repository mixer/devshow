using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    public class BlastBomb : Bomb
    {
        public int Damage { get; set; }

        public BlastBomb(float radius, int blastDamage) : base(radius)
        {
            Damage = blastDamage;
        }

        public override void Drop(Vector3 position)
        {
            foreach (var obj in GetEnemiesInRadius<EnemyHealth>(Radius, position))
            {
                obj.TakeDamage(Damage, obj.transform.position);
            }
        }
    }
}