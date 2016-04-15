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

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define LOAD_DLL_MANUALLY
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public enum PSMoveTrackingColor
{
    magenta = 0,
    cyan = 1,
    yellow = 2,
    red = 3,
    green = 4
};

public class PSMoveManager : MonoBehaviour 
{
    public bool EmitHitchLogging= false;
    public bool UseManualExposure= false;
    public PSMove_PositionFilter_Type FilterType = PSMove_PositionFilter_Type.PositionFilter_LowPass;
    public PSMoveTrackingColor InitialTrackingColor = PSMoveTrackingColor.magenta;
    public Vector3 PSMoveOffset = new Vector3();
    [Range(0.0f, 1.0f)]
    public float ManualExposureValue = 0.04f;
    public bool TrackerEnabled = true;

    private static PSMoveManager ManagerInstance;

    // Public API
    public static PSMoveManager GetManagerInstance()
    {
        return ManagerInstance;
    }

    public PSMoveDataContext AcquirePSMove(int PSMoveID)
    {
        return PSMoveWorker.GetWorkerInstance().AcquirePSMove(PSMoveID);
    }

    public void ReleasePSMove(PSMoveDataContext DataContext)
    {
        PSMoveWorker.GetWorkerInstance().ReleasePSMove(DataContext);
    }

    // Unity Callbacks
    public void Awake()
    {
        if (ManagerInstance == null)
        {
            ManagerInstance = this;
            PSMoveHitchWatchdog.EmitHitchLogging = this.EmitHitchLogging;
            PSMoveWorker.GetWorkerInstance().OnGameStarted(
                new PSMoveWorkerSettings()
                {
                    bUseManualExposure = this.UseManualExposure,
                    ManualExposureValue = this.ManualExposureValue,
                    InitialTrackingColor = this.InitialTrackingColor,
                    PSMoveOffset = this.PSMoveOffset,
                    FilterType = this.FilterType,
                    bTrackerEnabled = this.TrackerEnabled,
                    ApplicationDataPath = Application.dataPath
                });
        }
    }

    public void OnApplicationQuit()
    {
        if (ManagerInstance != null)
        {
            PSMoveWorker.GetWorkerInstance().OnGameEnded();

            ManagerInstance = null;
        }
    }
}

// -- private definitions ----

// TrackingContext contains references to the psmoveapi tracker and fusion objects, the controllers,
// and references to the shared (controller independent) data and the controller(s) data.
class WorkerContext
{
    public static int MAX_TRACKER_COUNT = 8;
    public static float CONTROLLER_COUNT_POLL_INTERVAL = 1000.0f; // milliseconds

    public PSMoveWorkerSettings WorkerSettings;
    public PSMoveRawControllerData_TLS[] WorkerControllerDataArray;

    public IntPtr[] PSMoves; // Array of PSMove*
    public int PSMoveCount;
    public Stopwatch moveCountCheckTimer;

    public IntPtr[] PSMoveTrackers; // PSMoveTracker*
    public IntPtr[] PSMoveFusions; // PSMoveFusion*
    public int TrackerCount;
    public int TrackerWidth;
    public int TrackerHeight;

    public IntPtr PSMovePositionFilter; // PSMovePositionFilter*

    // Constructor
    public WorkerContext(PSMoveRawControllerData_TLS[] controllerDataArray, PSMoveWorkerSettings settings)
    {
        WorkerSettings= settings;
        WorkerControllerDataArray= controllerDataArray;
        
        // This timestamp is used to throttle how frequently we poll for controller count changes
        moveCountCheckTimer = new Stopwatch();

        Reset();
    }
    
    public void Reset()
    {
        PSMoves = Enumerable.Repeat(IntPtr.Zero, PSMoveWorker.MAX_CONTROLLERS).ToArray();
        PSMoveCount = 0;
        moveCountCheckTimer.Reset();
        moveCountCheckTimer.Start();

        PSMoveTrackers = new IntPtr[MAX_TRACKER_COUNT];
        PSMoveFusions = new IntPtr[MAX_TRACKER_COUNT];
        for (int tracker_index = 0; tracker_index < MAX_TRACKER_COUNT; ++tracker_index)
        {
            PSMoveTrackers[tracker_index] = IntPtr.Zero;
            PSMoveFusions[tracker_index] = IntPtr.Zero;
        }
        TrackerCount = 0;
        TrackerWidth = 0;
        TrackerHeight = 0;

        PSMovePositionFilter = IntPtr.Zero;
    }
};

