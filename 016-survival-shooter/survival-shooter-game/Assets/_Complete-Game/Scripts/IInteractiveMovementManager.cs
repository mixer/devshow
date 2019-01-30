using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractiveMovementManager {
    bool GetIsBeingControlled();

    void SetIsBeingControlled(bool value);

    string ControllerSessionId { get; set; }
}
