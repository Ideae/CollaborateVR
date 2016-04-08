/**
* PSMove API - A Unity5 plugin for the PSMove motion controller.
*              Derived from the psmove-ue4 plugin by Chadwick Boulay
*              and the UniMove plugin by the Copenhagen Game Collective
* Copyright (C) 2015, PolyarcGames (http://www.polyarcgames.com)
*                   Brendan Walker (brendan@polyarcgames.com)
* 
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*    1. Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*
*    2. Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
* LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
* CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
* SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
* INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
* CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
* ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
* POSSIBILITY OF SUCH DAMAGE.
**/

using UnityEngine;
using System.Collections;
using System;

// Per-Move Controller data needed by the worker.
public class PSMoveRawControllerData_Base
{
    // -------
    // Data items that typically get filled by the Worker thread
    // then are made available to the consuming game object (i.e. component)
    public Vector3 PSMovePosition;
    public Quaternion PSMoveOrientation;
    public UInt32 Buttons;
    public byte TriggerValue;
    public bool IsConnected; // Whether or not the controller is connected
    public bool IsSeenByTracker; // Whether or not the tracker saw the controller on the latest frame.
    public bool IsTrackingEnabled; // Whether or not the tracker is tracking this specific controller
    public int SequenceNumber; // Increments with every update

    // -------
    // Data items that typically get filled by the game object (i.e., PSMoveComponent)
    // then are passed to the worker thread.
    public byte RumbleRequest; // Please make the controller rumbled
    public bool CycleColourRequest; // Please change to the next available color.

    public PSMoveRawControllerData_Base()
    {
        Clear();
    }

    public void Clear()
    {
        PSMovePosition = Vector3.zero;
        PSMoveOrientation = Quaternion.identity;
        Buttons = 0;
        TriggerValue = 0;
        RumbleRequest = 0;
        CycleColourRequest = false;
        IsConnected = false;
        IsSeenByTracker = false;
        IsTrackingEnabled = false;
        SequenceNumber = 0;
    }
};

public class PSMoveRawControllerData_Concurrent : PSMoveRawControllerData_Base
{
    // Used to make sure we're accessing the data in a thread safe way
    public object Lock;

    public PSMoveRawControllerData_Concurrent() : base()
    {
        Lock = new object();
    }
};

public class PSMoveRawControllerData_TLS : PSMoveRawControllerData_Base
{
    // This is a pointer to the 
    private PSMoveRawControllerData_Concurrent ConcurrentData;

    public PSMoveRawControllerData_TLS(PSMoveRawControllerData_Concurrent concurrentData)
        : base()
    {
        ConcurrentData = concurrentData;
    }

    public bool IsValid()
    {
        return ConcurrentData != null;
    }

    public void ComponentPost()
    {
        using (new PSMoveHitchWatchdog("PSMoveRawControllerData_TLS::ComponentPost", 500))
        {
            lock (ConcurrentData.Lock)
            {
                // Post the component thread's data to the worker thread's data
                ConcurrentData.RumbleRequest = this.RumbleRequest;
                ConcurrentData.CycleColourRequest = this.CycleColourRequest;

                // Clear the edge triggered flags after copying to concurrent state
                this.CycleColourRequest = false;
            }
        }
    }

    public void ComponentRead()
    {
        using (new PSMoveHitchWatchdog("PSMoveRawControllerData_TLS::ComponentRead", 500))
        {
            lock (ConcurrentData.Lock)
            {
                // Read the worker thread's data into the component thread's data
                this.PSMovePosition = ConcurrentData.PSMovePosition;
                this.PSMoveOrientation = ConcurrentData.PSMoveOrientation;
                this.Buttons = ConcurrentData.Buttons;
                this.TriggerValue = ConcurrentData.TriggerValue;
                this.IsConnected = ConcurrentData.IsConnected;
                this.IsSeenByTracker = ConcurrentData.IsSeenByTracker;
                this.IsTrackingEnabled = ConcurrentData.IsTrackingEnabled;
                this.SequenceNumber = ConcurrentData.SequenceNumber;
            }
        }
    }