class PSMoveWorkerSettings
{
    public bool bUseManualExposure;
    public float ManualExposureValue;
    public PSMoveTrackingColor InitialTrackingColor;
    public PSMove_PositionFilter_Type FilterType;
    public Vector3 PSMoveOffset;
    public bool bTrackerEnabled;
    public string ApplicationDataPath;

    public PSMoveWorkerSettings()
    {
        Clear();
    }

    public void Clear()
    {
        bUseManualExposure= false;
        ManualExposureValue= 0.0f;
        InitialTrackingColor= PSMoveTrackingColor.cyan;
        FilterType = PSMove_PositionFilter_Type.PositionFilter_LowPass;
        PSMoveOffset= Vector3.zero;
        bTrackerEnabled = true;
        ApplicationDataPath = "";
    }
}

class PSMoveWorker
{
    public static int MAX_CONTROLLERS = 5; // 5 tracking colors available: magenta, cyan, yellow, red, blue

    private static PSMoveWorker WorkerInstance;

    public static PSMoveWorker GetWorkerInstance()
    {
        if (WorkerInstance == null)
        {
            WorkerInstance = new PSMoveWorker();
        }

        return WorkerInstance; 
    }

    private PSMoveWorker()
    {
        WorkerSettings = new PSMoveWorkerSettings();

        HaltThreadSignal = new ManualResetEvent(false);
        ThreadExitedSignal = new ManualResetEvent(false);
        WorkerThread = new Thread(() => { this.ThreadProc(); });
        WorkerThread.Priority = System.Threading.ThreadPriority.AboveNormal;

        WorkerControllerDataArray_Concurrent = new PSMoveRawControllerData_Concurrent[MAX_CONTROLLERS];
        WorkerControllerDataArray = new PSMoveRawControllerData_TLS[MAX_CONTROLLERS];
        for (int i = 0; i < WorkerControllerDataArray_Concurrent.Length; i++)
        {
            WorkerControllerDataArray_Concurrent[i] = new PSMoveRawControllerData_Concurrent();
            WorkerControllerDataArray[i] = new PSMoveRawControllerData_TLS(WorkerControllerDataArray_Concurrent[i]);
        }

        psmoveapiSharedLibHandle = IntPtr.Zero;
        psmoveapiTrackerSharedLibHandle = IntPtr.Zero;
    }

    public void OnGameStarted(PSMoveWorkerSettings workerSettings)
    {
        // Start the worker thread in case it's not already running
        WorkerSetup(workerSettings);
    }

    public void OnGameEnded()
    {
        WorkerTeardown();
        WorkerInstance = null;
    }

    // Tell the PSMove Worker that we want to start listening to this controller.
    public PSMoveDataContext AcquirePSMove(int PSMoveID)
    {
        PSMoveDataContext DataContext= null;

        if (PSMoveID >= 0 && PSMoveID < MAX_CONTROLLERS)
        {
            // Bind the data context to the concurrent data for the requested controller
            // This doesn't mean  that the controller is active, just that a component
            // is now watching this block of data.
            // Also this is thread safe because were not actually looking at the concurrent data
            // at this point, just assigning a pointer to the concurrent data.
            DataContext= new PSMoveDataContext(
                PSMoveID,
                WorkerInstance.WorkerControllerDataArray_Concurrent[PSMoveID]);
        }

        return DataContext;
    }

    public void ReleasePSMove(PSMoveDataContext DataContext)
    {
        if (DataContext.PSMoveID != -1)
        {
            DataContext.Clear();
        }
    }
    
