# How to add typed network messages to a VR2Gather experience

VR2Gather's session networking routes all user-to-user communication through the master client. This document explains how to send and receive typed messages between participants — for example, to synchronise game state, trigger animations, or signal completion.

The API is in `VRT.OrchestratorComm` (`OrchestratorCommExtensions.cs`, `MessageForwarder.cs`, `BaseMessage.cs`).

---

## Overview of the architecture

```
Non-master --SendTypeEventToMaster()--> Master
                                           |
                              SendTypeEventToAll() (with forward=true)
                                           |
                                    All participants
                                  (including non-master)
```

- Only the master can send directly to all participants via `SendTypeEventToAll`.
- Non-master clients send to the master first; the master's handler then forwards to everyone.
- `SendTypeEventToAll` does **not** echo back to the sender. However, when the master forwards a non-master's message, the original sender receives the forwarded copy (with the original `SenderId` preserved). Your handler should drop those self-echoes.

---

## Step 1 — Assign a message type ID

`MessageTypeID` is an enum defined in `MessageForwarder.cs`. The VR2Gather package occupies values 100–199. **Do not modify the package file.** Instead, allocate a block for your experience and cast integers to the enum:

```csharp
// In a constants file for your experience, e.g. MyAppMessages.cs
internal static class MyAppMsgID
{
    public const int FooEvent = 200;   // first free slot for this experience
    public const int BarEvent = 201;
}
```

If multiple experiences share a VR2Gather installation, coordinate ID ranges between them (e.g., 200–299 for one experience, 300–399 for another).

---

## Step 2 — Define a message class

Each message type is a plain C# class that inherits from `BaseMessage`. Add public fields for any payload data; Unity's `JsonUtility` serialises them.

```csharp
using VRT.OrchestratorComm;

namespace MyApp
{
    public class FooEventMessage : BaseMessage
    {
        public string playerId;
        public long   timestampMs;
    }

    public class BarEventMessage : BaseMessage { }   // no payload
}
```

`BaseMessage` already provides `SenderId` (set automatically by the framework) and `TimeStamp`.

---

## Step 3 — Register the type mapping

Call `RegisterEventType` once per message type, before any messages are sent or received. `Awake()` is the right place. The call is idempotent, so it is safe to call from multiple component instances or multiple scene loads.

```csharp
using VRT.Orchestrator;
using VRT.OrchestratorComm;

public class MyController : MonoBehaviour
{
    void Awake()
    {
        VRTOrchestratorSingleton.Comm.RegisterEventType(
            (MessageTypeID)MyAppMsgID.FooEvent, typeof(FooEventMessage));
        VRTOrchestratorSingleton.Comm.RegisterEventType(
            (MessageTypeID)MyAppMsgID.BarEvent, typeof(BarEventMessage));
    }
```

---

## Step 4 — Subscribe and unsubscribe

Use `OnEnable` / `OnDisable` rather than `Start` / `OnDestroy`. This ensures subscriptions are cleaned up correctly if the component is disabled and re-enabled between scene loads.

```csharp
    void OnEnable()
    {
        VRTOrchestratorSingleton.Comm.Subscribe<FooEventMessage>(OnFooEvent);
        VRTOrchestratorSingleton.Comm.Subscribe<BarEventMessage>(OnBarEvent);
    }

    void OnDisable()
    {
        VRTOrchestratorSingleton.Comm?.Unsubscribe<FooEventMessage>(OnFooEvent);
        VRTOrchestratorSingleton.Comm?.Unsubscribe<BarEventMessage>(OnBarEvent);
    }
```

The `?.` null-conditional on `Unsubscribe` guards against the singleton having already been torn down when the scene unloads.

---

## Step 5 — Send a message

### Pattern A: master is the only sender (e.g. a game-start signal)

```csharp
    void OnSomeLocalEvent()
    {
        if (!VRTOrchestratorSingleton.Comm.UserIsMaster) return;
        VRTOrchestratorSingleton.Comm.SendTypeEventToAll(new BarEventMessage());
        // master also handles the effect locally — there is no echo to self
        ApplyBarEffect();
    }
```

