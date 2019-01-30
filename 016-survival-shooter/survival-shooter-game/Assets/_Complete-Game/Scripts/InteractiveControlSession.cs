using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveControlSession {
    public string ParticipantSessionId;
    public GameObject ControlledObject;
    public DateTime ControlStarted;
    public float ControlDurationSeconds;

    public IEnumerator TakeControl()
    {
        ControlStarted = DateTime.Now;
        var manager = null != ControlledObject
            ? ControlledObject.GetComponent<IInteractiveMovementManager>()
            : null;
        if (null != manager)
        {
            manager.ControllerSessionId = ParticipantSessionId;
            manager.SetIsBeingControlled(true);
        }

        SetInteractiveParticipantObject(ParticipantSessionId, ControlledObject.GetInstanceID());

        yield return new WaitForSeconds(ControlDurationSeconds);

        if (null != manager)
        {
            manager.ControllerSessionId = null;
            manager.SetIsBeingControlled(false);
        }

        SetInteractiveParticipantObject(ParticipantSessionId, null);
    }

    private void SetInteractiveParticipantObject(string participantSessionId, int? instanceId)
    {
        if (MixerInteractive.InteractivityState != Microsoft.Mixer.InteractivityState.InteractivityEnabled)
        {
            return;
        }

        string updateMessage = "{" +
            "\"type\": \"method\"," +
            "\"id\": 123,  " +
            "\"method\": \"updateParticipants\"," +
            "\"params\": {      " +
            "    \"participants\": [   " +
            "    { " +
            "      \"sessionID\": \"" + participantSessionId + "\"," +
            "      \"controllingObjectId\": " + (instanceId.HasValue ? instanceId.Value.ToString() : "null") +
            "    }, " +
            "    ]" +
            "  }" +
            "}";

        MixerInteractive.SendInteractiveMessage(updateMessage);
    }
}
