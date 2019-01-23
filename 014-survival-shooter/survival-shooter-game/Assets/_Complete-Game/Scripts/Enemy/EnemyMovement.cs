using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CompleteProject
{
    public class EnemyMovement : MonoBehaviour
    {
        Transform player;               // Reference to the player's position.
        PlayerHealth playerHealth;      // Reference to the player's health.
        EnemyHealth enemyHealth;        // Reference to this enemy's health.
        UnityEngine.AI.NavMeshAgent nav;               // Reference to the nav mesh agent.
        private List<MovementAffector> movementAffectors = new List<MovementAffector>();

        public void AddAffector(MovementAffector affector)
        {
            movementAffectors.Add(affector);
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
                nav.SetDestination (player.position);
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