    private void WorkerSetup(PSMoveWorkerSettings workerSettings)
    {
        #if LOAD_DLL_MANUALLY
        if (psmoveapiSharedLibHandle == IntPtr.Zero)
        {
            #if UNITY_EDITOR_WIN
            if (IntPtr.Size == 8)
            {
                psmoveapiSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/x86_64/psmoveapi.dll");
            }
            else
            {
                psmoveapiSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/x86/psmoveapi.dll");
            }
            #elif UNITY_STANDALONE_WIN
            psmoveapiSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/psmoveapi.dll");
            #endif
        }

        if (psmoveapiTrackerSharedLibHandle == IntPtr.Zero)
        {
            #if UNITY_EDITOR_WIN
            if (IntPtr.Size == 8)
            {
                psmoveapiTrackerSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/x86_64/psmoveapi_tracker.dll");
            }
            else
            {
                psmoveapiTrackerSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/x86/psmoveapi_tracker.dll");
            }
            #elif UNITY_STANDALONE_WIN
            psmoveapiSharedLibHandle = LoadLib(Application.dataPath + "/Plugins/psmoveapi_tracker.dll");
            #endif
        }
        #endif // LOAD_DLL_MANUALLY

        if (!WorkerThread.IsAlive)
        {
            WorkerSettings= workerSettings;
            WorkerThread.Start();
        }
    }

    private void WorkerTeardown()
    {
        if (WorkerThread.IsAlive)
        {
            // Signal the thread to stop
            HaltThreadSignal.Set();

            // Wait ten seconds for the thread to finish
            ThreadExitedSignal.WaitOne(10 * 1000);

            // Reset the stop and exited flags so that the thread can be restarted
            HaltThreadSignal.Reset();
            ThreadExitedSignal.Reset();
        }

        //Free any manually loaded DLLs
        if (psmoveapiTrackerSharedLibHandle != IntPtr.Zero)
        {
            FreeLibrary(psmoveapiTrackerSharedLibHandle);
            psmoveapiTrackerSharedLibHandle = IntPtr.Zero;
        }

        if (psmoveapiSharedLibHandle != IntPtr.Zero)
        {
            FreeLibrary(psmoveapiSharedLibHandle);
            psmoveapiSharedLibHandle = IntPtr.Zero;
        }
    }

    private void ThreadProc()
    {
        try
        {
            bool receivedStopSignal = false;

            ThreadSetup();
    
            //Initial wait before starting.
            Thread.Sleep(30);

            while (!receivedStopSignal)
            {
                ThreadUpdate();

                // See if the main thread signaled us to stop
                if (HaltThreadSignal.WaitOne(0))
                {
                    receivedStopSignal = true;
                }

                if (!receivedStopSignal)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }

            ThreadTeardown();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(string.Format("PSMoveWorker: WorkerThread crashed: {0}", e.Message));
            UnityEngine.Debug.LogException(e);
        }
        finally
        {
            ThreadExitedSignal.Set();
        }
    }

    public void ThreadSetup()
    {
        // Maintains the following psmove state on the stack
        // * psmove tracking state
        // * psmove fusion state
        // * psmove controller state
        // Tracking state is only initialized when we have a non-zero number of tracking contexts
        Context = new WorkerContext(WorkerControllerDataArray, WorkerSettings);

        if (PSMoveAPI.psmove_init(PSMoveAPI.PSMove_Version.PSMOVE_CURRENT_VERSION) == PSMove_Bool.PSMove_False)
        {
            throw new Exception("PS Move API init failed (wrong version?)");
        }
    }