### Pattern B: any participant can trigger (e.g. a player action)

```csharp
    void OnLocalPlayerAction()
    {
        var msg = new FooEventMessage
        {
            playerId    = VRTOrchestratorSingleton.Comm.SelfUser.userId,
            timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg);
        else
            VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(msg);

        ApplyFooEffect(msg.playerId);   // apply locally immediately
    }
```

---

## Step 6 — Handle incoming messages

All participants (including master) receive a callback for each subscribed message type. Your handler must:

1. If you are the master and the message came from a non-master client, **forward it to everyone** (passing `true` preserves the original `SenderId`).
2. **Drop self-echoes.** When the master forwards a non-master's message, the original sender receives it back. Check `SenderId` and return early if it matches your own user ID.
3. Apply the effect.

```csharp
    void OnFooEvent(FooEventMessage msg)
    {
        // 1. Forward if master (covers the non-master-sender case)
        if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);

        // 2. Drop self-echo
        if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;

        // 3. Apply effect
        ApplyFooEffect(msg.playerId);
    }

    void OnBarEvent(BarEventMessage msg)
    {
        // BarEvent is master-only (Pattern A), so no forwarding needed.
        // Drop self-echo just in case.
        if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
        ApplyBarEffect();
    }
```

> **Why forward in the handler rather than at send time?**  
> The master may itself be a participant that triggers the event (Pattern B). Using a single forwarding path in the handler keeps the logic consistent regardless of which client originally sent the message.

---

## Complete minimal example

```csharp
using System;
using UnityEngine;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace MyApp
{
    // ----- message definition -----
    internal static class MyAppMsgID { public const int Ready = 200; }
    public class ReadyMessage : BaseMessage { }

    // ----- component -----
    public class ReadySync : MonoBehaviour
    {
        bool _remoteReady;

        void Awake()
        {
            VRTOrchestratorSingleton.Comm.RegisterEventType(
                (MessageTypeID)MyAppMsgID.Ready, typeof(ReadyMessage));
        }

        void OnEnable()  => VRTOrchestratorSingleton.Comm.Subscribe<ReadyMessage>(OnReady);
        void OnDisable() => VRTOrchestratorSingleton.Comm?.Unsubscribe<ReadyMessage>(OnReady);

        public void SignalReady()
        {
            var msg = new ReadyMessage();
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg);
            else
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(msg);
        }

        void OnReady(ReadyMessage msg)
        {
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            _remoteReady = true;
        }
    }
}
```

---

## Reference: existing VR2Gather type IDs (100–199)

| ID  | Type constant                      | Used by                     |
|-----|------------------------------------|-----------------------------|
| 100 | TID_NetworkPlayerData              | Player transform sync        |
| 101 | TID_HandControllerData             | Hand/controller data         |
| 102 | TID_NetworkTriggerData             | NetworkTrigger               |
| 103 | TID_PlayerLocationData             | Player location              |
| 104 | TID_PlayerLocationDataRequest      | Location request             |
| 105 | TID_PlayerLocationChangeRequest    | Location change request      |
| 106 | TID_HandGrabEvent                  | Grab events                  |
| 107 | TID_RigidBodyData                  | Rigidbody sync               |
| 108 | TID_RigidbodySyncMessage           | Rigidbody sync               |
| 109 | TID_TextChatDataMessage            | Text chat                    |
| 110 | TID_TilingConfigMessage            | TilingConfigDistributor      |
| 111 | TID_InitCompleteMessage            | Session init                 |
| 112 | TID_KeywordsResponseData           | Keywords                     |
| 113 | TID_PlayerTransformSyncData        | Player transform             |
| 114 | TID_AddPlayerToSequenceData        | Player sequencing            |
| 115 | TID_SyncConfigMessage              | SyncConfigDistributor        |
| 116 | TID_NetworkInstantiatorData        | NetworkInstantiator          |
| 117 | TID_PersistenceManagerData         | PersistenceManager           |

Allocate your experience's IDs starting at 200 (or higher if multiple experiences share one installation).
