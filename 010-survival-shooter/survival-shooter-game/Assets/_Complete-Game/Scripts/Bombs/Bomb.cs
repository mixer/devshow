using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    public abstract class Bomb
    {
        public float Radius { get; set; }

        public Bomb(float radius)
        {
            Radius = radius;
        }

        protected IEnumerable<T> GetEnemiesInRadius<T>(float radius, Vector3 position) where T : MonoBehaviour
        {
            foreach (var collider in Physics.OverlapSphere(position, radius))
            {
                var asT = collider.gameObject.GetComponentInParent<T>();
                if (null != asT)
                {
                    yield return asT;
                }
            }
        }

        public abstract void Drop(Vector3 position);
    }
}