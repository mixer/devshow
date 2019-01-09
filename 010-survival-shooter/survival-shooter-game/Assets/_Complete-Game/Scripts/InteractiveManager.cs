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
        public float BlastBombRadius = 1.0f;
        public int BlastBombDamage = 50;

        public float SonicBombRadius = 1.0f;
        public MovementAffector SonicBombAffector;

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
    }
}