    public void ThreadUpdate()
    {
        using (new PSMoveHitchWatchdog("PSMoveWorker_ThreadUpdate", 34 * PSMoveHitchWatchdog.MICROSECONDS_PER_MILLISECOND))
        {
            // Setup or teardown tracking based on the updated tracking state
            if (WorkerSettings.bTrackerEnabled && !WorkerContextIsTrackingSetup(Context))
            {
                WorkerContextSetupTracking(WorkerSettings, Context);
            }
            else if (!WorkerSettings.bTrackerEnabled && WorkerContextIsTrackingSetup(Context))
            {
                WorkerContextTeardownTracking(Context);
            }

            // Setup or tear down controller connections based on the number of active controllers
            WorkerContextUpdateControllerConnections(Context);

            // Renew the image on camera, if tracking is enabled
            if (WorkerContextIsTrackingSetup(Context))
            {
                using (new PSMoveHitchWatchdog("PSMoveWorker_UpdateImage", 33 * PSMoveHitchWatchdog.MICROSECONDS_PER_MILLISECOND))
                {
                    for (int tracker_index = 0; tracker_index < Context.TrackerCount; ++tracker_index )
                    {
                        PSMoveAPI.psmove_tracker_update_image(Context.PSMoveTrackers[tracker_index]); // Sometimes libusb crashes here.
                    }
                }
            }

            // Update the raw positions on the local controller data
            if (WorkerContextIsTrackingSetup(Context))
            {
                for (int psmove_id = 0; psmove_id < Context.PSMoveCount; psmove_id++)
                {
                    PSMoveRawControllerData_TLS localControllerData = WorkerControllerDataArray[psmove_id];

                    if (WorkerSettings.bTrackerEnabled)
                    {
                        ControllerUpdatePositions(
                            WorkerSettings,
                            Context.PSMoveTrackers,
                            Context.PSMoveFusions,
                            Context.TrackerCount,
                            Context.PSMovePositionFilter,
                            Context.PSMoves[psmove_id],
                            localControllerData);
                    }
                    else
                    {
                        localControllerData.IsSeenByTracker = false;
                    }
                }
            }

            // Do bluetooth IO: Orientation, Buttons, Rumble
            for (int psmove_id = 0; psmove_id < Context.PSMoveCount; psmove_id++)
            {
                //TODO: Is it necessary to keep polling until no frames are left?
                while (PSMoveAPI.psmove_poll(Context.PSMoves[psmove_id]) > 0)
                {
                    PSMoveRawControllerData_TLS localControllerData = WorkerControllerDataArray[psmove_id];

                    // Update the controller status (via bluetooth)
                    PSMoveAPI.psmove_poll(Context.PSMoves[psmove_id]);  // Necessary to poll yet again?

                    // Store the controller orientation
                    ControllerUpdateOrientations(Context.PSMoves[psmove_id], localControllerData);

                    // Store the button state
                    ControllerUpdateButtonState(Context.PSMoves[psmove_id], localControllerData);

                    // Now read in requested changes from Component. e.g., RumbleRequest, CycleColourRequest
                    localControllerData.WorkerRead();

                    // Set the controller rumble (uint8; 0-255)
                    PSMoveAPI.psmove_set_rumble(Context.PSMoves[psmove_id], localControllerData.RumbleRequest);

                    // Push the updated rumble state to the controller
                    PSMoveAPI.psmove_update_leds(Context.PSMoves[psmove_id]);

                    if (localControllerData.CycleColourRequest)
                    {
                        if (WorkerSettings.bTrackerEnabled)
                        {
                            UnityEngine.Debug.Log("PSMoveWorker:: CYCLE COLOUR");

                            // Attempt to cycle the color for each tracker and re-acquire tracking
                            int happyTrackerCount = 0;
                            for (int tracker_index = 0; tracker_index < Context.TrackerCount; ++tracker_index)
                            {
                                PSMoveAPI.psmove_tracker_cycle_color(
                                    Context.PSMoveTrackers[tracker_index], Context.PSMoves[psmove_id]);

                                PSMoveTracker_Status tracker_status =
                                    PSMoveAPI.psmove_tracker_get_status(
                                        Context.PSMoveTrackers[tracker_index],
                                        Context.PSMoves[psmove_id]);

                                if (tracker_status == PSMoveTracker_Status.Tracker_CALIBRATED ||
                                    tracker_status == PSMoveTracker_Status.Tracker_TRACKING)
                                {
                                    ++happyTrackerCount;
                                }
                            }

                            // If not all trackers re-acquired,
                            // mark the controller as no having tracking enabled,
                            // and let WorkerContextUpdateControllerConnections() try 
                            // and reacquire next update.
                            if (happyTrackerCount < Context.TrackerCount)
                            {
                                Context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled = false;
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("PSMoveWorker:: CYCLE COLOUR ignored! Tracking is disabled!");
                        }

                        localControllerData.CycleColourRequest = false;
                    }

                    // Publish Position, Orientation, and Button state to the concurrent data
                    // This also publishes updated CycleColourRequest.
                    localControllerData.WorkerPost();
                }
            }
        }
    }

    public void ThreadTeardown()
    {
        WorkerContextTeardown(Context);
        Context = null;
    }

    #region Private Tracking Context Methods
    private static bool WorkerContextSetupTracking(
        PSMoveWorkerSettings WorkerSettings,
        WorkerContext context)
    {
        bool success = true;

        // Clear out the tracking state
        // Reset the shared worker data
        context.Reset();

        UnityEngine.Debug.Log("Setting up PSMove Tracking Context");

        // Initialize and configure the psmove_tracker.
        {
            PSMoveAPI.PSMoveTrackerSettings settings = new PSMoveAPI.PSMoveTrackerSettings();
            PSMoveAPI.psmove_tracker_settings_set_default(ref settings);
            
            settings.color_mapping_max_age = 0; // Don't used cached color mapping file

            if (WorkerSettings.bUseManualExposure)
            {
                settings.exposure_mode = PSMoveTracker_Exposure.Exposure_MANUAL;
                settings.camera_exposure = 
                    (int)(Math.Max(Math.Min(WorkerSettings.ManualExposureValue, 1.0f), 0.0f) * 65535.0f);
            }
            else
            {
                settings.exposure_mode = PSMoveTracker_Exposure.Exposure_LOW;
            }

            settings.use_fitEllipse = 1;
            settings.filter_do_2d_r = 0;
            settings.filter_do_2d_xy = 0;
            settings.camera_mirror = PSMove_Bool.PSMove_True;
            settings.color_list_start_ind = (int)WorkerSettings.InitialTrackingColor;

            context.TrackerCount = 0;
            for (int tracker_index = 0; tracker_index < WorkerContext.MAX_TRACKER_COUNT; ++tracker_index)
            {
                context.PSMoveTrackers[tracker_index] = 
                    PSMoveAPI.psmove_tracker_new_with_camera_and_settings(tracker_index, ref settings);

                if (context.PSMoveTrackers[tracker_index] != IntPtr.Zero)
                {
                    UnityEngine.Debug.Log(string.Format("PSMove tracker({0}) initialized.", tracker_index));
                    ++context.TrackerCount;

                    PSMoveAPI.psmove_tracker_get_size(
                        context.PSMoveTrackers[tracker_index], 
                        ref context.TrackerWidth, ref context.TrackerHeight);
                    UnityEngine.Debug.Log(string.Format("Camera Dimensions: {0} x {1}", context.TrackerWidth, context.TrackerHeight));
                }
                else
                {
                    PSMoveTracker_ErrorCode errorCode = PSMoveAPI.psmove_tracker_get_last_error();

                    UnityEngine.Debug.Log(string.Format("PSMove tracker({0}) not available: {1}", 
                        tracker_index, errorCode.ToString()));
                    break;
                }
            }

            if (context.TrackerCount <= 0)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to open any trackers"));
                success = false;
            }
        }

        // Initialize fusion API if the tracker started
        if (success)
        {
            for (int tracker_index = 0; tracker_index < context.TrackerCount; ++tracker_index)
            {
                context.PSMoveFusions[tracker_index] = 
                    PSMoveAPI.psmove_fusion_new(context.PSMoveTrackers[tracker_index], 1.0f, 1000.0f);

                if (context.PSMoveFusions[tracker_index] != IntPtr.Zero)
                {
                    UnityEngine.Debug.Log(string.Format("PSMove fusion({0}) initialized.", tracker_index));
                }
                else
                {
                    UnityEngine.Debug.LogError(string.Format("PSMove fusion({0}) failed to initialize.", tracker_index));
                    success = false;
                    break;
                }
            }
        }

        // Initialize a position filter to smooth out the tracking data
        if (success)
        {
            context.PSMovePositionFilter = PSMoveAPI.psmove_position_filter_new();

            if (context.PSMovePositionFilter != IntPtr.Zero)
            {
                UnityEngine.Debug.Log("PSMove position filter initialized.");

                PSMoveAPI.PSMove_3AxisVector initial_position = new PSMoveAPI.PSMove_3AxisVector()
                {
                    x = 0.0f,
                    y = 0.0f,
                    z = 0.0f,
                };
                PSMoveAPI.PSMovePositionFilterSettings filter_settings = new PSMoveAPI.PSMovePositionFilterSettings();
                PSMoveAPI.psmove_position_filter_get_default_settings(ref filter_settings);
                filter_settings.filter_type = WorkerSettings.FilterType;
                PSMoveAPI.psmove_position_filter_init(ref filter_settings, ref initial_position, context.PSMovePositionFilter);
            }
            else
            {
                UnityEngine.Debug.LogError(string.Format("Failed to allocate PSMove Position Filter"));
                success = false;
            }
        }

        if (!success)
        {
            WorkerContextTeardownTracking(context);
        }

        return success;
    }