    public void WorkerRead()
    {
        using (new PSMoveHitchWatchdog("PSMoveRawControllerData_TLS::WorkerRead", 500))
        {
            lock (ConcurrentData.Lock)
            {
                // Read the component thread's data into the worker thread's data
                this.RumbleRequest = ConcurrentData.RumbleRequest;
                this.CycleColourRequest = ConcurrentData.CycleColourRequest;

                // Clear the edge triggered flags after copying from concurrent state
                ConcurrentData.CycleColourRequest = false;
            }
        }
    }

    public void WorkerPost()
    {
        using (new PSMoveHitchWatchdog("PSMoveRawControllerData_TLS::WorkerPost", 500))
        {
            lock (ConcurrentData.Lock)
            {
                // Since we just updated the controller data, bump the sequence data
                this.SequenceNumber++;

                // Post the worker thread's data to the component thread's data
                ConcurrentData.PSMovePosition = this.PSMovePosition;
                ConcurrentData.PSMoveOrientation = this.PSMoveOrientation;
                ConcurrentData.Buttons = this.Buttons;
                ConcurrentData.TriggerValue = this.TriggerValue;
                ConcurrentData.IsConnected = this.IsConnected;
                ConcurrentData.IsSeenByTracker = this.IsSeenByTracker;
                ConcurrentData.IsTrackingEnabled = this.IsTrackingEnabled;
                ConcurrentData.SequenceNumber = this.SequenceNumber;
            }
        }
    }
};

public class PSMoveDataContext
{
    public int PSMoveID { get; private set; }
    public PSMovePose Pose { get; private set; }

    // Controller Data bound to data in the worker thread    
    PSMoveRawControllerData_TLS RawControllerData;

    // Controller Data from previous frame
    UInt32 RawControllerPreviousButtons;
    byte RawControllerPreviousTriggerValue;

    public PSMoveDataContext(
        int moveID,
        PSMoveRawControllerData_Concurrent controllerConcurrentData)
    {
        PSMoveID = moveID;
        Pose = new PSMovePose();
        RawControllerData = new PSMoveRawControllerData_TLS(controllerConcurrentData);
    }

    public void Clear()
    {
        PSMoveID = -1;
        Pose.Clear();
        RawControllerData.Clear();
        RawControllerPreviousButtons = 0;
        RawControllerPreviousTriggerValue = 0;
    }

    public void ComponentPost()
    {
        if (RawControllerData.IsValid())
        {
            RawControllerData.ComponentPost();
        }
    }

    public void ComponentRead(Transform ParentGameObjectTransform)
    {
        if (RawControllerData.IsValid())
        {
            // Backup controller state from the previous frame that we want to compute deltas on
            // before it gets stomped by InputManagerPostAndRead()
            int CurrentSequenceNumber = RawControllerData.SequenceNumber;
            byte CurrentTriggerValue = RawControllerData.TriggerValue;
            UInt32 CurrentButtons = RawControllerData.Buttons;

            // If the worker thread updated the controller, the sequence number will change
            RawControllerData.ComponentRead();

            // If the worker thread posted new data then the sequence number should changes
            //EDIT
            //if (RawControllerData.SequenceNumber != CurrentSequenceNumber)
            //{
                // Actually update the previous controller state when we get new data
                RawControllerPreviousTriggerValue = CurrentTriggerValue;
                RawControllerPreviousButtons = CurrentButtons;
            //}

            // Refresh the world space controller pose
            Pose.PoseUpdate(this, ParentGameObjectTransform);
        }
    }

