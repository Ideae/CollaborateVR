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
using System.Diagnostics;
using System;

public class PSMoveHitchWatchdog : IDisposable
{
    public static bool EmitHitchLogging = false;

    public static long MILLISECONDS_PER_SECOND = 1000;
    public static long MICROSECONDS_PER_MILLISECOND = 1000;
    public static long MICROSECONDS_PER_SECOND = MICROSECONDS_PER_MILLISECOND * MILLISECONDS_PER_SECOND;

    public PSMoveHitchWatchdog(string blockName, float microseconds_timeout)
    {
        this.blockName = blockName;
        this.timeout = microseconds_timeout;
        this.stopWatch = new Stopwatch();
        this.stopWatch.Start(); 
    }

    public virtual void Dispose()
    {
        this.stopWatch.Stop();
        float TimeDeltaInMicroseconds = (float)((stopWatch.ElapsedTicks * MICROSECONDS_PER_SECOND) / Stopwatch.Frequency);

        if (TimeDeltaInMicroseconds > timeout)
        {
            if (PSMoveHitchWatchdog.EmitHitchLogging)
            {
                UnityEngine.Debug.LogWarning(
                    string.Format("PSMoveHitchWatchdog: HITCH DETECTED({0})! Section took {1}us (>={2}us)",
                    blockName,
                    TimeDeltaInMicroseconds,
                    timeout));
            }
        }
    }

    private string blockName;
    private float timeout;
    private Stopwatch stopWatch;
};