    private static bool WorkerContextIsTrackingSetup(WorkerContext context)
    {
        return context.TrackerCount > 0;
    }

    private static bool WorkerContextUpdateControllerConnections(WorkerContext context)
    {
        bool controllerCountChanged = false;
    
        if (context.moveCountCheckTimer.ElapsedMilliseconds >= WorkerContext.CONTROLLER_COUNT_POLL_INTERVAL)
        {
            // Update the number
            int newcount = PSMoveAPI.psmove_count_connected();
        
            if (context.PSMoveCount != newcount)
            {
                UnityEngine.Debug.Log(string.Format("PSMove Controllers count changed: {0} -> {1}.", context.PSMoveCount, newcount));
            
                context.PSMoveCount = newcount;
                controllerCountChanged = true;
            }
        
            // Refresh the connection and tracking state of every controller entry
            for (int psmove_id = 0; psmove_id < context.PSMoves.Length; psmove_id++)
            {
                if (psmove_id < context.PSMoveCount)
                {
                    if (context.PSMoves[psmove_id] == IntPtr.Zero)
                    {
                        // The controller should be connected
                        context.PSMoves[psmove_id] = PSMoveAPI.psmove_connect_by_id(psmove_id);

                        if (context.PSMoves[psmove_id] != IntPtr.Zero)
                        {
                            PSMoveAPI.psmove_enable_orientation(context.PSMoves[psmove_id], PSMove_Bool.PSMove_True);
                            System.Diagnostics.Debug.Assert(PSMoveAPI.psmove_has_orientation(context.PSMoves[psmove_id]) == PSMove_Bool.PSMove_True);

                            context.WorkerControllerDataArray[psmove_id].IsConnected = true;
                        }
                        else
                        {
                            context.WorkerControllerDataArray[psmove_id].IsConnected = false;
                            UnityEngine.Debug.LogError(string.Format("Failed to connect to PSMove controller {0}", psmove_id));
                        }
                    }

                    if (context.PSMoves[psmove_id] != IntPtr.Zero && 
                        context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled == false &&
                        context.WorkerSettings.bTrackerEnabled &&
                        WorkerContextIsTrackingSetup(context))
                    {
                        int happyTrackerCount = 0;

                        // Attempt to enable any trackers that haven't successfully calibrated the controller yet
                        for (int tracker_index = 0; tracker_index < context.TrackerCount; ++tracker_index)
                        {
                            PSMoveTracker_Status tracker_status=
                                PSMoveAPI.psmove_tracker_get_status(
                                    context.PSMoveTrackers[tracker_index],
                                    context.PSMoves[psmove_id]);

                            if (tracker_status == PSMoveTracker_Status.Tracker_CALIBRATED ||
                                tracker_status == PSMoveTracker_Status.Tracker_TRACKING)
                            {
                                ++happyTrackerCount;
                            }
                            else
                            {
                                // The controller is connected, but not tracking yet
                                // Enable tracking for this controller with next available color.
                                if (PSMoveAPI.psmove_tracker_enable(
                                        context.PSMoveTrackers[tracker_index],
                                        context.PSMoves[psmove_id]) == PSMoveTracker_Status.Tracker_CALIBRATED)
                                {
                                    ++happyTrackerCount;
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError(string.Format("Failed to enable tracking for PSMove controller {0} on tracker {1}", psmove_id, tracker_index));
                                }
                            }
                        }

                        if (happyTrackerCount >= context.TrackerCount)
                        {
                            context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled = true;
                        }
                    }
                }
                else
                {
                    // The controller should no longer be tracked
                    if (context.PSMoves[psmove_id] != IntPtr.Zero)
                    {
                        PSMoveAPI.psmove_disconnect(context.PSMoves[psmove_id]);
                        context.PSMoves[psmove_id] = IntPtr.Zero;
                        context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled = false;
                        context.WorkerControllerDataArray[psmove_id].IsConnected = false;
                    }
                }
            }
        
            // Remember the last time we polled the move count
            context.moveCountCheckTimer.Reset();
            context.moveCountCheckTimer.Start();
        }
    
        return controllerCountChanged;
    }

