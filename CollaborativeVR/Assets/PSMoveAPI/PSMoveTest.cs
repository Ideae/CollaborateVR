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
using System;
using System.Collections.Generic;

public class PSMoveTest : MonoBehaviour 
{
    public bool HideWhenUntracked = false;

    private PSMoveController moveComponent;
    private MeshRenderer rendererComponent;

	void Start() 
	{
        moveComponent = gameObject.GetComponent<PSMoveController>();
        rendererComponent = gameObject.GetComponent<MeshRenderer>();

        // Button Pressed callbacks
        moveComponent.OnButtonTrianglePressed += move_OnButtonTrianglePressed;
        moveComponent.OnButtonCirclePressed += move_OnButtonCirclePressed;
        moveComponent.OnButtonCrossPressed += move_OnButtonCrossPressed;
        moveComponent.OnButtonSquarePressed += move_OnButtonSquarePressed;
        moveComponent.OnButtonSelectPressed += move_OnButtonSelectPressed;
        moveComponent.OnButtonStartPressed += move_OnButtonStartPressed;
        moveComponent.OnButtonPSPressed += move_OnButtonPSPressed;
        moveComponent.OnButtonMovePressed += move_OnButtonMovePressed;

        // Button Release callbacks
        moveComponent.OnButtonTriangleReleased += move_OnButtonTriangleReleased;
        moveComponent.OnButtonCircleReleased += move_OnButtonCircleReleased;
        moveComponent.OnButtonCrossReleased += move_OnButtonCrossReleased;
        moveComponent.OnButtonSquareReleased += move_OnButtonSquareReleased;
        moveComponent.OnButtonSelectReleased += move_OnButtonSelectReleased;
        moveComponent.OnButtonStartReleased += move_OnButtonStartReleased;
        moveComponent.OnButtonPSReleased += move_OnButtonPSReleased;
        moveComponent.OnButtonMoveReleased += move_OnButtonMoveReleased;

        // Don't show the controller model until we get tracking
        if (HideWhenUntracked && rendererComponent != null)
        {
            rendererComponent.enabled = false;
        }
    }

    void Update() 
    {
        if (moveComponent != null)
        {
            if (HideWhenUntracked && rendererComponent != null)
            {
                if (moveComponent.IsTracking && !rendererComponent.enabled)
                {
                    // Gained tracking - show the controller model
                    rendererComponent.enabled = true;
                }
                else if (!moveComponent.IsTracking && rendererComponent.enabled)
                {
                    // Lost tracking - hide the controller model
                    rendererComponent.enabled = false;
                }
            }

            // Set the rumble based on how much the trigger is down
            //moveComponent.SetRumble(moveComponent.TriggerValue);
        }
    }

    #region Button Pressed Callbacks
    void move_OnButtonTrianglePressed(object sender, EventArgs e)
    {
        Debug.Log("Triangle button pressed");
    }

    void move_OnButtonCirclePressed(object sender, EventArgs e)
    {
        Debug.Log("Circle button pressed");
        moveComponent.CycleTrackingColor();
    }

    void move_OnButtonCrossPressed(object sender, EventArgs e)
    {
        Debug.Log("Cross button pressed");
    }

    void move_OnButtonSquarePressed(object sender, EventArgs e)
    {
        Debug.Log("Square button pressed");
    }

    void move_OnButtonSelectPressed(object sender, EventArgs e)
    {
        Debug.Log("Select button pressed");
    }

    void move_OnButtonStartPressed(object sender, EventArgs e)
    {
        Debug.Log("Start button pressed");
    }

    void move_OnButtonPSPressed(object sender, EventArgs e)
    {
        Debug.Log("PS button pressed");
    }

    void move_OnButtonMovePressed(object sender, EventArgs e)
    {
        Debug.Log("Move button pressed");
        moveComponent.ResetYaw();
    }
    #endregion

    #region Button Released Callbacks
    void move_OnButtonTriangleReleased(object sender, EventArgs e)
    {
        Debug.Log("Triangle button released");
    }

    void move_OnButtonCircleReleased(object sender, EventArgs e)
    {
        Debug.Log("Circle button released");
    }

    void move_OnButtonCrossReleased(object sender, EventArgs e)
    {
        Debug.Log("Cross button released");
    }

    void move_OnButtonSquareReleased(object sender, EventArgs e)
    {
        Debug.Log("Square button released");
    }

    void move_OnButtonSelectReleased(object sender, EventArgs e)
    {
        Debug.Log("Select button released");
    }

    void move_OnButtonStartReleased(object sender, EventArgs e)
    {
        Debug.Log("Start button released");
    }

    void move_OnButtonPSReleased(object sender, EventArgs e)
    {
        Debug.Log("PS button released");
    }

    void move_OnButtonMoveReleased(object sender, EventArgs e)
    {
        Debug.Log("Move button released");
    }
    #endregion


    void OnGUI() 
    {
        if (moveComponent != null)
        {
            string display = "";

            if (moveComponent.IsConnected)
            {
                display = string.Format("PS Move {0} - Connected: Triangle[{0}] Circle[{1}] Cross[{2}] Square[{3}] Select[{4}] Start [{5}] PS[{6}] Move[{7}] Trigger[{8}]\n",
                    moveComponent.PSMoveID,
                    moveComponent.IsTriangleButtonDown ? 1 : 0,
                    moveComponent.IsCircleButtonDown ? 1 : 0,
                    moveComponent.IsCrossButtonDown ? 1 : 0,
                    moveComponent.IsSquareButtonDown ? 1 : 0,
                    moveComponent.IsSelectButtonDown ? 1 : 0,
                    moveComponent.IsStartButtonDown ? 1 : 0,
                    moveComponent.IsPSButtonDown ? 1 : 0,
                    moveComponent.IsMoveButtonDown ? 1 : 0,
                    moveComponent.TriggerValue);
            }
            else
            {
                display = string.Format("PS Move {0} - NOT CONNECTED", moveComponent.PSMoveID);
            }

            GUI.Label(new Rect(10, 10 + moveComponent.PSMoveID * 20, 500, 100), display);
        }
    }
}
