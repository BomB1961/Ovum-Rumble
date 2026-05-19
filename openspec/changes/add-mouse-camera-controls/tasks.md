## 1. CameraController Script

- [x] 1.1 Create `Assets/_Project/Scripts/Presentation/CameraController.cs` with MonoBehaviour, serialized fields, and `Camera.main` fallback in Awake
- [x] 1.2 Initialize pivot point and yaw/pitch from camera's current Transform on Start

## 2. Orbit (Right-click + Drag)

- [x] 2.1 Implement right-click detection using Input + InputSystem dual pattern (same style as FlickInputController)
- [x] 2.2 Implement spherical coordinate orbit: convert mouse delta to yaw/pitch changes, clamp pitch to [-89, 89]
- [x] 2.3 Apply orbit in LateUpdate: compute camera position from yaw/pitch/distance, set LookAt(pivot)

## 3. Pan (Middle-click + Drag)

- [x] 3.1 Implement middle-click detection with same dual Input pattern
- [x] 3.2 Implement Gimbal Pan: on middle-click down, raycast to Y=0 plane to store anchor point
- [x] 3.3 On drag, raycast again to Y=0 plane, compute delta, move pivot (and camera follows via LateUpdate orbit logic)

## 4. Dolly (Scroll Wheel)

- [x] 4.1 Implement scroll wheel detection with dual Input pattern
- [x] 4.2 Apply scroll delta to distance with sensitivity, clamp to [minDistance, maxDistance]

## 5. Scene Wiring

- [x] 5.1 Attach CameraController to Main Camera in `A_EggPhysics_Prototype.unity`
- [x] 5.2 Attach CameraController to Main Camera in `B_EggPhysics_Prototype.unity`
- [x] 5.3 Verify `inputCamera` field references are set in both scenes