    private static void WorkerContextTeardownTracking(WorkerContext context)
    {
        // Disable tracking on all active controllers
        for (int psmove_id = 0; psmove_id < context.PSMoves.Length; psmove_id++)
        {
            if (context.PSMoves[psmove_id] != IntPtr.Zero &&
                context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled)
            {
                UnityEngine.Debug.Log(string.Format("Disabling tracking on PSMove controller {0}", psmove_id));
                context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled = false;
            }
        }

        for (int tracker_index = 0; tracker_index < WorkerContext.MAX_TRACKER_COUNT; ++tracker_index)
        {
            // Delete the tracking fusion state
            if (context.PSMoveFusions[tracker_index] != IntPtr.Zero)
            {
                UnityEngine.Debug.Log("PSMove fusion disposed");
                PSMoveAPI.psmove_fusion_free(context.PSMoveFusions[tracker_index]);
                context.PSMoveFusions[tracker_index] = IntPtr.Zero;
            }

            // Delete the tracker state
            if (context.PSMoveTrackers[tracker_index] != IntPtr.Zero)
            {
                UnityEngine.Debug.Log("PSMove tracker disposed");
                PSMoveAPI.psmove_tracker_free(context.PSMoveTrackers[tracker_index]);
                context.PSMoveTrackers[tracker_index] = IntPtr.Zero;
            }
        }
        context.TrackerCount = 0;

        // Delete the position filter
        if (context.PSMovePositionFilter != IntPtr.Zero)
        {
            PSMoveAPI.psmove_position_filter_free(context.PSMovePositionFilter);
            context.PSMovePositionFilter = IntPtr.Zero;
        }
    }

