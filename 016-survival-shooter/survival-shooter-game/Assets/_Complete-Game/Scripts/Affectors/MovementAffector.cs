using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    [Serializable]
    public class MovementAffector
    {
        public float AffectorDuration = 1.0f;
        public float AffectDistance = 3.0f;
        public AnimationCurve AffectorCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private float current = 0;
        private Vector3 AffectVector;

        public MovementAffector(Vector3 affectVector)
        {
            AffectVector = affectVector.normalized;
        }

        public MovementAffector(MovementAffector Other, Vector3 affectVector) : this(affectVector)
        {
            AffectorCurve = Other.AffectorCurve;
            AffectorDuration = Other.AffectorDuration;
            AffectDistance = Other.AffectDistance;
        }

        public bool Apply(GameObject recipient)
        {
            if (0 == current)
            {
                AffectVector *= AffectDistance;
            }

            current += Time.deltaTime;
            if (current >= AffectorDuration)
            {
                return false;
            }

            var delta = AffectVector * AffectorCurve.Evaluate(current / AffectorDuration);
            recipient.transform.position += delta;
            return true;
        }
    }
}