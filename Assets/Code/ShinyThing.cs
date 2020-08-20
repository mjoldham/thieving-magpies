using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SpringMaths;

public class ShinyThing : MonoBehaviour
{
    PlayerInput holder = null;
    static readonly float coolDownDuration = 2.0f;
    float coolDownTimer = 0.0f;

    Vector3 velocity = Vector3.zero;

    int nestMask, blockMask;

    Nest levelNest;
    bool isDeposited = false;

    DampedVector3 spring;

    public UnityEvent onGrabEvent = new UnityEvent();

    [SerializeField]
    Vector3 anchorPoint = Vector3.down, worldScale = Vector3.one;

    void Rescale()
    {
        Vector3 scale = worldScale;
        if (transform.parent != null)
        {
            scale.x /= transform.parent.lossyScale.x;
            scale.y /= transform.parent.lossyScale.y;
            scale.z /= transform.parent.lossyScale.z;
        }

        transform.localScale = scale;
    }

    void Start()
    {
        nestMask = LayerMask.GetMask("Nest");
        blockMask = LayerMask.GetMask("Ground", "Obstacle");

        levelNest = FindObjectOfType<Nest>();
        Rescale();
        spring = new DampedVector3(10.0f, 1.0f, transform.position);
    }

    void FixedUpdate()
    {
        if (coolDownTimer > 0.0f)
        {
            coolDownTimer -= Time.fixedDeltaTime;
        }

        if (holder != null)
        {
            if (Physics.CheckSphere(transform.position, 1.0f, nestMask)
                || Physics.SphereCast(new Ray(transform.position, Vector3.down), 1.0f, 100.0f, nestMask))
            {
                Deposit();
            }
        }
        else
        {
            Transform t = transform;
            PlayerInput.EnforceBoundaries(ref t, ref velocity);
            transform.position = t.position;
            transform.rotation = t.rotation;
        }
    }

    Coroutine moving = null;
    void MoveToPosition(Vector3 position, bool isLocal = false)
    {
        if (moving != null)
        {
            StopCoroutine(moving);
            moving = null;
        }

        moving = StartCoroutine(MovingToPosition(position, isLocal));
    }

    IEnumerator MovingToPosition(Vector3 position, bool isLocal)
    {
        spring.velocity = Vector3.zero;
        if (isLocal)
        {
            spring.position = transform.localPosition;
            while (Vector3.Distance(transform.localPosition, position) > 0.1f)
            {
                yield return new WaitForFixedUpdate();
                transform.localPosition = spring.Step(position, Time.fixedDeltaTime);
            }
        }
        else
        {
            spring.position = transform.position;
            while (Vector3.Distance(transform.position, position) > 0.1f)
            {
                yield return new WaitForFixedUpdate();
                transform.position = spring.Step(position, Time.fixedDeltaTime);
            }
        }

        moving = null;
    }

    void Grab(PlayerInput grabber)
    {
        holder = grabber;
        transform.parent = holder.transform;
        coolDownTimer = coolDownDuration;
        Rescale();

        onGrabEvent.Invoke();
        holder.onGrabEvent.Invoke();

        MoveToPosition(anchorPoint, true);
    }

    void Deposit()
    {
        if (moving != null)
        {
            StopCoroutine(moving);
            moving = null;
        }

        moving = StartCoroutine(Depositing());
    }

    IEnumerator Depositing()
    {
        holder.onDepositEvent.Invoke();

        holder = null;
        transform.parent = null;
        coolDownTimer = coolDownDuration;
        Rescale();

        Vector3 vel = Vector3.zero;
        while (!Physics.CheckSphere(transform.position, 1.0f, nestMask))
        {
            yield return new WaitForFixedUpdate();
            vel += Physics.gravity * Time.fixedDeltaTime;
            transform.position += vel * Time.fixedDeltaTime;
        }

        onGrabEvent.Invoke();
        levelNest.BankedShinies++;
        isDeposited = true;

        MoveToPosition(levelNest.transform.position + 1.5f * Vector3.up);
    }

    public void Drop(Vector3 normal)
    {
        if (moving != null)
        {
            StopCoroutine(moving);
        }

        moving = StartCoroutine(Dropping(normal));
    }

    IEnumerator Dropping(Vector3 impactNormal)
    {
        holder = null;
        transform.parent = null;
        coolDownTimer = coolDownDuration;
        Rescale();

        // Calculates random velocity relative to collision normal.
        Vector3 velDir = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.5f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
        velocity = 10.0f * velDir;
        velocity = Quaternion.FromToRotation(Vector3.up, impactNormal) * velocity;

        // Moves this transform according to velocity, gravity, and repulsion from objects.
        float sleepTime = 6.0f;
        float sleepVel = 0.7f;
        float timer = 0.0f;
        float repelRadius = 5.0f;
        float maxRepel = 100.0f;
        float drag = 0.01f;
        Collider[] colliders = new Collider[10];
        while (true)
        {
            // Adds change in velocity due to gravity.
            Vector3 gravityDeltaVec = Physics.gravity * Time.fixedDeltaTime;
            velocity += gravityDeltaVec;
            float gravityDelta = gravityDeltaVec.magnitude;

            // Repels from each collider within repel radius.
            Physics.OverlapSphereNonAlloc(transform.position, repelRadius, colliders, blockMask);
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    break;
                }

                Vector3 r = transform.position - collider.ClosestPoint(transform.position);
                // If r is zero then the transform is inside the collider.
                if (r == Vector3.zero)
                {
                    r = GetDirectionToOutside(collider);
                    velocity += maxRepel * gravityDelta * r;
                }
                else
                {
                    float dist = r.magnitude;
                    r /= dist;
                    float distScale = repelRadius / dist;
                    distScale = float.IsNaN(distScale) ? maxRepel : Mathf.Min(distScale, maxRepel);

                    velocity += distScale * gravityDelta * r;
                }
            }