    public void PostRumbleRequest(byte RequestedRumbleValue)
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            RawControllerData.RumbleRequest = RequestedRumbleValue;
            ComponentPost();
        }
    }

    public void PostCycleColourRequest()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            RawControllerData.CycleColourRequest = true;
            ComponentPost();
        }
    }

    public void ResetYaw()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            Pose.SnapshotOrientationYaw();
        }
    }

    // Controller Data Functions
    public Vector3 GetTrackingSpacePosition()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.PSMovePosition;
        } 
        else 
        {
            return Vector3.zero;
        }
    }

    public Quaternion GetTrackingSpaceOrientation()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.PSMoveOrientation;
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public bool GetIsConnected()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.IsConnected;
        }
        else
        {
            return false;
        }
    }

    public bool GetIsSeenByTracker()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.IsSeenByTracker;
        }
        else 
        {
            return false;
        }
    }
    
    public bool GetIsTrackingEnabled()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.IsTrackingEnabled;
        }
        else 
        {
            return false;
        }
    }
    
    // Current Button State
    public bool GetButtonTriangle()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Triangle) != 0;
        }
        else
        {
            return false;
        }
    }
    
    public bool GetButtonCircle()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Circle) != 0;
        }
        else
        {
            return false;
        }
    }
    
    public bool GetButtonCross()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Cross) != 0;
        }
        else
        {
            return false;
        }
    }
    
    public bool GetButtonSquare()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Square) != 0;
        }
        else 
        {
            return false;
        }
    }
    
    public bool GetButtonSelect()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Select) != 0;
        }
        else 
        {
            return false;
        }
    }
    
    public bool GetButtonStart()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Start) != 0;
        }
        else 
        {
            return false;
        }
    }
    
    public bool GetButtonPS()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.PS) != 0;
        }
        else
        {
            return false;
        }
    }
    
    public bool GetButtonMove()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return (RawControllerData.Buttons & (UInt32)PSMoveButton.Move) != 0;
        }
        else
        {
            return false;
        }
    }

	public byte GetPreviousTriggerValue()
	{
		return RawControllerPreviousTriggerValue;
	}
    
    public byte GetTriggerValue()
    {
        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            return RawControllerData.TriggerValue;
        }
        else
        {
            return 0;
        }
    }

    // Pressed This Frame
    public bool GetButtonPressedThisFrame(PSMoveButton ButtonMask)
    {
        bool PressedThisFrame = false;

        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            PressedThisFrame =
                (RawControllerPreviousButtons & (UInt32)ButtonMask) == 0 &&
                (RawControllerData.Buttons & (UInt32)ButtonMask) != 0;
        }

        return PressedThisFrame;
    }

    public bool GetButtonTrianglePressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Triangle);
    }

    public bool GetButtonCirclePressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Circle);
    }

    public bool GetButtonCrossPressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Cross);
    }

    public bool GetButtonSquarePressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Square);
    }

    public bool GetButtonSelectPressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Select);
    }

    public bool GetButtonStartPressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Start);
    }

    public bool GetButtonPSPressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.PS);
    }

    public bool GetButtonMovePressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Move);
    }

    public bool GetButtonTriggerPressed()
    {
        return GetButtonPressedThisFrame(PSMoveButton.Trigger);
    }

    // Released This Frame
    public bool GetButtonReleasedThisFrame(PSMoveButton ButtonMask)
    {
        bool ReleasedThisFrame = false;

        if (RawControllerData.IsValid() && RawControllerData.IsConnected)
        {
            ReleasedThisFrame =
                (RawControllerPreviousButtons & (UInt32)ButtonMask) != 0 &&
                (RawControllerData.Buttons & (UInt32)ButtonMask) == 0;
        }

        return ReleasedThisFrame;
    }

    public bool GetButtonTriangleReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Triangle);
    }

    public bool GetButtonCircleReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Circle);
    }

    public bool GetButtonCrossReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Cross);
    }

    public bool GetButtonSquareReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Square);
    }

    public bool GetButtonSelectReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Select);
    }

    public bool GetButtonStartReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Start);
    }

    public bool GetButtonPSReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.PS);
    }

    public bool GetButtonMoveReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Move);
    }

    public bool GetButtonTriggerReleased()
    {
        return GetButtonReleasedThisFrame(PSMoveButton.Trigger);
    }  
}
