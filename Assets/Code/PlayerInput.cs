using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Runtime.InteropServices;

[RequireComponent(typeof(Rigidbody))]
public class PlayerInput : MonoBehaviour
{
    [HideInInspector]
    public Rigidbody rb;

    static Vector3 levelBounds = new Vector3(250.0f, 250.0f, 250.0f);

    float yScale;
    public void FlipY(bool value)
    {
        yScale *= -1.0f;
    }

    Camera cam;

    Animator animator;
    List<Material> materials = new List<Material>();
    List<Color> matColours = new List<Color>();

    public enum CursorDir
    {
        Centre,
        Left,
        LeftUp,
        Up,
        RightUp,
        Right,
        RightDown,
        Down,
        LeftDown
    }

    CursorDir currentCursor = CursorDir.Centre;

    public UnityEvent onDamageEvent = new UnityEvent();
    public UnityEvent onGrabEvent = new UnityEvent();
    public UnityEvent onDepositEvent = new UnityEvent();

    public AudioSource windSource;

    public RawImage cursorMobile;

    [SerializeField]
    Texture2D cursorCentre, cursorLeft, cursorLeftUp, cursorUp, cursorRightUp, cursorRight,
        cursorRightDown, cursorDown, cursorLeftDown;

    [SerializeField]
    float minSpeed = 15.0f, maxPitchSpeed = 180.0f, maxRollSpeed = 360.0f, inputDeadzone = 0.2f, gravityScale = 4.0f,
        forwardDrag = 0.1f, sideDrag = 2.0f, adjustSpeed = 1.0f, stunLength = 1.0f, stunForce = 20.0f, maxRateOfDeflection = 1.0f;

    public void SetCursor(CursorDir dir)
    {
        if (dir != currentCursor)
        {
            Texture2D texture;
            switch (dir)
            {
                case CursorDir.Centre:
                    texture = cursorCentre;
                    break;

                case CursorDir.Left:
                    texture = cursorLeft;
                    break;

                case CursorDir.LeftUp:
                    texture = cursorLeftUp;
                    break;

                case CursorDir.Up:
                    texture = cursorUp;
                    break;

                case CursorDir.RightUp:
                    texture = cursorRightUp;
                    break;

                case CursorDir.Right:
                    texture = cursorRight;
                    break;

                case CursorDir.RightDown:
                    texture = cursorRightDown;
                    break;

                case CursorDir.Down:
                    texture = cursorDown;
                    break;

                case CursorDir.LeftDown:
                    texture = cursorLeftDown;
                    break;

                default:
                    texture = null;
                    break;
            }

            currentCursor = dir;

            if (controlMethod == ControlMethod.Mouse)
            {
                Vector2 hotSpot = 0.5f * new Vector2(texture.width, texture.height);
                Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
            }
            else
            {
                cursorMobile.texture = texture;
            }
        }
    }

    Vector2 SetCursor(Vector2 input)
    {
        if (controlMethod == ControlMethod.Tilt)
        {
            Vector2 screenPos = input;
            screenPos.y *= -yScale;
            screenPos *= 0.5f;
            screenPos *= Screen.height;
            screenPos.x *= cam.aspect;

            screenPos = Vector2.Lerp(cursorMobile.rectTransform.anchoredPosition, screenPos, 5.0f * Time.fixedDeltaTime);
            screenPos = RectTransformUtility.PixelAdjustPoint(screenPos, cursorMobile.rectTransform, cursorMobile.canvas);
            cursorMobile.rectTransform.anchoredPosition = screenPos;
        }

        input.y *= yScale;

        if (Mathf.Abs(input.x) < inputDeadzone)
        {
            input.x = 0.0f;
        }

        if (Mathf.Abs(input.y) < inputDeadzone)
        {
            input.y = 0.0f;
        }

        CursorDir dir;
        if (input.x > 0.0f)
        {
            if (input.y > 0.0f)
            {
                dir = CursorDir.RightDown;
            }
            else if (input.y == 0.0f)
            {
                dir = CursorDir.Right;
            }
            else
            {
                dir = CursorDir.RightUp;
            }
        }
        else if (input.x == 0.0f)
        {
            if (input.y > 0.0f)
            {
                dir = CursorDir.Down;
            }
            else if (input.y == 0.0f)
            {
                dir = CursorDir.Centre;
            }
            else
            {
                dir = CursorDir.Up;
            }
        }
        else
        {
            if (input.y > 0.0f)
            {
                dir = CursorDir.LeftDown;
            }
            else if (input.y == 0.0f)
            {
                dir = CursorDir.Left;
            }
            else
            {
                dir = CursorDir.LeftUp;
            }
        }

        SetCursor(dir);
        return input;
    }