            System.Array.Clear(colliders, 0, colliders.Length);
            velocity -= drag * velocity;
            transform.position += velocity * Time.fixedDeltaTime;

            if (velocity.magnitude < sleepVel)
            {
                timer += Time.fixedDeltaTime;
                if (timer >= sleepTime)
                {
                    break;
                }
            }
            else
            {
                timer = 0.0f;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    Vector3 GetDirectionToOutside(Collider collider)
    {
        Vector3 r, localPos;
        switch (collider)
        {
            case BoxCollider box:
                localPos = box.transform.InverseTransformPoint(transform.position) - box.center;
                Vector3 boxExtents = 0.5f * box.size;

                // Finds closest side of box so that the closest point can be calculated.
                // Distance to min extent.
                float dx1 = -boxExtents.x - localPos.x;
                // Distance to max extent.
                float dx2 = boxExtents.x - localPos.x;
                r.x = Mathf.Abs(dx1) < Mathf.Abs(dx2) ? dx1 : dx2;

                float dy1 = -boxExtents.y - localPos.y;
                float dy2 = boxExtents.y - localPos.y;
                r.y = Mathf.Abs(dy1) < Mathf.Abs(dy2) ? dy1 : dy2;

                float dz1 = -boxExtents.z - localPos.z;
                float dz2 = boxExtents.z - localPos.z;
                r.z = Mathf.Abs(dz1) < Mathf.Abs(dz2) ? dz1 : dz2;

                // If on an edge, change r to point outwards.
                if (r.x == 0.0f)
                {
                    r = Mathf.Sign(localPos.x) * Vector3.right;
                }
                else if (r.y == 0.0f)
                {
                    r = Mathf.Sign(localPos.y) * Vector3.up;
                }
                else if (r.z == 0.0f)
                {
                    r = Mathf.Sign(localPos.z) * Vector3.forward;
                }
                // Otherwise find the minimum difference.
                else if (Mathf.Abs(r.x) < Mathf.Abs(r.y))
                {
                    r.y = 0.0f;

                    if (Mathf.Abs(r.x) < Mathf.Abs(r.z))
                    {
                        r.z = 0.0f;
                    }
                    else
                    {
                        r.x = 0.0f;
                    }
                }
                else
                {
                    r.x = 0.0f;

                    if (Mathf.Abs(r.y) < Mathf.Abs(r.z))
                    {
                        r.z = 0.0f;
                    }
                    else
                    {
                        r.y = 0.0f;
                    }
                }

                return box.transform.TransformDirection(r).normalized;

            case CapsuleCollider capsule:
                Vector3 capsuleDir;
                if (capsule.direction == 0)
                {
                    capsuleDir = Vector3.right;
                }
                else if (capsule.direction == 1)
                {
                    capsuleDir = Vector3.up;
                }
                else
                {
                    capsuleDir = Vector3.forward;
                }

                localPos = capsule.transform.InverseTransformPoint(transform.position) - capsule.center;

                // If in one of the capsule's two caps...
                if (Mathf.Abs(localPos[capsule.direction]) >= 0.5f * capsule.height - capsule.radius)
                {
                    float sign = Mathf.Sign(localPos[capsule.direction]);
                    Vector3 capCentre = sign * (0.5f * capsule.height - capsule.radius) * capsuleDir;
                    if (localPos != capCentre)
                    {
                        r = localPos - capCentre;
                    }
                    else
                    {
                        r = sign * capsuleDir;
                    }
                }
                // Else in the middle of the capsule...
                else
                {
                    Vector3 linCentre = localPos[capsule.direction] * capsuleDir;
                    if (localPos != linCentre)
                    {
                        r = localPos - linCentre;
                    }
                    else
                    {
                        if (capsule.direction == 1)
                        {
                            r = Vector3.forward;
                        }
                        else
                        {
                            r = Vector3.up;
                        }
                    }
                }

                return capsule.transform.TransformDirection(r).normalized;

            case SphereCollider sphere:
                localPos = sphere.transform.InverseTransformPoint(transform.position) - sphere.center;
                if (localPos != Vector3.zero)
                {
                    r = sphere.transform.TransformDirection(localPos).normalized;
                }
                else
                {
                    r = Vector3.up;
                }

                return r;

            default:
                if (transform.position != collider.transform.position)
                {
                    r = (transform.position - collider.transform.position).normalized;
                }
                else
                {
                    r = Vector3.up;
                }

                return r;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }

        if (!isDeposited && coolDownTimer <= 0.0f && other.TryGetComponent(out PlayerInput temp))
        {
            Grab(temp);
        }
    }
}
