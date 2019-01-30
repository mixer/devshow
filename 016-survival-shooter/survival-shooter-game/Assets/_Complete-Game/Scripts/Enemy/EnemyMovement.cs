using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CompleteProject
{
    public class EnemyMovement : MonoBehaviour, IInteractiveMovementManager
    {
        Transform player;               // Reference to the player's position.
        PlayerHealth playerHealth;      // Reference to the player's health.
        EnemyHealth enemyHealth;        // Reference to this enemy's health.
        UnityEngine.AI.NavMeshAgent nav;               // Reference to the nav mesh agent.
        private List<MovementAffector> movementAffectors = new List<MovementAffector>();

        public string ControllerSessionId { get; set; }

        private bool isBeingControlled;
        private Vector3 interactiveVector;

        public void AddAffector(MovementAffector affector)
        {
            movementAffectors.Add(affector);
        }

        public bool GetIsBeingControlled()
        {
            return isBeingControlled;
        }

        public void SetIsBeingControlled(bool value)
        {
            if (value != isBeingControlled)
            {
                if (value)
                {
                    interactiveVector = Vector3.zero;
                    MixerInteractive.OnInteractiveButtonEvent += MixerInteractive_OnInteractiveButtonEvent;
                }
                else
                {
                    MixerInteractive.OnInteractiveButtonEvent -= MixerInteractive_OnInteractiveButtonEvent;
                }

                isBeingControlled = value;
            }
        }

        private void MixerInteractive_OnInteractiveButtonEvent(object sender, Microsoft.Mixer.InteractiveButtonEventArgs e)
        {
            if (e.Participant._sessionID != ControllerSessionId)
            {
                return;
            }

            switch (e.ControlID)
            {
                case InteractiveManager.BUTTON_MOVE_FORWARD:
                    interactiveVector.z += EventToValue(e.IsPressed);
                    break;

                case InteractiveManager.BUTTON_MOVE_BACKWARD:
                    interactiveVector.z -= EventToValue(e.IsPressed);
                    break;

                case InteractiveManager.BUTTON_MOVE_RIGHT:
                    interactiveVector.x += EventToValue(e.IsPressed);
                    break;

                case InteractiveManager.BUTTON_MOVE_LEFT:
                    interactiveVector.x -= EventToValue(e.IsPressed);
                    break;
            }
        }

        private float EventToValue(bool e)
        {
            if (e)
            {
                return nav.speed;
            }

            return -nav.speed;
        }

        void Awake ()
        {
            // Set up the references.
            player = GameObject.FindGameObjectWithTag ("Player").transform;
            playerHealth = player.GetComponent <PlayerHealth> ();
            enemyHealth = GetComponent <EnemyHealth> ();
            nav = GetComponent <UnityEngine.AI.NavMeshAgent> ();
        }


        void Update ()
        {
            // If the enemy and the player have health left...
            if(enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0)
            {
                // ... set the destination of the nav mesh agent to the player.
                if (GetIsBeingControlled())
                {
                    nav.SetDestination(gameObject.transform.position + interactiveVector);
                }
                else
                {
                    nav.SetDestination(player.position);
                }
            }
            // Otherwise...
            else
            {
                // ... disable the nav mesh agent.
                nav.enabled = false;
            }

            if (movementAffectors.Count > 0)
            {
                var completedAffectors = new List<MovementAffector>();
                foreach (var affector in movementAffectors)
                {
                    if (!affector.Apply(this.gameObject))
                    {
                        completedAffectors.Add(affector);
                    }
                }

                foreach (var completed in completedAffectors)
                {
                    movementAffectors.Remove(completed);
                }
            }
        }
    }
}