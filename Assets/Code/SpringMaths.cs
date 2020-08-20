using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpringMaths
{
    // Based on http://mathproofs.blogspot.com/2013/07/critically-damped-spring-smoothing.html
    // and https://github.com/keijiro/SmoothingTest/blob/master/Assets/Flask/Tween.cs
    public abstract class DampedSpring<T, U>
    {
        public float undampedFreq, dampingRatio;
        public T position;
        public U velocity;

        public DampedSpring(float undampedFreq, float dampingRatio, T position)
        {
            this.undampedFreq = undampedFreq;
            this.dampingRatio = dampingRatio;
            this.position = position;
        }

        public abstract T Step(T target, float dt, float limit = 0.0f);
    }

    public class DampedFloat : DampedSpring<float, float>
    {
        public DampedFloat(float undampedFreq, float dampingRatio, float position)
            : base(undampedFreq, dampingRatio, position)
        {
            velocity = 0.0f;
        }

        public override float Step(float target, float dt, float limit = 0.0f)
        {
            // Calculating new velocity.
            velocity += undampedFreq * undampedFreq * dt * (target - position);
            velocity /= (1.0f + undampedFreq * undampedFreq * dt * dt + 2.0f * dampingRatio * undampedFreq * dt);

            // Calculating new position.
            position += velocity * dt;

            if (limit > 0.0f)
            {
                if (Mathf.Abs(target - position) > limit)
                {
                    position = target + limit * Mathf.Sign(position - target);
                }
            }

            return position;
        }
    }

    public class DampedVector2 : DampedSpring<Vector2, Vector2>
    {
        public DampedVector2(float undampedFreq, float dampingRatio, Vector2 position)
            : base(undampedFreq, dampingRatio, position)
        {
            velocity = Vector2.zero;
        }

        public override Vector2 Step(Vector2 target, float dt, float limit = 0.0f)
        {
            // Calculating new velocity.
            velocity += undampedFreq * undampedFreq * dt * (target - position);
            velocity /= (1.0f + undampedFreq * undampedFreq * dt * dt + 2.0f * dampingRatio * undampedFreq * dt);

            // Calculating new position.
            position += velocity * dt;

            if (limit > 0.0f)
            {
                if ((target - position).sqrMagnitude > limit * limit)
                {
                    position = target + limit * (position - target).normalized;
                }
            }

            return position;
        }
    }

    public class DampedVector3 : DampedSpring<Vector3, Vector3>
    {
        public DampedVector3(float undampedFreq, float dampingRatio, Vector3 position)
            : base(undampedFreq, dampingRatio, position)
        {
            velocity = Vector3.zero;
        }

        public override Vector3 Step(Vector3 target, float dt, float limit = 0.0f)
        {
            // Calculating new velocity.
            velocity += undampedFreq * undampedFreq * dt * (target - position);
            velocity /= (1.0f + undampedFreq * undampedFreq * dt * dt + 2.0f * dampingRatio * undampedFreq * dt);

            // Calculating new position.
            position += velocity * dt;

            if (limit > 0.0f)
            {
                if ((target - position).sqrMagnitude > limit * limit)
                {
                    position = target + limit * (position - target).normalized;
                }
            }

            return position;
        }
    }

    public class DampedVector4 : DampedSpring<Vector4, Vector4>
    {
        public DampedVector4(float undampedFreq, float dampingRatio, Vector4 position)
            : base(undampedFreq, dampingRatio, position)
        {
            velocity = Vector4.zero;
        }

        public override Vector4 Step(Vector4 target, float dt, float limit = 0.0f)
        {
            // Calculating new velocity.
            velocity += undampedFreq * undampedFreq * dt * (target - position);
            velocity /= (1.0f + undampedFreq * undampedFreq * dt * dt + 2.0f * dampingRatio * undampedFreq * dt);

            // Calculating new position.
            position += velocity * dt;

            if (limit > 0.0f)
            {
                if ((target - position).sqrMagnitude > limit * limit)
                {
                    position = target + limit * (position - target).normalized;
                }
            }

            return position;
        }
    }

    static class Conversion
    {
        public static Vector4 ToVector4(Quaternion q)
        {
            return new Vector4(q.x, q.y, q.z, q.w);
        }

        public static Quaternion ToQuaternion(Vector4 v)
        {
            return new Quaternion(v.x, v.y, v.z, v.w);
        }
    }

    public class DampedQuaternion : DampedSpring<Quaternion, Vector4>
    {
        public DampedQuaternion(float undampedFreq, float dampingRatio, Quaternion position)
            : base(undampedFreq, dampingRatio, position)
        {
            velocity = Vector4.zero;
        }

        public override Quaternion Step(Quaternion target, float dt, float limit = 0.0f)
        {
            Vector4 vPosition = Conversion.ToVector4(position);
            Vector4 vTarget = Conversion.ToVector4(target);
            if (Vector4.Dot(vPosition, vTarget) < 0.0f)
            {
                vTarget = -vTarget;
            }

            // Calculating new velocity.
            velocity += undampedFreq * undampedFreq * dt * (vTarget - vPosition);
            velocity /= 1.0f + undampedFreq * undampedFreq * dt * dt + 2.0f * dampingRatio * undampedFreq * dt;

            // Calculating new position.
            vPosition += velocity * dt;
            vPosition.Normalize();
            position = Conversion.ToQuaternion(vPosition);

            if (limit > 0.0f)
            {
                if ((vTarget - vPosition).sqrMagnitude > limit * limit)
                {
                    vPosition = vTarget + limit * (vPosition - vTarget).normalized;
                    vPosition.Normalize();
                    position = Conversion.ToQuaternion(vPosition);
                }
            }

            return position;
        }
    }
}