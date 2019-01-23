using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    [Serializable]
    class InteractiveBombMessage {
        public string bombType;
        public InteractiveBombLocation location;
    }

    [Serializable]
    class InteractiveBombLocation
    {
        public float x;
        public float y;
    }

    [Serializable]
    class InteractivePacket<T>
    {
        public string method;
        public InteractiveInputParams @params;
    }

    [Serializable]
    class InteractiveInputParams
    {
        public string participantID;
        public InteractiveBombMessage input;
    }

    public class InteractiveManager : MonoBehaviour
    {
        public const float UpdateMovementEpsilon = 0.01f;

        public float BlastBombRadius = 1.0f;
        public int BlastBombDamage = 50;

        public float SonicBombRadius = 1.0f;
        public MovementAffector SonicBombAffector;

        public GameObject[] enemies;
        public GameObject player;

        private Dictionary<GameObject, Point> lastUpdatedPoints = new Dictionary<GameObject, Point>();
        private List<GameObject> removeList = new List<GameObject>();


        private float lastSendTime = -1;
        private float sendInterval = 0.1f;

        // Use this for initialization
        void Start()
        {
            MixerInteractive.GoInteractive();
            MixerInteractive.OnInteractiveMessageEvent += MixerInteractive_OnInteractiveMessageEvent;
        }

        private void MixerInteractive_OnInteractiveMessageEvent(object sender, Microsoft.Mixer.InteractiveMessageEventArgs e)
        {
            if (!e.Message.Contains("giveInput"))
            {
                return;
            }

            var message = JsonUtility.FromJson<InteractivePacket<InteractiveBombMessage>>(e.Message);
            if (null == message.@params || null == message.@params.input)
            {
                return;
            }

            var location = message.@params.input.location;
            RaycastHit hit;
            var ray = Camera.main.ViewportPointToRay(new Vector3(
                Util.ScaleValue(location.x, 0, 1, Util.MinimapMinBoundary.x, Util.MinimapMaxBoundary.x),
                Util.ScaleValue(1 - location.y, 0, 1, Util.MinimapMinBoundary.y, Util.MinimapMaxBoundary.y),
                0));

            if (!Physics.Raycast(ray, out hit))
            {
                Debug.LogError("Bomb missed world");
                return;
            }

            switch (message.@params.input.bombType)
            {
                case "blast":
                    new BlastBomb(BlastBombRadius, BlastBombDamage)
                            .Drop(hit.point);
                    break;
                case "sonic":
                    new SonicBomb(SonicBombRadius, SonicBombAffector)
                        .Drop(hit.point);
                    break;
                default:
                    Debug.LogError("I'm not dropping it like it's hot: " + message.@params.input.bombType);
                    break;
            }
        }

        [ContextMenu("Drop Test Blast Bomb")]
        void DropTestBlastBomb()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (null != player)
            {
                new BlastBomb(BlastBombRadius, BlastBombDamage)
                    .Drop(player.transform.position);
            }
        }

        [ContextMenu("Drop Test Sonic Bomb")]
        void DropTestSonicBomb()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (null == player)
            {
                throw new Exception("No player in scene");
            }

            var bomb = new SonicBomb(SonicBombRadius, SonicBombAffector);
            bomb.Drop(player.transform.position);
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

            var updateList = new Dictionary<int, Point>();

            // Send up points
            var point = PointFromGameObject(player);
            if (null != point)
            {
                updateList.Add(player.GetInstanceID(), point);
            }

            foreach (GameObject enemy in enemies)
            {
                var enemyPoint = PointFromGameObject(enemy);
                var oldPoint = lastUpdatedPoints.ContainsKey(enemy)
                    ? lastUpdatedPoints[enemy]
                    : null;
                if (null != enemyPoint && (null == oldPoint || IsPointUpdated(enemyPoint, oldPoint)))
                {
                    updateList.Add(enemy.GetInstanceID(), enemyPoint);
                    TrackEnemyPoint(enemyPoint, enemy);
                }
            }

            foreach (var enemy in removeList)
            {
                updateList.Add(enemy.GetInstanceID(), null);
            }

            removeList.Clear();

            if (updateList.Count > 1)
            {
                string pointUpdateMessage = "{" +
                        "\"type\": \"method\"," +
                        "\"id\": 123,  " +
                        "\"method\": \"broadcastEvent\"," +
                        "\"params\": {      " +
                        "    \"scope\": [   " +
                        "    \"group:default\"" +
                        "      ],  " +
                        "      \"data\": " + JsonConvert.SerializeObject(updateList) +
                        "    }, " +
                        "    \"discard\": true" +
                        "  }";

                if (MixerInteractive.InteractivityState == Microsoft.Mixer.InteractivityState.InteractivityEnabled)
                {
                    MixerInteractive.SendInteractiveMessage(pointUpdateMessage);
                }
            }
        }

        private bool IsPointUpdated(Point newPoint, Point oldPoint)
        {
            return newPoint.enemy != oldPoint.enemy
                || newPoint.colorHex != oldPoint.colorHex
                || newPoint.size != oldPoint.size
                || Mathf.Abs(newPoint.x - oldPoint.x) > UpdateMovementEpsilon
                || Mathf.Abs(newPoint.y - oldPoint.y) > UpdateMovementEpsilon;
        }

        private void TrackEnemyPoint(Point point, GameObject enemy)
        {
            if (!lastUpdatedPoints.ContainsKey(enemy))
            {
                var healthManager = enemy.GetComponent<EnemyHealth>();
                if (null == healthManager)
                {
                    // Can't track
                    return;
                }

                healthManager.EnemyDestroyed += HealthManager_EnemyDestroyed;
            }

            lastUpdatedPoints[enemy] = point;
        }

        private void HealthManager_EnemyDestroyed(GameObject sender)
        {
            removeList.Add(sender);
        }

        private Point PointFromGameObject(GameObject obj)
        {
            var newPosition = Camera.main.WorldToViewportPoint(obj.transform.position);

            if (
                newPosition.x > Util.MinimapMinBoundary.x
                && newPosition.x < Util.MinimapMaxBoundary.x
                && newPosition.y > Util.MinimapMinBoundary.y
                && newPosition.y < Util.MinimapMaxBoundary.y
            )
            {
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
    }
}