    private static void WorkerContextTeardown(WorkerContext context)
    {
        // Delete the controllers
        for (int psmove_id = 0; psmove_id < context.PSMoves.Length; psmove_id++)
        {
            if (context.PSMoves[psmove_id] != IntPtr.Zero)
            {
                UnityEngine.Debug.Log(string.Format("Disconnecting PSMove controller {0}", psmove_id));
                context.WorkerControllerDataArray[psmove_id].IsConnected = false;
                context.WorkerControllerDataArray[psmove_id].IsTrackingEnabled = false;
                PSMoveAPI.psmove_disconnect(context.PSMoves[psmove_id]);
                context.PSMoves[psmove_id] = IntPtr.Zero;
            }
        }

        // Delete the tracker
        WorkerContextTeardownTracking(context);

        context.Reset();
    }

    private static void ControllerUpdatePositions(
        PSMoveWorkerSettings WorkerSettings,
        IntPtr[] psmove_trackers, // PSMoveTracker*
        IntPtr[] psmove_fusions, // PSMoveFusion*
        int tracker_count,
        IntPtr position_filter,
        IntPtr psmove, // PSMove*
        PSMoveRawControllerData_Base controllerData)
    {
        // Update the tracked position of the psmove for each tracker
        for (int tracker_index = 0; tracker_index < tracker_count; ++tracker_index)
        {
            PSMoveAPI.psmove_tracker_update(psmove_trackers[tracker_index], psmove);
        }

        // Compute the triangulated camera position
        PSMoveAPI.PSMove_3AxisVector measured_position = new PSMoveAPI.PSMove_3AxisVector();
        controllerData.IsSeenByTracker =
            PSMoveAPI.psmove_fusion_get_multicam_tracking_space_location(
                psmove_fusions, tracker_count, psmove,
                ref measured_position.x, ref measured_position.y, ref measured_position.z) == PSMove_Bool.PSMove_True;

        // Update the position of the controller
        if (controllerData.IsSeenByTracker)
        {
            // Update the filtered position
            PSMoveAPI.psmove_position_filter_update(
                ref measured_position,
                controllerData.IsSeenByTracker ? PSMove_Bool.PSMove_True : PSMove_Bool.PSMove_False,
                position_filter);

            // Get the filtered position
            PSMoveAPI.PSMove_3AxisVector filtered_position =
                PSMoveAPI.psmove_position_filter_get_position(position_filter);
        
            // [Store the controller position]
            // Remember the position the ps move controller in either its native space
            // or in a transformed space if a transform file existed.
            controllerData.PSMovePosition = 
                new Vector3(
                    filtered_position.x + WorkerSettings.PSMoveOffset.x,
                    filtered_position.y + WorkerSettings.PSMoveOffset.y,
                    filtered_position.z + WorkerSettings.PSMoveOffset.z);
        }
    }

