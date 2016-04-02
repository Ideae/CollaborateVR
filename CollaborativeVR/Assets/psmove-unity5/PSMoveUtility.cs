using UnityEngine;
using System.Collections;

public class PSMoveUtility : MonoBehaviour 
{
    // Conversion between centimeters and meters (Unity Units)
    public static float CMToMeters = 1.0f / 100.0f;
    public static float MetersToCM = 100.0f;

    public static Quaternion PSMoveQuatToUnityQuat(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, q.z, q.w);
    }

    public static Vector3 PSMoveCSToUnityCSPosition(Vector3 p)
    {
        // Convert from OpenGL/PSMove Right Handed coordinate system to Unity Left Handed coordinate system.
        // PSMove Coordinate System -> Unity Coordinate system ==> (x, y, z) -> (-x, y, z)
        // PSMove also specifies points in centimenters, while Unity specifies them in meters
        return new Vector3(-p.x * CMToMeters, p.y * CMToMeters, p.z * CMToMeters);
    }

    public static void ComputeTrackingToWorldTransforms(
        Transform parentGameObjectTransform,
        ref Matrix4x4 TrackingSpaceToWorldSpacePosition,
        ref Quaternion OrientationTransform)
    {
        float TrackingCameraNearPlane = 0.0f;
        float TrackingCameraFarPlane = 0.0f;
        float TrackingCameraHHalfRadians = 0.0f;
        float TrackingCameraVHalfRadians = 0.0f;

        ComputeTrackingToWorldTransformsAndFrustum(
            parentGameObjectTransform,
            ref TrackingSpaceToWorldSpacePosition, 
            ref OrientationTransform, 
            ref TrackingCameraNearPlane,
            ref TrackingCameraFarPlane,
            ref TrackingCameraHHalfRadians,
            ref TrackingCameraVHalfRadians);
    }

    public static void ComputeTrackingToWorldTransformsAndFrustum(
        Transform parentGameObjectTransform,
        ref Matrix4x4 TrackingSpaceToWorldSpacePosition, 
        ref Quaternion OrientationTransform,
        ref float TrackingCameraNearPlane, 
        ref float TrackingCameraFarPlane, 
        ref float TrackingCameraHHalfRadians,
        ref float TrackingCameraVHalfRadians)
    {
        // Get the world game camera transform for the player
        Quaternion ParentGameObjectOrientation = 
            (parentGameObjectTransform != null) ? parentGameObjectTransform.rotation : Quaternion.identity;
        Vector3 ParentGameObjectLocation = 
            (parentGameObjectTransform != null) ? parentGameObjectTransform.position : Vector3.zero;

        if (OVRManager.tracker != null && OVRManager.tracker.isPresent && OVRManager.tracker.isEnabled)
        {
            Vector3 TrackingCameraOrigin = Vector3.zero;
            Quaternion TrackingCameraOrientation = Quaternion.identity;
            float TrackingCameraHFOVDegrees = 0.0f;
            float TrackingCameraVFOVDegrees = 0.0f;

            // Get the camera pose in player reference frame, UE4 CS (LHS), Unreal Units
            GetPositionalTrackingCameraProperties(
                    ref TrackingCameraOrigin, ref TrackingCameraOrientation,
                    ref TrackingCameraHFOVDegrees, ref TrackingCameraVFOVDegrees,
                    ref TrackingCameraNearPlane, ref TrackingCameraFarPlane);

            TrackingCameraHHalfRadians = Mathf.Deg2Rad * TrackingCameraHFOVDegrees / 2.0f;
            TrackingCameraVHalfRadians = Mathf.Deg2Rad * TrackingCameraVFOVDegrees / 2.0f;

            // Apply the parent game object orientation THEN apply tracking camera orientation
            Quaternion TrackingCameraToGameRotation = ParentGameObjectOrientation * TrackingCameraOrientation;

            // Compute the tracking camera location in world space
            Vector3 TrackingCameraWorldSpaceOrigin =
                ParentGameObjectOrientation * TrackingCameraOrigin + ParentGameObjectLocation;

            // Compute the Transform to go from Tracking Camera Space to World Space
            TrackingSpaceToWorldSpacePosition =
                Matrix4x4.TRS(TrackingCameraWorldSpaceOrigin, TrackingCameraToGameRotation, Vector3.one);
        }
        else
        {
            // DK2 Camera Frustum properties
            const float k_default_tracking_hfov_degrees = 74.0f; // degrees
            const float k_default_tracking_vfov_degrees = 54.0f;  // degrees
            const float k_default_tracking_distance = 1.5f; // meters
            const float k_default_tracking_near_plane_distance = 0.4f; // meters
            const float k_default_tracking_far_plane_distance = 2.5f; // meters

            // Pretend that the tracking camera is directly in front of the game camera
            const float FakeTrackingCameraOffset = k_default_tracking_distance;
            Vector3 FakeTrackingCameraWorldSpaceOrigin =
                ParentGameObjectLocation + (ParentGameObjectOrientation * Vector3.forward) * FakeTrackingCameraOffset;

            // Get tracking frustum properties from defaults
            TrackingCameraHHalfRadians = Mathf.Deg2Rad * k_default_tracking_hfov_degrees / 2.0f;
            TrackingCameraVHalfRadians = Mathf.Deg2Rad * k_default_tracking_vfov_degrees / 2.0f;
            TrackingCameraNearPlane = k_default_tracking_near_plane_distance;
            TrackingCameraFarPlane = k_default_tracking_far_plane_distance;

            // Compute the Transform to go from faux tracking camera Space to World Space
            TrackingSpaceToWorldSpacePosition =
                // Put in the orientation of the game camera
                Matrix4x4.TRS(FakeTrackingCameraWorldSpaceOrigin, ParentGameObjectOrientation, Vector3.one);
        }

        // Transform the orientation of the controller from world space to camera space
        OrientationTransform = ParentGameObjectOrientation;
    }

    public static void GetPositionalTrackingCameraProperties(
        ref Vector3 position,
        ref Quaternion rotation,
        ref float cameraHFov,
        ref float cameraVFov,
        ref float cameraNearZ,
        ref float cameraFarZ)
    {
        OVRPose ss = OVRManager.tracker.GetPose(0.0f);

        rotation = new Quaternion(ss.orientation.x,
                                  ss.orientation.y,
                                  ss.orientation.z,
                                  ss.orientation.w);

        position = new Vector3(ss.position.x, // meters
                               ss.position.y,
                               ss.position.z);

        OVRTracker.Frustum ff = OVRManager.tracker.frustum;

        cameraHFov = ff.fov.x; // degrees
        cameraVFov = ff.fov.y; // degrees
        cameraNearZ = ff.nearZ; // meters
        cameraFarZ = ff.farZ; // meters
    }

    // Debug Rendering
    public static void DebugDrawHMDFrustum(Transform parentGameObjectTransform)
    {
        Matrix4x4 TrackingToWorldTransform = Matrix4x4.identity;
        Quaternion OrientationTransform= Quaternion.identity;
        float TrackingCameraNearPlane= 0.0f;
        float TrackingCameraFarPlane= 0.0f;
        float TrackingCameraHHalfRadians= 0.0f;
        float TrackingCameraVHalfRadians= 0.0f;

        ComputeTrackingToWorldTransformsAndFrustum(
            parentGameObjectTransform,
            ref TrackingToWorldTransform,
            ref OrientationTransform,
            ref TrackingCameraNearPlane,
            ref TrackingCameraFarPlane,
            ref TrackingCameraHHalfRadians,
            ref TrackingCameraVHalfRadians);

        float HorizontalRatio = Mathf.Tan(TrackingCameraHHalfRadians);
        float VerticalRatio = Mathf.Tan(TrackingCameraVHalfRadians);

        float HalfNearWidth = TrackingCameraNearPlane * HorizontalRatio;
        float HalfNearHeight = TrackingCameraNearPlane * VerticalRatio;

        float HalfFarWidth = TrackingCameraFarPlane * HorizontalRatio;
        float HalfFarHeight = TrackingCameraFarPlane * VerticalRatio;

        Vector3 Origin = TrackingToWorldTransform.GetColumn(3);
        Vector3 XAxis = TrackingToWorldTransform.GetColumn(0);
        Vector3 YAxis = TrackingToWorldTransform.GetColumn(1);
        Vector3 ZAxis = TrackingToWorldTransform.GetColumn(2);

        Vector3 NearV0 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(HalfNearWidth, HalfNearHeight, TrackingCameraNearPlane));
        Vector3 NearV1 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(-HalfNearWidth, HalfNearHeight, TrackingCameraNearPlane));
        Vector3 NearV2 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(-HalfNearWidth, -HalfNearHeight, TrackingCameraNearPlane));
        Vector3 NearV3 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(HalfNearWidth, -HalfNearHeight, TrackingCameraNearPlane));

        Vector3 FarV0 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(HalfFarWidth, HalfFarHeight, TrackingCameraFarPlane));
        Vector3 FarV1 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(-HalfFarWidth, HalfFarHeight, TrackingCameraFarPlane));
        Vector3 FarV2 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(-HalfFarWidth, -HalfFarHeight, TrackingCameraFarPlane));
        Vector3 FarV3 = TrackingToWorldTransform.MultiplyPoint3x4(new Vector3(HalfFarWidth, -HalfFarHeight, TrackingCameraFarPlane));

        Debug.DrawLine(Origin, FarV0, Color.yellow);
        Debug.DrawLine(Origin, FarV1, Color.yellow);
        Debug.DrawLine(Origin, FarV2, Color.yellow);
        Debug.DrawLine(Origin, FarV3, Color.yellow);

        Debug.DrawLine(NearV0, NearV1, Color.yellow);
        Debug.DrawLine(NearV1, NearV2, Color.yellow);
        Debug.DrawLine(NearV2, NearV3, Color.yellow);
        Debug.DrawLine(NearV3, NearV0, Color.yellow);

        Debug.DrawLine(FarV0, FarV1, Color.yellow);
        Debug.DrawLine(FarV1, FarV2, Color.yellow);
        Debug.DrawLine(FarV2, FarV3, Color.yellow);
        Debug.DrawLine(FarV3, FarV0, Color.yellow);

        Debug.DrawLine(Origin, Origin + XAxis, Color.red);
        Debug.DrawLine(Origin, Origin + YAxis, Color.green);
        Debug.DrawLine(Origin, Origin + ZAxis, Color.blue);
    }
}
