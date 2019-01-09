using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    public class SonicBomb : Bomb
    {
        MovementAffector Affector;

        public SonicBomb(float radius, MovementAffector affector): base(radius)
        {
            Affector = affector;
        }

        public override void Drop(Vector3 position)
        {
            foreach (var obj in GetEnemiesInRadius<EnemyMovement>(Radius, position))
            {
                var vector = obj.gameObject.transform.position - position;
                obj.AddAffector(new MovementAffector(Affector, vector));
            }
        }
    }
}