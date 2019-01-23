﻿/*
 * Mixer Unity SDK
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using UnityEngine;
using Microsoft.Mixer;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Reflection;
using Microsoft;
#if UNITY_WSA && !UNITY_EDITOR
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System;
using Windows.Security.Credentials;
using Windows.Security.Authentication.Web.Core;
using System.Threading.Tasks;
#endif
#if UNITY_XBOXONE && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections;
#endif

public class MixerInteractive : MonoBehaviour
{
    public bool runInBackground = true;
    public string defaultSceneID;

    // Custom Unity Inspectors are not great at displaying complex objects
    // so we'll store these as seperate variables instead of a List.
    public List<string> groupIDs;
    public List<string> sceneIDs;

    // Events
    public delegate void OnErrorEventHandler(object sender, InteractiveEventArgs e);
    public static event OnErrorEventHandler OnError;

    public delegate void OnGoInteractiveHandler(object sender, InteractiveEventArgs e);
    public static event OnGoInteractiveHandler OnGoInteractive;

    public delegate void OnInteractivityStateChangedHandler(object sender, InteractivityStateChangedEventArgs e);
    public static event OnInteractivityStateChangedHandler OnInteractivityStateChanged;

    public delegate void OnParticipantStateChangedHandler(object sender, InteractiveParticipantStateChangedEventArgs e);
    public static event OnParticipantStateChangedHandler OnParticipantStateChanged;

    public delegate void OnInteractiveButtonEventHandler(object sender, InteractiveButtonEventArgs e);
    public static event OnInteractiveButtonEventHandler OnInteractiveButtonEvent;

    public delegate void OnInteractiveJoystickControlEventHandler(object sender, InteractiveJoystickEventArgs e);
    public static event OnInteractiveJoystickControlEventHandler OnInteractiveJoystickControlEvent;

    public delegate void OnInteractiveMouseButtonEventHandler(object sender, InteractiveMouseButtonEventArgs e);
    public static event OnInteractiveMouseButtonEventHandler OnInteractiveMouseButtonEvent;

    public delegate void OnInteractiveCoordinatesChangedHandler(object sender, InteractiveCoordinatesChangedEventArgs e);
    public static event OnInteractiveCoordinatesChangedHandler OnInteractiveCoordinatesChangedEvent;

    public delegate void OnInteractiveTextControlEventHandler(object sender, InteractiveTextEventArgs e);
    public static event OnInteractiveTextControlEventHandler OnInteractiveTextControlEvent;

    public delegate void OnInteractiveMessageEventHandler(object sender, InteractiveMessageEventArgs e);
    public static event OnInteractiveMessageEventHandler OnInteractiveMessageEvent;

    private static InteractivityManager interactivityManager;
    private static List<InteractiveEventArgs> queuedEvents;
    private static bool previousRunInBackgroundValue;
    private static MixerInteractiveDialog mixerDialog;
    private static bool pendingGoInteractive;
    private static string outstandingSetDefaultSceneRequest;
    private static List<string> outstandingCreateGroupsRequests;
    private static bool outstandingRequestsCompleted;
    private static float lastCheckForOutstandingRequestsTime;
    private static bool processedSerializedProperties;
    private static bool hasFiredGoInteractiveEvent;
    private static bool shouldCheckForOutstandingRequests;

    // Custom controls
    public GameObject addNewRpcMethodSource;
    // Unity doesn't handle serialization of dictionaries easily
    // so we keep 2 lists of strings instead.
    public List<string> rpcOwningMonoBehaviorNames;
    public List<string> rpcMethodNames;

    private static List<string> outboundMessages;
    internal static Websocket _websocket;

#if !UNITY_WSA || UNITY_EDITOR
    private static BackgroundWorker backgroundWorker;
#endif

    private const string DEFAULT_GROUP_ID = "default";
    private const float CHECK_FOR_OUTSTANDING_REQUESTS_INTERVAL = 1f;
    internal const float _DEFAULT_MIXER_SYNCVAR_UPDATE_INTERVAL = 1f;

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
        // Make sure the MixerInteractiveHelper is initialized. Old game projects won't have the MixerInteractiveHelper
        // on the InteractivityManager prefab. Since we don't want to break them, we add it under the covers.
        gameObject.AddComponent<MixerInteractiveHelper>();
    }

    // Use this for initialization
    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (mixerDialog == null)
        {
            mixerDialog = FindObjectOfType<MixerInteractiveDialog>();
        }
        if (queuedEvents == null)
        {
            queuedEvents = new List<InteractiveEventArgs>();
        }
        // Listen for interactive events
        bool interactivityManagerAlreadyInitialized = false;
        if (interactivityManager == null)
        {
            interactivityManager = InteractivityManager.SingletonInstance;

            interactivityManager.OnError -= HandleError;
            interactivityManager.OnInteractivityStateChanged -= HandleInteractivityStateChanged;
            interactivityManager.OnParticipantStateChanged -= HandleParticipantStateChanged;
            interactivityManager.OnInteractiveButtonEvent -= HandleInteractiveButtonEvent;
            interactivityManager.OnInteractiveJoystickControlEvent -= HandleInteractiveJoystickControlEvent;
            interactivityManager.OnInteractiveMouseButtonEvent -= HandleInteractiveMouseButtonEvent;
            interactivityManager.OnInteractiveCoordinatesChangedEvent -= HandleInteractiveCoordinatesChangedHandler;
            interactivityManager.OnInteractiveTextControlEvent -= HandleInteractiveTextControlEvent;
            interactivityManager.OnInteractiveMessageEvent -= HandleInteractiveMessageEvent;

            interactivityManager.OnError += HandleError;
            interactivityManager.OnInteractivityStateChanged += HandleInteractivityStateChanged;
            interactivityManager.OnParticipantStateChanged += HandleParticipantStateChanged;
            interactivityManager.OnInteractiveButtonEvent += HandleInteractiveButtonEvent;
            interactivityManager.OnInteractiveMouseButtonEvent += HandleInteractiveMouseButtonEvent;
            interactivityManager.OnInteractiveCoordinatesChangedEvent += HandleInteractiveCoordinatesChangedHandler;
            interactivityManager.OnInteractiveJoystickControlEvent += HandleInteractiveJoystickControlEvent;
            interactivityManager.OnInteractiveTextControlEvent += HandleInteractiveTextControlEvent; 
            interactivityManager.OnInteractiveMessageEvent += HandleInteractiveMessageEvent;
        }
        else
        {
            interactivityManagerAlreadyInitialized = true;
        }
        MixerInteractiveHelper helper = MixerInteractiveHelper._SingletonInstance;
        helper._runInBackgroundIfInteractive = runInBackground;
        helper._defaultSceneID = defaultSceneID;
        for (int i = 0; i < groupIDs.Count; i++)
        {
            string groupID = groupIDs[i];
            if (groupID != string.Empty &&
                !helper._groupSceneMapping.ContainsKey(groupID))
            {
                helper._groupSceneMapping.Add(groupID, sceneIDs[i]);
            }
        }

        if (outstandingCreateGroupsRequests == null)
        {
            outstandingCreateGroupsRequests = new List<string>();
        }
        outstandingSetDefaultSceneRequest = string.Empty;
        processedSerializedProperties = false;
        outstandingRequestsCompleted = false;
        shouldCheckForOutstandingRequests = false;
        lastCheckForOutstandingRequestsTime = -1;
        outboundMessages = new List<string>();
#if !UNITY_WSA
        backgroundWorker = new BackgroundWorker();
#endif
        if (interactivityManagerAlreadyInitialized &&
            InteractivityManager.SingletonInstance.InteractivityState == InteractivityState.InteractivityEnabled)
        {
            ProcessSerializedProperties();
        }
        _websocket = gameObject.AddComponent<Websocket>();
        InteractivityManager.SingletonInstance.SetWebsocketInstance(_websocket);
    }

    private static void HandleInteractiveJoystickControlEvent(object sender, InteractiveJoystickEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleInteractiveMouseButtonEvent(object sender, InteractiveMouseButtonEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleInteractiveCoordinatesChangedHandler(object sender, InteractiveCoordinatesChangedEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleInteractiveButtonEvent(object sender, InteractiveButtonEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private void HandleInteractiveTextControlEvent(object sender, InteractiveTextEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleParticipantStateChanged(object sender, InteractiveEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleInteractivityStateChanged(object sender, InteractivityStateChangedEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleError(object sender, InteractiveEventArgs e)
    {
        queuedEvents.Add(e);
    }

    private static void HandleInteractiveMessageEvent(object sender, InteractiveEventArgs e)
    {
        queuedEvents.Add(e);
    }

    // Called when a remote RPC messages is sent
    internal static void InvokeRpcMethod(string methodName, List<MixerHelperParameterInfo> mixerParameterInfos)
    {
        // See if we can find the method. If not, refresh the method cache and try again.
        bool methodFound = FindAndInvokeRpcMethod(methodName, mixerParameterInfos);
        if (!methodFound)
        {
            RefreshRPCMethods();
            FindAndInvokeRpcMethod(methodName, mixerParameterInfos);
        }
    }

    private static bool FindAndInvokeRpcMethod(string methodName, List<MixerHelperParameterInfo> mixerParameterInfos)
    {
        bool found = false;
        RpcCachedMethodInfo rpcCachedMethodInfo = new RpcCachedMethodInfo();
        var helper = MixerInteractiveHelper._SingletonInstance;
        if (helper.cachedRPCMethods.TryGetValue(methodName, out rpcCachedMethodInfo))
        {
            // Invoke the function
            MethodInfo methodInfo = rpcCachedMethodInfo.methodInfo;
            ParameterInfo[] parametersInfo = methodInfo.GetParameters();
            object[] methodParameters = new object[parametersInfo.Length];
            for (int i = 0; i < parametersInfo.Length; i++)
            {
                ParameterInfo parameterInfo = parametersInfo[i];
                string parameterAsString = mixerParameterInfos[i].typeValue;
                methodParameters[i] = Convert.ChangeType(parameterAsString, parameterInfo.ParameterType);
            }

            found = true;
            try
            {
                methodInfo.Invoke((object)rpcCachedMethodInfo.owningMonoBehavior, methodParameters);
            }
            catch (Exception e)
            {
                Debug.Log("Error calling method " + rpcCachedMethodInfo.owningMonoBehavior.name + "." + methodInfo.Name + ". Details: " + e.Message);
            }
        }
        return found;
    }

    public static void AddRpcMethodsFromTheEditor(GameObject owningGameObject, List<string> rpcMethodNames)
    {
        if (rpcMethodNames.Count == 0)
        {
            return;
        }

        var helper = MixerInteractiveHelper._SingletonInstance;
        MonoBehaviour[] activeBehaviors = owningGameObject.GetComponents<MonoBehaviour>();
        foreach (string rpcMethodName in rpcMethodNames)
        {
            string trimmedMethodName = TrimMethodName(rpcMethodName);
            foreach (MonoBehaviour monoBehavior in activeBehaviors)
            {
                Type monoType = monoBehavior.GetType();
                MethodInfo[] methods = monoType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name == trimmedMethodName)
                    {
                        if (!helper.cachedRPCMethods.ContainsKey(method.Name))
                        {
                            helper.cachedRPCMethods.Add(method.Name, new RpcCachedMethodInfo
                            {
                                owningMonoBehavior = monoBehavior,
                                methodInfo = method
                            });
                        }
                    }
                }
            }
        }
    }

    internal static string TrimMethodName(string unTrimmedMethodName)
    {
        return unTrimmedMethodName.Split('(')[0].Trim();
    }

    private static void RefreshRPCMethods(bool includeMethodsFromTheInspector = false)
    {
        // Add all methods with the "MixerRpcMethod" to our method cache.
        var helper = MixerInteractiveHelper._SingletonInstance;
        MonoBehaviour[] activeBehaviors = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour monoBehavior in activeBehaviors)
        {
            Type monoType = monoBehavior.GetType();
            MethodInfo[] methods = monoType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(MixerRpcMethod), false))
                {
                    if (!helper.cachedRPCMethods.ContainsKey(method.Name))
                    {
                        helper.cachedRPCMethods.Add(method.Name, new RpcCachedMethodInfo
                        {
                            owningMonoBehavior = monoBehavior,
                            methodInfo = method
                        });
                    }
                }

                // Check for inspector methods
                if (includeMethodsFromTheInspector)
                {
                    string currentMonoBehaviorName = monoBehavior.name;
                    for (int i = 0; i < helper.rpcOwningMonoBehaviorNames.Count; i++)
                    {
                        if (helper.rpcOwningMonoBehaviorNames[i] == currentMonoBehaviorName &&
                            helper.rpcMethodNames[i] == method.Name)
                        {
                            if (!helper.cachedRPCMethods.ContainsKey(method.Name))
                            {
                                helper.cachedRPCMethods.Add(method.Name, new RpcCachedMethodInfo
                                {
                                    owningMonoBehavior = monoBehavior,
                                    methodInfo = method
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    public static void FlushUpdates()
    {
        // Send all the outbound messages regardless of time.
        SendOutboundMessages();
    }

    internal static void SendOutboundMessages()
    {
        foreach (string message in outboundMessages)
        {
            InteractivityManager.SingletonInstance.SendMessage(message);
        }
    }

    private static void SerializeSyncVars()
    {
#if !UNITY_WSA
        // Get all syncvars
        MonoBehaviour[] activeBehaviors = GameObject.FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour monoBehavior in activeBehaviors)
        {
            Type monoType = monoBehavior.GetType();

            // Get the fields
            FieldInfo[] behaviorFields = monoType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in behaviorFields)
            {
                MixerSyncVar attribute = Attribute.GetCustomAttribute(field, typeof(MixerSyncVar)) as MixerSyncVar;
                if (attribute != null)
                {
                    object valueAsObject = field.GetValue(monoBehavior);
                    string valueAsString = string.Empty;
                    if (valueAsObject != null)
                    {
                        valueAsString = valueAsObject.ToString();
                    }
                    ParseAndSendCustomMessage(field.Name, valueAsString);
                }
            }
        }
#endif
    }

    private static void ParseAndSendCustomMessage(string name, string value)
    {
        InteractivityManager.SingletonInstance.SendMessage("{" +
            "   name: " + name +
            "   value: " + value +
            "}");
    }

    internal static void QueueCustomMessage(string newMessage)
    {
        if (outboundMessages == null)
        {
            outboundMessages = new List<string>();
        }
        outboundMessages.Add(newMessage);
    }

    internal static int GetTimeSinceStartUpInMilliSeconds()
    {
        return (int)Time.realtimeSinceStartup * 1000;
    }

    /// <summary>
    /// Gets and sets the token used for authentication with Mixer. 
    /// You can set the token manually that the SDK will use. 
    /// Currently, this is only applicable for Xbox One.
    /// </summary>
    public static string Token
    {
        get
        {
            return InteractivityManager.SingletonInstance._authToken;
        }
        set
        {
            InteractivityManager.SingletonInstance._authToken = value;
        }
    }

    /// <summary>
    /// Can query the state of the InteractivityManager.
    /// </summary>
    public static InteractivityState InteractivityState
    {
        get
        {
            return InteractivityManager.SingletonInstance.InteractivityState;
        }
    }

    /// <summary>
    /// Gets all the groups associated with the current interactivity instance.
    /// Will be empty if initialization is not complete.
    /// </summary>
    public static IList<InteractiveGroup> Groups
    {
        get
        {
            return InteractivityManager.SingletonInstance.Groups;
        }
    }

    /// <summary>
    /// Gets all the scenes associated with the current interactivity instance.
    /// </summary>
    public static IList<InteractiveScene> Scenes
    {
        get
        {
            return InteractivityManager.SingletonInstance.Scenes;
        }
    }

    /// <summary>
    /// Returns all the participants.
    /// </summary>
    public static IList<InteractiveParticipant> Participants
    {
        get
        {
            return InteractivityManager.SingletonInstance.Participants;
        }
    }

    /// <summary>
    /// Retrieve a list of all of the button controls.
    /// </summary>
    public static IList<InteractiveButtonControl> Buttons
    {
        get
        {
            return InteractivityManager.SingletonInstance.Buttons;
        }
    }

    /// <summary>
    /// Retrieve a list of all of the joystick controls.
    /// </summary>
    public static IList<InteractiveJoystickControl> Joysticks
    {
        get
        {
            return InteractivityManager.SingletonInstance.Joysticks;
        }
    }

    /// <summary>
    /// By default the Unity SDK will automatically handle deducting sparks from users
    /// when the GetButton* APIs are called and evaluate to true. If you would like to
    /// manually handle spark transactions, you can set this value to false.
    /// </summary>
    public static bool ManuallyHandleSparkTransactions
    {
        get;
        set;
    }

    /// <summary>
    /// Returns the current mouse position in Unity's screen space.
    /// </summary>
    public static Vector3 MousePosition
    {
        get
        {
            Vector3 mousePosition = Vector3.zero;
            if (InteractivityManager._mousePositionsByParticipant.Count > 0)
            {
                Dictionary<uint, Vector2> mousePositionsByParticipant = InteractivityManager._mousePositionsByParticipant;
                var mousePositionByParticipantKeys = mousePositionsByParticipant.Keys;
                float totalX = 0;
                float totalY = 0;
                foreach (var mousePositionByParticipantKey in mousePositionByParticipantKeys)
                {
                    totalX += mousePositionsByParticipant[mousePositionByParticipantKey].x;
                    totalY += mousePositionsByParticipant[mousePositionByParticipantKey].y;
                }
                // We average all the mouse positions. Z in always zero.
                mousePosition.x = totalX / mousePositionByParticipantKeys.Count;
                mousePosition.y = totalY / mousePositionByParticipantKeys.Count;
            }

            return mousePosition;
        }
    }

    /// <summary>
    /// The string the broadcaster needs to enter in the Mixer website to
    /// authorize the interactive session.
    /// </summary>
    public static string ShortCode
    {
        get
        {
            return InteractivityManager.SingletonInstance.ShortCode;
        }
    }

    /// <summary>
    /// This method will give you the participant who gave input on the control. If there
    /// was more than one user who gave input, it will return the first one.
    /// </summary>
    /// <returns>Returns the participant who gave input. Returns null if no participant gave input.</returns>
    /// <param name="controlID">The ID of the control.</param>
    /// <remarks></remarks>
    public static InteractiveParticipant GetParticipantWhoGaveInputForControl(string controlID)
    {
        // Find which participant send the input for the given control.
        InteractiveParticipant participant = null;
        _InternalParticipantTrackingState participantTrackingState = new _InternalParticipantTrackingState();
        bool participantTrackingStateEntryExists = InteractivityManager._participantsWhoTriggeredGiveInput.TryGetValue(controlID, out participantTrackingState);
        if (participantTrackingStateEntryExists)
        {
            participant = participantTrackingState.particpant;
        }
        return participant;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="controlID">The ID of the control to check for submissions.</param>
    /// <returns></returns>
    public static bool HasSubmissions(string controlID)
    {
        bool hasSubmissions = false;
        // Get the control, check if the submit state is true.
        InteractiveTextControl textControl = GetControl(controlID) as InteractiveTextControl;
        if (textControl != null &&
            GetText(controlID).Count > 0)
        {
            hasSubmissions = true;
        }

        if (hasSubmissions)
        {
            CaptureTransactionForControlID(controlID);
        }

        return hasSubmissions;
    }

    /// <summary>
    /// Kicks off a background task to set up the connection to the interactivity service.
    /// </summary>
    /// <returns>true if initialization request was accepted, false if not</returns>
    /// <param name="goInteractive"> If true, initializes and enters interactivity. Defaults to true</param>
    /// <remarks></remarks>
    public static void Initialize(bool goInteractive = true)
    {
        InteractivityManager.SingletonInstance.Initialize(goInteractive);
    }

    /// <summary>
    /// Trigger a cooldown, disabling the specified control for a period of time.
    /// </summary>
    /// <param name="controlID">String ID of the control to disable.</param>
    /// <param name="cooldown">Duration (in milliseconds) required between triggers.</param>
    public static void TriggerCooldown(string controlID, int cooldown)
    {
        InteractivityManager.SingletonInstance.TriggerCooldown(controlID, cooldown);
    }

    /// <summary>
    /// Used by the title to inform the interactivity service that it is ready to recieve interactive input.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public static void StartInteractive()
    {
        InteractivityManager.SingletonInstance.StartInteractive();
    }

    /// <summary>
    /// Used by the title to inform the interactivity service that it is no longer receiving interactive input.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public static void StopInteractive()
    {
        InteractivityManager.SingletonInstance.StopInteractive();
        pendingGoInteractive = false;
        if (MixerInteractiveHelper._SingletonInstance._runInBackgroundIfInteractive)
        {
            Application.runInBackground = previousRunInBackgroundValue;
        }
    }

    /// <summary>
    /// Manages and maintains proper state updates between the title and the interactivity service.
    /// To ensure best performance, DoWork() must be called frequently, such as once per frame.
    /// Title needs to be thread safe when calling DoWork() since this is when states are changed.
    /// </summary>
    public static void DoWork()
    {
        InteractivityManager.SingletonInstance.DoWork();

        // Send any outstanding custom controls messages that are queued.
        SendOutboundMessages();
    }

    /// <summary>
    /// Frees resources used by the InteractivityManager.
    /// </summary>
    public static void Dispose()
    {
        InteractivityManager interactivityManager = InteractivityManager.SingletonInstance;
        if (interactivityManager != null)
        {
            interactivityManager.OnInteractivityStateChanged -= HandleInteractivityStateChangedInternal;

#if !UNITY_WSA && !UNITY_EDITOR
            // Run initialization in another thread.
            backgroundWorker.DoWork -= BackgroundWorkerDoWork;
#endif
        }
        if (queuedEvents != null)
        {
            queuedEvents.Clear();
        }
        previousRunInBackgroundValue = true;
        pendingGoInteractive = false;
        outstandingSetDefaultSceneRequest = string.Empty;
        if (outstandingCreateGroupsRequests != null)
        {
            outstandingCreateGroupsRequests.Clear();
        }
        outstandingRequestsCompleted = false;
        lastCheckForOutstandingRequestsTime = -1;
        processedSerializedProperties = false;
        hasFiredGoInteractiveEvent = false;
        interactivityManager.Dispose();
    }

    private void ResetInternalState()
    {
        previousRunInBackgroundValue = true;
        outstandingSetDefaultSceneRequest = string.Empty;
        if (outstandingCreateGroupsRequests != null)
        {
            outstandingCreateGroupsRequests.Clear();
        }
        outstandingRequestsCompleted = false;
        lastCheckForOutstandingRequestsTime = -1;
        processedSerializedProperties = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (processedSerializedProperties &&
            shouldCheckForOutstandingRequests &&
            !outstandingRequestsCompleted &&
            Time.time - lastCheckForOutstandingRequestsTime > CHECK_FOR_OUTSTANDING_REQUESTS_INTERVAL)
        {
            lastCheckForOutstandingRequestsTime = Time.time;
            outstandingRequestsCompleted = CheckForOutStandingRequestsCompleted();
        }

        DoWork();

        List<InteractiveEventArgs> processedEvents = new List<InteractiveEventArgs>();
        if (queuedEvents != null)
        {
            // Raise events
            foreach (InteractiveEventArgs interactiveEvent in queuedEvents)
            {
                if (interactiveEvent == null)
                {
                    continue;
                }
                switch (interactiveEvent.EventType)
                {
                    case InteractiveEventType.InteractivityStateChanged:
                        InteractivityStateChangedEventArgs interactivityStateChangedArgs = interactiveEvent as InteractivityStateChangedEventArgs;
                        if (interactivityStateChangedArgs.State == InteractivityState.InteractivityEnabled &&
                            (!shouldCheckForOutstandingRequests || outstandingRequestsCompleted) &&
                            !hasFiredGoInteractiveEvent)
                        {
                            if (OnGoInteractive != null)
                            {
                                hasFiredGoInteractiveEvent = true;
                                OnGoInteractive(this, interactivityStateChangedArgs);
                            }
                        }
                        if (OnInteractivityStateChanged != null)
                        {
                            OnInteractivityStateChanged(this, interactivityStateChangedArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.ParticipantStateChanged:
                        if (outstandingRequestsCompleted)
                        {
                            if (OnParticipantStateChanged != null)
                            {
                                OnParticipantStateChanged(this, interactiveEvent as InteractiveParticipantStateChangedEventArgs);
                            }
                            processedEvents.Add(interactiveEvent);
                        }
                        break;
                    case InteractiveEventType.Button:
                        if (OnInteractiveButtonEvent != null)
                        {
                            OnInteractiveButtonEvent(this, interactiveEvent as InteractiveButtonEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.Joystick:
                        if (OnInteractiveJoystickControlEvent != null)
                        {
                            OnInteractiveJoystickControlEvent(this, interactiveEvent as InteractiveJoystickEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.MouseButton:
                        if (OnInteractiveMouseButtonEvent != null)
                        {
                            OnInteractiveMouseButtonEvent(this, interactiveEvent as InteractiveMouseButtonEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.Coordinates:
                        if (OnInteractiveCoordinatesChangedEvent != null)
                        {
                            OnInteractiveCoordinatesChangedEvent(this, interactiveEvent as InteractiveCoordinatesChangedEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.TextInput:
                        if (OnInteractiveTextControlEvent != null)
                        {
                            OnInteractiveTextControlEvent(this, interactiveEvent as InteractiveTextEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    case InteractiveEventType.Error:
                        if (OnError != null)
                        {
                            OnError(this, interactiveEvent as InteractiveEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                    default:
                        if (OnInteractiveMessageEvent != null)
                        {
                            OnInteractiveMessageEvent(this, interactiveEvent as InteractiveMessageEventArgs);
                        }
                        processedEvents.Add(interactiveEvent);
                        break;
                }
            }
            foreach (InteractiveEventArgs eventArgs in processedEvents)
            {
                queuedEvents.Remove(eventArgs);
            }
        }
        if (InteractivityManager.SingletonInstance.InteractivityState == InteractivityState.InteractivityEnabled &&
            shouldCheckForOutstandingRequests &&
            outstandingRequestsCompleted &&
            !hasFiredGoInteractiveEvent)
        {
            if (OnGoInteractive != null)
            {
                hasFiredGoInteractiveEvent = true;
                OnGoInteractive(this, new InteractiveEventArgs());
            }
        }
    }

    /// <summary>
    /// Returns whether the button with the given control ID is currently down.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static bool GetButtonDown(string controlID)
    {
        bool buttonDown = InteractivityManager.SingletonInstance.GetButton(controlID).ButtonDown;
        if (buttonDown &&
            !ManuallyHandleSparkTransactions)
        {
            CaptureTransactionForButtonControlID(controlID);
        }
        return buttonDown;
    }

    /// <summary>
    /// Returns whether the button with the given control ID is currently pressed.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static bool GetButton(string controlID)
    {
        bool buttonPressed = InteractivityManager.SingletonInstance.GetButton(controlID).ButtonPressed;
        if (buttonPressed &&
            !ManuallyHandleSparkTransactions)
        {
            CaptureTransactionForButtonControlID(controlID);
        }
        return buttonPressed;
    }

    /// <summary>
    /// Returns whether the button with the given control ID is currently up.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static bool GetButtonUp(string controlID)
    {
        bool buttonUp = InteractivityManager.SingletonInstance.GetButton(controlID).ButtonUp;
        if (buttonUp &&
            !ManuallyHandleSparkTransactions)
        {
            CaptureTransactionForButtonControlID(controlID);
        }
        return buttonUp;
    }

    /// <summary>
    /// Returns how many buttons with the given control ID are pressed down.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static uint GetCountOfButtonDowns(string controlID)
    {
        return InteractivityManager.SingletonInstance.GetButton(controlID).CountOfButtonDowns;
    }

    /// <summary>
    /// Returns how many buttons with the given control ID are pressed.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static uint GetCountOfButtons(string controlID)
    {
        return InteractivityManager.SingletonInstance.GetButton(controlID).CountOfButtonPresses;
    }

    /// <summary>
    /// Returns how many buttons with the given control ID are up.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static uint GetCountOfButtonUps(string controlID)
    {
        return InteractivityManager.SingletonInstance.GetButton(controlID).CountOfButtonUps;
    }

    /// <summary>
    /// Returns the joystick with the given control ID.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static InteractiveJoystickControl GetJoystick(string controlID)
    {
        return InteractivityManager.SingletonInstance.GetJoystick(controlID);
    }

    /// <summary>
    /// Returns the X coordinate of the joystick with the given control ID.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static float GetJoystickX(string controlID)
    {
        return (float)InteractivityManager.SingletonInstance.GetJoystick(controlID).X;
    }

    /// <summary>
    /// Returns the Y coordinate of the joystick with the given control ID.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    public static float GetJoystickY(string controlID)
    {
        return (float)InteractivityManager.SingletonInstance.GetJoystick(controlID).Y;
    }

    /// <summary>
    /// Returns whether the mouse button down event occured.
    /// </summary>
    /// <param name="buttonIndex">
    /// The index of the mouse button. Only 1 value is supported right now
    /// which is 0. 0 corresponds to the left mouse button.
    /// </param>
    public static bool GetMouseButtonDown(int buttonIndex = 0)
    {
        bool getButtonDownResult = false;
        Dictionary<uint, _InternalMouseButtonState> mouseButtonStateByParticipant = InteractivityManager._mouseButtonStateByParticipant;
        var mouseButtonStateByParticipantKeys = mouseButtonStateByParticipant.Keys;
        foreach (uint mouseButtonStateByParticipantKey in mouseButtonStateByParticipantKeys)
        {
            if (mouseButtonStateByParticipant[mouseButtonStateByParticipantKey].IsDown)
            {
                getButtonDownResult = true;
                break;
            }
        }
        return getButtonDownResult;
    }

    /// <summary>
    /// Returns whether the mouse button is currently pressed.
    /// </summary>
    /// <param name="buttonIndex">
    /// The index of the mouse button. Only 1 value is supported right now
    /// which is 0. 0 corresponds to the left mouse button.
    /// </param>
    public static bool GetMouseButton(int buttonIndex = 0)
    {
        bool getButtonDownResult = false;
        Dictionary<uint, _InternalMouseButtonState> mouseButtonStateByParticipant = InteractivityManager._mouseButtonStateByParticipant;
        var mouseButtonStateByParticipantKeys = mouseButtonStateByParticipant.Keys;
        foreach (uint mouseButtonStateByParticipantKey in mouseButtonStateByParticipantKeys)
        {
            if (mouseButtonStateByParticipant[mouseButtonStateByParticipantKey].IsPressed)
            {
                getButtonDownResult = true;
                break;
            }
        }
        return getButtonDownResult;
    }

    /// <summary>
    /// Returns whether the mouse button up event occured.
    /// </summary>
    /// <param name="buttonIndex">
    /// The index of the mouse button. Only 1 value is supported right now
    /// which is 0. 0 corresponds to the left mouse button.
    /// </param>
    public static bool GetMouseButtonUp(int buttonIndex = 0)
    {
        bool getButtonDownResult = false;
        Dictionary<uint, _InternalMouseButtonState> mouseButtonStateByParticipant = InteractivityManager._mouseButtonStateByParticipant;
        var mouseButtonStateByParticipantKeys = mouseButtonStateByParticipant.Keys;
        foreach (uint mouseButtonStateByParticipantKey in mouseButtonStateByParticipantKeys)
        {
            if (mouseButtonStateByParticipant[mouseButtonStateByParticipantKey].IsUp)
            {
                getButtonDownResult = true;
                break;
            }
        }
        return getButtonDownResult;
    }

    /// <summary>
    /// Gets a button control object by ID.
    /// </summary>
    /// <param name="controlID">The ID of the control.</param>
    /// <returns></returns>
    public static InteractiveButtonControl Button(string controlID)
    {
        return InteractivityManager.SingletonInstance.GetButton(controlID);
    }

    /// <summary>
    /// Gets the current scene for the default group.
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentScene()
    {
        return InteractivityManager.SingletonInstance.GetCurrentScene();
    }

    /// <summary>
    /// Sets the current scene for the default group.
    /// </summary>
    /// <param name="sceneID">The ID of the scene to change to.</param>
    public static void SetCurrentScene(string sceneID)
    {
        InteractivityManager.SingletonInstance.SetCurrentScene(sceneID);
    }

    /// <summary>
    /// Returns the specified group. Will return null if initialization
    /// is not yet complete or group does not exist.
    /// </summary>
    /// <param name="groupID">The ID of the group.</param>
    /// <returns></returns>
    public static InteractiveGroup GetGroup(string groupID)
    {
        return InteractivityManager.SingletonInstance.GetGroup(groupID);
    }

    /// <summary>
    /// Returns the specified scene. Will return nullptr if initialization
    /// is not yet complete or scene does not exist.
    /// </summary>
    public static InteractiveScene GetScene(string sceneID)
    {
        return InteractivityManager.SingletonInstance.GetScene(sceneID);
    }

    /// <summary>
    /// Sends a custom message. The format must be JSON.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public static void SendInteractiveMessage(string message)
    {
        InteractivityManager.SingletonInstance.SendMessage(message);
    }

    /// <summary>
    /// Sends a custom message. The message will be formatted as JSON automatically.
    /// </summary>
    /// <param name="messageType">The name of this type of message.</param>
    /// <param name="parameters">A collection of name / value pairs.</param>
    public static void SendInteractiveMessage(string messageType, Dictionary<string, object> parameters)
    {
        InteractivityManager.SingletonInstance.SendMessage(messageType, parameters);
    }

    /// <summary>
    /// Removes cached login information. This function removes saved project IDs and cached tokens.
    /// This is useful if you have multiple users switching between accounts on the same machine.
    /// </summary>
    public static void ClearSavedLoginInformation()
    {
        PlayerPrefs.DeleteKey("MixerInteractive-AuthToken");
        PlayerPrefs.DeleteKey("MixerInteractive-RefreshToken");
        PlayerPrefs.Save();
    }

    private IEnumerator InitializeCoRoutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://mixer.com/api/v1/interactive/hosts"))
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.Log("Error: Could not retrieve websocket URL. " + request.error);
            }
            else // Success
            {
                string websocketHostsJson = request.downloadHandler.text;
                InteractivityManager.SingletonInstance.Initialize(true, null);
            }
        }
    }

    /// <summary>
    /// This method returns a control matching the given ID.
    /// </summary>
    /// <param name="controlID">The ID of the control.</param>
    /// <returns>Returns the interactive control matching the given ID.</returns>
    public static InteractiveControl GetControl(string controlID)
    {
        return InteractivityManager.SingletonInstance._GetControl(controlID);
    }

    /// <summary>
    /// Returns a list of participants and the text they entered.
    /// </summary>
    /// <param name="controlID">String ID of the control.</param>
    /// <returns>Returns a list of InteractiveTextResult objects. Returns an empty list if there was no input.</returns>
    public static IList<InteractiveTextResult> GetText(string controlID)
    {
        return InteractivityManager.SingletonInstance._GetText(controlID);
    }

    /// <summary>
    /// Connects to the interactivity service and signals the service that the InteractivityManager is ready to recieve messages.
    /// It also, handles signals authentication events if necessary.
    /// </summary>
    public static void GoInteractive()
    {
        if (pendingGoInteractive)
        {
            return;
        }
        pendingGoInteractive = true;
        // We fire the OnGoInteractive event again even if we are already interactive, because
        // it could have been a scene change and the developer has updated group or scene data
        // in the InteractivityManager prefab.
        hasFiredGoInteractiveEvent = false;
        var interactivityManager = InteractivityManager.SingletonInstance;
        interactivityManager.OnInteractivityStateChanged -= HandleInteractivityStateChangedInternal;
        interactivityManager.OnInteractivityStateChanged += HandleInteractivityStateChangedInternal;

#if UNITY_WSA && !UNITY_EDITOR
        InitializeAsync();
#else
        // Run initialization in another thread.
        // Workaround - in certain cases Unity does not call the Start function, which means
        // initialization does not happen. We need to check if the background worker hasn't
        // been initialized and if not, initialize it.
        if (backgroundWorker == null)
        {
            backgroundWorker = new BackgroundWorker();
        }
        backgroundWorker.DoWork -= BackgroundWorkerDoWork;
        backgroundWorker.DoWork += BackgroundWorkerDoWork;
        backgroundWorker.RunWorkerAsync();
#endif

        if (MixerInteractiveHelper._SingletonInstance._runInBackgroundIfInteractive)
        {
            previousRunInBackgroundValue = Application.runInBackground;
            Application.runInBackground = true;
        }
    }

#if WINDOWS_UWP
    private static async void InitializeAsync()
    {
        await Task.Run(() =>
        {
            InteractivityManager.SingletonInstance.Initialize(true);
        });
    }
#endif

    private static void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        InteractivityManager.SingletonInstance.Initialize();
    }

    private static void HandleInteractivityStateChangedInternal(object sender, InteractivityStateChangedEventArgs e)
    {
        if (e == null)
        {
            return;
        }

        var state = e.State;
        switch (state)
        {
            case InteractivityState.ShortCodeRequired:
                if (!mixerDialog.gameObject.activeInHierarchy)
                {
                    mixerDialog.gameObject.SetActive(true);
                }
                mixerDialog.Show(InteractivityManager.SingletonInstance.ShortCode);
                break;
            case InteractivityState.InteractivityEnabled:
                mixerDialog.Hide();
                ProcessSerializedProperties();
                pendingGoInteractive = false;
                break;
            default:
                break;
        }
    }

    private static void ProcessSerializedProperties()
    {
        MixerInteractiveHelper helper = MixerInteractiveHelper._SingletonInstance;
        InteractivityManager interactivityManager = InteractivityManager.SingletonInstance;
        string defaultSceneID = helper._defaultSceneID;
        if (helper._groupSceneMapping.Count > 0 ||
            defaultSceneID != string.Empty)
        {
            shouldCheckForOutstandingRequests = true;
        }
        if (helper._groupSceneMapping.Count > 0)
        {
            var groupIDs = helper._groupSceneMapping.Keys;
            foreach (var groupID in groupIDs)
            {
                if (groupID == string.Empty)
                {
                    continue;
                }
                // Supress this warning because calling the contructor
                // triggers the creation of a group.
#pragma warning disable 0219
                InteractiveGroup group;
#pragma warning restore 0219
                string sceneID = helper._groupSceneMapping[groupID];
                if (sceneID != string.Empty)
                {
                    group = new InteractiveGroup(groupID, sceneID);
                }
                else
                {
                    group = new InteractiveGroup(groupID);
                }
                outstandingCreateGroupsRequests.Add(groupID);
            }
            if (defaultSceneID != string.Empty)
            {
                interactivityManager.SetCurrentScene(defaultSceneID);
                outstandingSetDefaultSceneRequest = defaultSceneID;
            }
        }
        processedSerializedProperties = true;
    }

    private static bool CheckForOutStandingRequestsCompleted()
    {
        bool outstandingRequestsCompleted = false;
        List<string> groupsToRemove = new List<string>();
        if (outstandingSetDefaultSceneRequest == string.Empty)
        {
            foreach (string groupID in outstandingCreateGroupsRequests)
            {
                foreach (InteractiveGroup group in InteractivityManager.SingletonInstance.Groups)
                {
                    if (group.GroupID == groupID)
                    {
                        groupsToRemove.Add(groupID);
                    }
                }
            }
            foreach (string groupID in groupsToRemove)
            {
                outstandingCreateGroupsRequests.Remove(groupID);
            }
        }
        else
        {
            foreach (InteractiveGroup group in InteractivityManager.SingletonInstance.Groups)
            {
                if (group.GroupID == DEFAULT_GROUP_ID &&
                    group.SceneID == outstandingSetDefaultSceneRequest)
                {
                    outstandingSetDefaultSceneRequest = string.Empty;
                    break;
                }
            }
        }

        if (outstandingCreateGroupsRequests.Count == 0 &&
            outstandingSetDefaultSceneRequest == string.Empty)
        {
            outstandingRequestsCompleted = true;
        }
        return outstandingRequestsCompleted;
    }

#if WINDOWS_UWP
    private static async Task<string> GetXTokenAsync()
    {
        string token = string.Empty;

        TaskCompletionSource<string> getTokenTaskCompletionSource = new TaskCompletionSource<string>();
        Task<string> getTokenTask = getTokenTaskCompletionSource.Task;

        try
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Get an XToken
                // Find the account provider using the signed in user.
                // We always use the 1st signed in user, because we just need a valid token. It doesn't
                // matter who's it is.
                Windows.System.User currentUser;
                WebTokenRequest request;
                var users = await Windows.System.User.FindAllAsync();
                if (users.Count > 0)
                {
                    currentUser = users[0];
                    WebAccountProvider xboxProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://xsts.auth.xboxlive.com", "", currentUser);

                    // Build the web token request using the account provider.
                    // Url = URL of the service we are getting a token for - for example https://apis.mycompany.com/something. 
                    // As this is a sample just use xboxlive.com
                    // Target & Policy should always be set to "xboxlive.signin" and "DELEGATION"
                    // For this call to succeed your console needs to be in the XDKS.1 sandbox
                    request = new Windows.Security.Authentication.Web.Core.WebTokenRequest(xboxProvider);
                    request.Properties.Add("Url", "https://mixer.com");
                    request.Properties.Add("Target", "xboxlive.signin");
                    request.Properties.Add("Policy", "DELEGATION");

                    // Request a token - correct pattern is to call getTokenSilentlyAsync and if that 
                    // fails with WebTokenRequestStatus.userInteractionRequired then call requestTokenAsync
                    // to get the token and prompt the user if required.
                    // getTokenSilentlyAsync can be called on a background thread.
                    WebTokenRequestResult tokenResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request);
                    //If we got back a token call our service with that token 
                    if (tokenResult.ResponseStatus == WebTokenRequestStatus.Success)
                    {
                        token = tokenResult.ResponseData[0].Token;
                    }
                    else if (tokenResult.ResponseStatus == WebTokenRequestStatus.UserInteractionRequired)
                    { // WebTokenRequestStatus.userInteractionRequired = 3
                      // If user interaction is required then call requestTokenAsync instead - this will prompt for user permission if required
                      // Note: RequestTokenAsync cannot be called on a background thread.
                        WebTokenRequestResult tokenResult2 = await WebAuthenticationCoreManager.RequestTokenAsync(request);
                        //If we got back a token call our service with that token
                        string tokenResultString = tokenResult2.ResponseStatus.ToString();
                        if (tokenResult2.ResponseStatus == WebTokenRequestStatus.Success)
                        {
                            token = tokenResult.ResponseData[0].Token;
                        }
                        else if (tokenResult2.ResponseStatus == WebTokenRequestStatus.UserCancel)
                        {
                            Debug.Log("Error: Unable to get an XToken, the user denied this app access to get an XToken.");
                        }
                        else if (tokenResult2.ResponseStatus == WebTokenRequestStatus.ProviderError)
                        {
                            Debug.Log("Error: Unable to get an XToken, please check the IDs for this game.");
                        }
                    }
                    else if (tokenResult.ResponseStatus == WebTokenRequestStatus.ProviderError)
                    {
                        Debug.Log("Error: Unable to get an XToken, please check the IDs for this game.");
                    }
                    getTokenTaskCompletionSource.SetResult(token);
                }
                else
                {
                    Debug.Log("Error: No users signed in.");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.Log("Error: Unexpected error retrieving an XToken. Exception details: " + ex.Message);
        }

        token = getTokenTask.Result;
        return token;
    }
#endif

    private static void CaptureTransactionForButtonControlID(string controlID)
    {
        var buttons = Buttons;
        var buttonStateKeys = InteractivityManager._buttonStates.Keys;
        foreach (string buttonStateKey in buttonStateKeys)
        {
            if (buttonStateKey == controlID)
            {
                InteractivityManager.SingletonInstance.CaptureTransaction(
                    InteractivityManager._buttonStates[buttonStateKey].TransactionID
                    );
                break;
            }
        }
    }

    private static void CaptureTransactionForControlID(string controlID)
    {
        var transactionIDsStateKeys = InteractivityManager._transactionIDsState.Keys;
        foreach (string transactionControlID in transactionIDsStateKeys)
        {
            if (transactionControlID == controlID)
            {
                InteractivityManager.SingletonInstance.CaptureTransaction(
                    InteractivityManager._transactionIDsState[transactionControlID].transactionID
                    );
                break;
            }
        }
    }

    void OnDestroy()
    {
        ResetInternalState();
    }

    void OnApplicationQuit()
    {
        StopInteractive();
    }

    // Custom controls
    public class RpcCachedMethodInfo
    {
        public MonoBehaviour owningMonoBehavior;
        public MethodInfo methodInfo;
    }

    public class ObservedCachedFieldInfo
    {
        public FieldInfo fieldInfo;
        public object owningObject;
        public float updateInterval;
        public float lastSendTime;
        public string previousValueAsString;
    }

    public struct MixerHelperParameterInfo
    {
        public string typeName;
        public string typeValue;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class MixerSyncVar : Attribute
{
    public float updateInterval;
    public MixerSyncVar(double newUpdateInterval = MixerInteractive._DEFAULT_MIXER_SYNCVAR_UPDATE_INTERVAL)
    {
        updateInterval = (float)newUpdateInterval;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class MixerRpcMethod : Attribute { }