    public enum ControlMethod
    {
        Mouse,
        Tilt
    }

    public ControlMethod controlMethod;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        onDamageEvent.AddListener(SetAnimDamageTrigger);
        onGrabEvent.AddListener(SetAnimFlapTrigger);
        onDepositEvent.AddListener(SetAnimFlapTrigger);

        if (windSource == null)
        {
            Debug.LogError("Must provide AudioSource to PlayerInput.");
        }

        if (cursorMobile == null)
        {
            Debug.LogError("Must provide cursor RawImage to PlayerInput.");
        }

        if (cursorCentre == null || cursorLeft == null || cursorLeftUp == null || cursorUp == null || cursorRightUp == null
            || cursorRight == null || cursorRightDown == null || cursorDown == null || cursorLeftDown == null)
        {
            Debug.LogError("Must provide cursor textures to PlayerInput.");
        }

        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        // Stores original colour of each material in skinned mesh.
        GetComponentInChildren<SkinnedMeshRenderer>().GetMaterials(materials);
        foreach(Material material in materials)
        {
            matColours.Add(material.color);
        }

        // Sets appropriate control method.
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            yScale = 1.0f;
            controlMethod = ControlMethod.Tilt;
        }
        else
        {
            yScale = -1.0f;
            controlMethod = ControlMethod.Mouse;
        }

        SetCursor(CursorDir.Centre);
    }

    void SetAnimDamageTrigger()
    {
        animator.SetBool("IsDamaged", true);
        SetAnimFlapBool(false);
    }

    void SetAnimDamageBool(bool value)
    {
        animator.SetBool("IsDamaged", value);
        if (value)
        {
            SetAnimFlapBool(false);
        }
    }

    bool GetAnimDamageBool()
    {
        return animator.GetBool("IsDamaged");
    }

    void SetAnimFlapTrigger()
    {
        animator.SetBool("IsFlapping", true);
        SetAnimDamageBool(false);
    }

    void SetAnimFlapBool(bool value)
    {
        animator.SetBool("IsFlapping", value);
        if (value)
        {
            SetAnimDamageBool(false);
        }
    }

    bool GetAnimFlapBool()
    {
        return animator.GetBool("IsFlapping");
    }

    float stunTimer = 0.0f, recoveryTimer = 0.0f;

    void ManageTimers()
    {
        if (stunTimer > 0.0f)
        {
            if (stunTimer <= 0.3f)
            {
                SetAnimFlapTrigger();
            }

            stunTimer -= Time.fixedDeltaTime;
        }
        else
        {
            if (recoveryTimer > 0.0f)
            {
                if (rb.velocity.y < 0.5f && Physics.Raycast(transform.position, Vector3.down, 1.0f, LayerMask.GetMask("Ground")))
                {
                    Vector3 newVel = rb.velocity;
                    newVel.y = stunForce;
                    rb.velocity = newVel;
                }

                recoveryTimer -= Time.fixedDeltaTime;
            }
        }
    }

    void UpdateWindSound()
    {
        float speed = rb.velocity.magnitude;
        windSource.volume = speed / 50.0f;
        windSource.pitch = 3.0f * speed / 50.0f;
    }

    void Update()
    {
        if (controlMethod == ControlMethod.Mouse)
        {
            if (Input.GetButtonDown("Cancel"))
            {
                Application.Quit();
            }
        }
    }

    Vector2 GetInput()
    {
        Vector2 input = Vector2.zero;
        if (controlMethod == ControlMethod.Mouse)
        {
            input = Input.mousePosition;
            input /= Screen.height;
            input.x -= 0.5f * cam.aspect;
            input.y -= 0.5f;
            input *= 2.0f;
        }
        else if (controlMethod == ControlMethod.Tilt)
        {
            input = new Vector2(Input.acceleration.x, Input.acceleration.y);
            input.x = Mathf.Clamp(2.0f * input.x, -1.0f, 1.0f);
            input.y = Mathf.Clamp(2.0f * input.y + 1.0f, -1.0f, 1.0f);
        }

        return SetCursor(input);
    }

    void FixedUpdate()
    {
        if (Time.timeScale > 0.0f)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ManageTimers();

            Vector2 input = GetInput();

            if (stunTimer <= 0.0f)
            {
                RollAndPitch(input);
            }

            rb.velocity += gravityScale * Physics.gravity * Time.fixedDeltaTime;
            ApplyDrag();

            if (stunTimer <= 0.0f)
            {
                LiftAndThrust();
            }
            else if (GetAnimDamageBool())
            {
                if (Physics.Raycast(transform.position, Vector3.down, rb.velocity.magnitude, LayerMask.GetMask("Ground")))
                {
                    SetAnimFlapTrigger();
                }
            }

            UpdateWindSound();
            AdjustRotation();
            EnforceBoundaries();
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }

    void RollAndPitch(Vector2 input)
    {
        if (input == Vector2.zero)
        {
            return;
        }

        // Lerps from deadzone to max value.
        float length = input.magnitude;
        input /= length;
        if (controlMethod == ControlMethod.Mouse)
        {
            input.x *= (length - inputDeadzone) / (cam.aspect - inputDeadzone);
            input.y *= (length - inputDeadzone) / (1.0f - inputDeadzone);
        }
        else
        {
            input *= (length - inputDeadzone) / (1.0f - inputDeadzone);
        }

        input.x *= maxRollSpeed * Time.fixedDeltaTime;
        input.y *= maxPitchSpeed * Time.fixedDeltaTime;

        Quaternion rot = Quaternion.Euler(input.y, 0.0f, -input.x);
        transform.rotation *= rot;
    }

    void ApplyDrag()
    {
        // Applies drag along vertical axis.
        float upSpeed = Vector3.Dot(rb.velocity, transform.up);
        rb.velocity -= sideDrag * upSpeed * transform.up * Time.fixedDeltaTime;

        // Applies drag along horizontal axis.
        float sideSpeed = Vector3.Dot(rb.velocity, transform.right);
        rb.velocity -= sideDrag * sideSpeed * transform.right * Time.fixedDeltaTime;

        // Applies drag along forward axis.
        float fwdSpeed = Vector3.Dot(rb.velocity, transform.forward);
        rb.velocity -= forwardDrag * fwdSpeed * transform.forward * Time.fixedDeltaTime;
    }

    void LiftAndThrust()
    {
        // Nullifies vertical speed, but only if it's downwards.
        float upSpeed = Vector3.Dot(rb.velocity, transform.up);
        if (upSpeed < 0.0f)
        {
            rb.velocity -= upSpeed * transform.up;
        }

        // Maintains minimum forward speed.
        float fwdSpeed = Vector3.Dot(rb.velocity, transform.forward);
        if (fwdSpeed < minSpeed)
        {
            rb.velocity -= fwdSpeed * transform.forward;
            rb.velocity += minSpeed * transform.forward;
            SetAnimFlapTrigger();
        }
        else
        {
            SetAnimFlapBool(false);
        }
    }

    void AdjustRotation()
    {
        Vector3 fwd;
        if (rb.velocity.sqrMagnitude < 1.0f)
        {
            fwd = transform.forward;
        }
        else
        {
            fwd = rb.velocity.normalized;
        }

        float factor;
        Vector3 up;
        if (stunTimer > 0.0f)
        {
            factor = 2.0f;
            up = Vector3.up;
            fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            if (fwd == Vector3.zero)
            {
                fwd = Vector3.forward;
            }
        }
        else
        {
            factor = 1.0f;
            up = Vector3.ProjectOnPlane(transform.up, fwd).normalized;
            if (up == Vector3.zero)
            {
                up = Vector3.Cross(fwd, transform.right);
            }
        }

        Quaternion target = Quaternion.LookRotation(fwd, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, factor * adjustSpeed * Time.fixedDeltaTime);
    }

    void EnforceBoundaries()
    {
        Vector3 newPos = transform.position;
        if (newPos.y > levelBounds.y)
        {
            stunTimer = 2.0f * stunLength;
        }
        else if (newPos.y < 0.0f)
        {
            newPos.y = 0.0f;
        }

        if (Mathf.Abs(transform.position.x) > levelBounds.x)
        {
            float sign = Mathf.Sign(newPos.x);
            newPos.x = sign * levelBounds.x;

            Vector3 fwd = Vector3.Reflect(transform.forward, -sign * Vector3.right);
            transform.rotation = Quaternion.LookRotation(fwd, transform.up);

            rb.velocity = Vector3.Reflect(rb.velocity, -sign * Vector3.right);
        }

        if (Mathf.Abs(transform.position.z) > levelBounds.z)
        {
            float sign = Mathf.Sign(newPos.z);
            newPos.z = sign * levelBounds.z;

            Vector3 fwd = Vector3.Reflect(transform.forward, -sign * Vector3.forward);
            transform.rotation = Quaternion.LookRotation(fwd, transform.up);

            rb.velocity = Vector3.Reflect(rb.velocity, -sign * Vector3.forward);
        }

        transform.position = newPos;
    }

    public static void EnforceBoundaries(ref Transform thing, ref Vector3 velocity)
    {
        Vector3 newPos = thing.position;
        if (thing.position.y < 0.0f)
        {
            newPos.y = 0.0f;
        }

        if (Mathf.Abs(thing.position.x) > levelBounds.x)
        {
            float sign = Mathf.Sign(newPos.x);
            newPos.x = sign * levelBounds.x;

            Vector3 fwd = Vector3.Reflect(thing.forward, -sign * Vector3.right);
            thing.rotation = Quaternion.LookRotation(fwd, thing.up);

            velocity = Vector3.Reflect(velocity, -sign * Vector3.right);
        }

        if (Mathf.Abs(thing.position.z) > levelBounds.z)
        {
            float sign = Mathf.Sign(newPos.z);
            newPos.z = sign * levelBounds.z;

            Vector3 fwd = Vector3.Reflect(thing.forward, -sign * Vector3.forward);
            thing.rotation = Quaternion.LookRotation(fwd, thing.up);

            velocity = Vector3.Reflect(velocity, -sign * Vector3.forward);
        }

        thing.position = newPos;
    }

    void HandleCollision(Collision collision)
    {
        Vector3 normal = collision.GetContact(0).normal;
        rb.velocity = Vector3.Reflect(rb.velocity, normal);
        rb.velocity += stunForce * normal;
        stunTimer = recoveryTimer = stunLength;

        onDamageEvent.Invoke();

        ShinyThing[] shinies = GetComponentsInChildren<ShinyThing>();
        if (shinies.Length > 0)
        {
            foreach(ShinyThing shiny in shinies)
            {
                shiny.Drop(normal);
            }
        }

        StartCoroutine(CollisionFlash());
    }

    int flashFrames = 10;

    IEnumerator CollisionFlash()
    {
        Color colour = Color.red;
        colour.g += 0.25f;
        colour.b += 0.25f;
        foreach (Material material in materials)
        {
            material.color = colour;
        }

        for (int i = 0; i < flashFrames; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = matColours[i] * Color.red;
        }

        yield return new WaitUntil(() => stunTimer <= 0.0f);

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = matColours[i];
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (stunTimer <= 0.0f && recoveryTimer <= 0.0f)
        {
            HandleCollision(collision);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (stunTimer <= 0.0f && recoveryTimer <= 0.0f)
        {
            HandleCollision(collision);
        }
    }

    void ApplyDeflection(Collider obstacle)
    {
        // Slightly deflects from nearby obstacles.
        Vector3 contact = obstacle.ClosestPoint(transform.position);
        Vector3 normal = transform.position - contact;

        if (normal == Vector3.zero)
        {
            normal = Vector3.up;
        }
        else if (Vector3.Angle(rb.velocity, -normal) > 45.0f)
        {
            return;
        }

        float dist = normal.magnitude;
        normal /= dist;

        Vector3 deflection = 0.5f * maxRateOfDeflection * Time.fixedDeltaTime * normal / dist;
        deflection = Vector3.ProjectOnPlane(deflection, transform.forward);
        rb.velocity += deflection;
    }

    void OnTriggerStay(Collider other)
    {
        int mask = LayerMask.GetMask("Ground", "Obstacle");
        if (((1 << other.gameObject.layer) & mask) > 0)
        {
            if (stunTimer <= 0.0f)
            {
                ApplyDeflection(other);
            }
        }
    }
}
