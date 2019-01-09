using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    [System.Serializable()]
    public class Point
    {
        [SerializeField]
        public float x;

        [SerializeField]
        public float y;

        [SerializeField]
        public bool enemy;

        [SerializeField]
        public string colorHex;

        [SerializeField]
        public float size;

        public Point(float positionX, float positionY, bool isEnemy, Color color, float pointsize)
        {
            x = positionX;
            y = positionY;
            enemy = isEnemy;
            colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
            size = pointsize;
        }
    }

    public class EnemyManager : MonoBehaviour
    {
        public PlayerHealth playerHealth;       // Reference to the player's heatlh.
        public GameObject enemy;                // The enemy prefab to be spawned.
        public float spawnTime = 3f;            // How long between each spawn.
        public Transform[] spawnPoints;         // An array of the spawn points this enemy can spawn from.
        public GameObject[] enemies;
        public GameObject player;

        private List<Point> points;
        

        private float lastSendTime = -1;
        private float sendInterval = 0.25f;

        void Start ()
        {
            // Call the Spawn function after a delay of the spawnTime and then continue to call after the same amount of time.
            InvokeRepeating ("Spawn", spawnTime, spawnTime);
        }

        void Update()
        {
            if (Time.time - lastSendTime < sendInterval)
            {
                return;
            }

            lastSendTime = Time.time;

            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            player = GameObject.FindGameObjectWithTag("Player");
            points = new List<Point>();

            // Send up points
            var point = PointFromGameObject(player);
            if (null != point)
            {
                points.Add(point);
            }

            foreach (GameObject enemy in enemies)
            {
                var enemyPoint = PointFromGameObject(enemy);
                if (null != enemyPoint)
                {
                    points.Add(enemyPoint);
                }
            }
            
            if (points.Count > 1)
            {
                string pointUpdateMessage = "{" +
                        "\"type\": \"method\"," +
                        "\"id\": 123,  " +
                        "\"method\": \"broadcastEvent\"," +
                        "\"params\": {      " +
                        "    \"scope\": [   " +
                        "    \"group:default\"" +
                        "      ],  " +
                        "      \"data\": " + JsonConvert.SerializeObject(points) +
                        "    }, " +
                        "    \"discard\": true" +
                        "  }";

                if (MixerInteractive.InteractivityState == Microsoft.Mixer.InteractivityState.InteractivityEnabled)
                {
                    MixerInteractive.SendInteractiveMessage(pointUpdateMessage);
                }
            }
        }

        private Point PointFromGameObject(GameObject obj)
        {
            var newPosition = Camera.main.WorldToViewportPoint(obj.transform.position);

            if (
                newPosition.x > Util.MinimapMinBoundary.x
                && newPosition.x < Util.MinimapMaxBoundary.x
                && newPosition.y > Util.MinimapMinBoundary.y
                && newPosition.y < Util.MinimapMaxBoundary.y
            ) {
                // Default
                float size = 1.0f;
                Color color = Color.black;
                bool isEnemy = true;

                var mmHelper = obj.GetComponent<MinimapHelper>();
                if (null != mmHelper)
                {
                    color = mmHelper.MinimapColor;
                    size = mmHelper.MinimapSize;
                    isEnemy = mmHelper.IsEnemy;
                }

                return new Point(
                    Util.ScaleValue(newPosition.x, Util.MinimapMinBoundary.x, Util.MinimapMaxBoundary.x, 0, 1),
                    Util.ScaleValue(newPosition.y, Util.MinimapMinBoundary.y, Util.MinimapMaxBoundary.y, 0, 1),
                    isEnemy,
                    color,
                    size);
            }

            return null;
        }
        

        void Spawn ()
        {
            // If the player has no health left...
            if(playerHealth.currentHealth <= 0f)
            {
                // ... exit the function.
                return;
            }

            // Find a random index between zero and one less than the number of spawn points.
            int spawnPointIndex = Random.Range (0, spawnPoints.Length);

            // Create an instance of the enemy prefab at the randomly selected spawn point's position and rotation.
            Instantiate (enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        }
    }
}