    private static void ControllerUpdateOrientations(
        IntPtr psmove, // PSMove*
        PSMoveRawControllerData_Base controllerData)
    {
        float oriw = 1.0f, orix = 0.0f, oriy = 0.0f, oriz = 0.0f;

        // Get the controller orientation (uses IMU).
        PSMoveAPI.psmove_get_orientation(psmove, ref oriw, ref orix, ref oriy, ref oriz);

        //NOTE: This orientation is in the PSMoveApi coordinate system
        controllerData.PSMoveOrientation = new Quaternion(orix, oriy, oriz, oriw);
    }

    private static void ControllerUpdateButtonState(
        IntPtr psmove, // PSMove*
        PSMoveRawControllerData_Base controllerData)
    {
        // Get the controller button state
        controllerData.Buttons = PSMoveAPI.psmove_get_buttons(psmove);  // Bitwise; tells if each button is down.

        // Get the controller trigger value (uint8; 0-255)
        controllerData.TriggerValue = (byte)PSMoveAPI.psmove_get_trigger(psmove);
    }
    #endregion
  
    // Thread local version of the concurrent worker settings data
    public PSMoveWorkerSettings WorkerSettings;
   
    // Number of controllers currently active
    private int PSMoveCount;

    // Published worker data that shouldn't be touched directly.
    // Access through _TLS version of the structures.
    private PSMoveRawControllerData_Concurrent[] WorkerControllerDataArray_Concurrent;

    // Thread local version of the concurrent controller data
    private PSMoveRawControllerData_TLS[] WorkerControllerDataArray;

    // Maintains all of the tracking camera and controller state
    private WorkerContext Context;

    // Threading State
    private Thread WorkerThread;
    private ManualResetEvent HaltThreadSignal;
    private ManualResetEvent ThreadExitedSignal;

    // DLL Handles
    private IntPtr psmoveapiSharedLibHandle;
    private IntPtr psmoveapiTrackerSharedLibHandle;

#if LOAD_DLL_MANUALLY
    private IntPtr LoadLib(string path)
    {
        IntPtr ptr = LoadLibrary(path);
        if (ptr == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            UnityEngine.Debug.LogError(string.Format("Failed to load library {1} (ErrorCode: {0})", errorCode, path));
        }
        else
        {
            UnityEngine.Debug.Log("loaded lib " + path);
        }
        return ptr;
    }
#endif

    // Win32 API
#if LOAD_DLL_MANUALLY
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string libname);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool FreeLibrary(IntPtr hModule);
#endif
}