/**
* PSMove API - A Unity5 plugin for the PSMove motion controller.
*              Derived from the psmove-ue4 plugin by Chadwick Boulay
*              and the UniMove plugin by the Copenhagen Game Collective
* Copyright (C) 2015, PolyarcGames (http://www.polyarcgames.com)
* 					   Brendan Walker (brendan@polyarcgames.com)
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

using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class PSMoveController : MonoBehaviour
{
    // PSMove controller ID - 0-based
    public int PSMoveID = 0;

	// Pressed This Frame
    public event EventHandler OnButtonTrianglePressed;
    public event EventHandler OnButtonCirclePressed;
    public event EventHandler OnButtonCrossPressed;
    public event EventHandler OnButtonSquarePressed;
    public event EventHandler OnButtonSelectPressed;
    public event EventHandler OnButtonStartPressed;
    public event EventHandler OnButtonPSPressed;
    public event EventHandler OnButtonMovePressed;

    // Released This Frame
    public event EventHandler OnButtonTriangleReleased;
    public event EventHandler OnButtonCircleReleased;
    public event EventHandler OnButtonCrossReleased;
    public event EventHandler OnButtonSquareReleased;
    public event EventHandler OnButtonSelectReleased;
    public event EventHandler OnButtonStartReleased;
    public event EventHandler OnButtonPSReleased;
    public event EventHandler OnButtonMoveReleased;

    // Used to send and receive controller data from the PSMoveWorker thread
    private PSMoveDataContext dataContext;

    public bool useInterpolation;
    public float lerpRate;
    private Vector3 lerpTarget;

  

  #region Controller Properties
  public bool IsConnected
    {
        get { return dataContext.GetIsConnected(); }
    }

    public bool IsEnabled
    {
        get { return dataContext.GetIsTrackingEnabled(); }
    }

    public bool IsTracking
    {
        get { return dataContext.GetIsSeenByTracker(); }
    }

    public float TriggerValue
    {
        get { return (float)dataContext.GetTriggerValue() / 255.0f; }
    }

    public bool IsTriangleButtonDown
    { 
        get { return dataContext.GetButtonTriangle(); } 
    }

    public bool IsCircleButtonDown
    {
        get { return dataContext.GetButtonCircle(); }
    }

    public bool IsCrossButtonDown
    {
        get { return dataContext.GetButtonCross(); }
    }

    public bool IsSquareButtonDown
    {
        get { return dataContext.GetButtonSquare(); }
    }

    public bool IsSelectButtonDown
    {
        get { return dataContext.GetButtonSelect(); }
    }

    public bool IsStartButtonDown
    {
        get { return dataContext.GetButtonStart(); }
    }

    public bool IsPSButtonDown
    {
        get { return dataContext.GetButtonPS(); }
    }

    public bool IsMoveButtonDown
    {
        get { return dataContext.GetButtonMove(); }
    }

  public bool GetPSButton(PSMoveButton psButton)
  {
    return dataContext.GetPSButton(psButton);
  }

  private Dictionary<PSMoveButton, bool> isDownDict = new Dictionary<PSMoveButton, bool>();


    // Debug
  public bool ShowTrackingDebug;
    public bool ShowHMDFrustumDebug;
    
  #endregion

  #region Controller Actions
  /// <summary>
  /// Converts the current orientation to the identity orientation
  /// </summary
  public void ResetYaw()
    {
        if (dataContext != null)
        {
            dataContext.ResetYaw();
        }
    }

    public void CycleTrackingColor()
    {
        if (dataContext != null)
        {
            dataContext.PostCycleColourRequest();
        }
    }

    /// <summary>
    /// Sets the amount of rumble
    /// </summary>
    /// <param name="rumble">the rumble amount (0-1)</param>
    public void SetRumble(float rumble)
    {
        if (dataContext != null)
        {
            // Clamp value between 0 and 1:
            dataContext.PostRumbleRequest((byte)(Math.Min(Math.Max(rumble, 0f), 1f) * 255));
        }
    }
    #endregion

    #region Unity Callbacks
    /// <summary>
    /// NOTE! This function does NOT pair the controller by Bluetooth.
    /// If the controller is not already paired, it can only be connected by USB.
    /// See README for more information.
    /// </summary>
	public void Start()
	{
        if (PSMoveManager.GetManagerInstance() != null)
        {
            dataContext = PSMoveManager.GetManagerInstance().AcquirePSMove(this.PSMoveID);
        }
	}

    public void OnDestroy()
    {
        if (PSMoveManager.GetManagerInstance() != null)
        {
            PSMoveManager.GetManagerInstance().ReleasePSMove(dataContext);
        }
    }

  void Update() 
	{
        // Get the latest state from the 
        dataContext.ComponentRead(this.gameObject.transform.parent);

    //Debug.Log("hello");

        // Button Pressed Handlers
        if (OnButtonTrianglePressed != null && dataContext.GetButtonTrianglePressed())
            OnButtonTrianglePressed(this, EventArgs.Empty);
        if (OnButtonCirclePressed != null && dataContext.GetButtonCirclePressed())
            OnButtonCirclePressed(this, EventArgs.Empty);
        if (OnButtonCrossPressed != null && dataContext.GetButtonCrossPressed())
            OnButtonCrossPressed(this, EventArgs.Empty);
        if (OnButtonSquarePressed != null && dataContext.GetButtonSquarePressed())
            OnButtonSquarePressed(this, EventArgs.Empty);
        if (OnButtonSelectPressed != null && dataContext.GetButtonSelectPressed())
            OnButtonSelectPressed(this, EventArgs.Empty);
        if (OnButtonStartPressed != null && dataContext.GetButtonStartPressed())
            OnButtonStartPressed(this, EventArgs.Empty);
        if (OnButtonPSPressed != null && dataContext.GetButtonPSPressed())
            OnButtonPSPressed(this, EventArgs.Empty);
        if (OnButtonMovePressed != null && dataContext.GetButtonMovePressed())
            OnButtonMovePressed(this, EventArgs.Empty);

        // Button Released Handlers
        if (OnButtonTriangleReleased != null && dataContext.GetButtonTriangleReleased())
            OnButtonTriangleReleased(this, EventArgs.Empty);
        if (OnButtonCircleReleased != null && dataContext.GetButtonCircleReleased())
            OnButtonCircleReleased(this, EventArgs.Empty);
        if (OnButtonCrossReleased != null && dataContext.GetButtonCrossReleased())
            OnButtonCrossReleased(this, EventArgs.Empty);
        if (OnButtonSquareReleased != null && dataContext.GetButtonSquareReleased())
            OnButtonSquareReleased(this, EventArgs.Empty);
        if (OnButtonSelectReleased != null && dataContext.GetButtonSelectReleased())
            OnButtonSelectReleased(this, EventArgs.Empty);
        if (OnButtonStartReleased != null && dataContext.GetButtonStartReleased())
            OnButtonStartReleased(this, EventArgs.Empty);
        if (OnButtonPSReleased != null && dataContext.GetButtonPSReleased())
            OnButtonPSReleased(this, EventArgs.Empty);
        if (OnButtonMoveReleased != null && dataContext.GetButtonMoveReleased())
            OnButtonMoveReleased(this, EventArgs.Empty);

        bool seen = dataContext.GetIsSeenByTracker();
        // Update the transform of this game object based on the new pose

        if (useInterpolation)
        {
          if (seen)
          {
            lerpTarget = dataContext.Pose.WorldPosition;
          }
          this.gameObject.transform.position = Vector3.Lerp(transform.position, lerpTarget, lerpRate);
        }
        else if (seen)
        {
          this.gameObject.transform.position = dataContext.Pose.WorldPosition;
        }





        this.gameObject.transform.rotation = dataContext.Pose.WorldOrientation;

            // Show the HMD frus
            if (ShowHMDFrustumDebug)
            {
                PSMoveUtility.DebugDrawHMDFrustum(this.gameObject.transform.parent);
            }
        }
    #endregion
}
