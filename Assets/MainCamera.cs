using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Strategy camera: WASD/arrows pan, Q/E orbit, mouse wheel zoom.
/// Uses unscaled time so it still works when the game is paused or sped up.
/// </summary>
public class MainCamera : MonoBehaviour
{
    public Vector3 focus = new Vector3(0f, 0f, 2f);
    public float yaw = 0f;
    public float pitch = 58f;
    public float distance = 88f;

    public float panSpeed = 35f;
    public float orbitSpeed = 90f;
    public float minDistance = 25f;
    public float maxDistance = 120f;
    public float mapLimit = 50f;

    void Start() => Apply();

    void LateUpdate()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        float dt = Time.unscaledDeltaTime;

        if (kb != null)
        {
            if (kb.qKey.isPressed) yaw += orbitSpeed * dt;
            if (kb.eKey.isPressed) yaw -= orbitSpeed * dt;

            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += forward;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move -= forward;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += right;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= right;
            focus += move * panSpeed * (distance / 80f) * dt;
            focus.x = Mathf.Clamp(focus.x, -mapLimit, mapLimit);
            focus.z = Mathf.Clamp(focus.z, -mapLimit, mapLimit);
        }

        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * distance * 0.1f, minDistance, maxDistance);
        }

        Apply();
    }

    private void Apply()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focus - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }
}
