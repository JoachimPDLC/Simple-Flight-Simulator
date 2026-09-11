using Godot;

namespace SFS
{
    public struct Utilities
    {
        // From this unity forum post
        // https://discussions.unity.com/t/how-to-calculate-the-point-of-intercept-in-3d-space/22540/2
        public static Vector3 FirstOrderIntercept(Vector3 shooterPosition,
            Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, 
            Vector3 targetVelocity)
        {
            Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
            float t = FirstOrderInterceptTime(shotSpeed, targetPosition - 
                shooterPosition, targetRelativeVelocity);
            return targetPosition + t * (targetRelativeVelocity);
        }

        // first-order intercept using relative target position
        public static float FirstOrderInterceptTime(float shotSpeed,
            Vector3 targetRelativePosition, Vector3 targetRelativeVelocity)
        {
            float velocitySquared = targetRelativeVelocity.LengthSquared();
            if (velocitySquared < 0.001f)
                return 0f;

            float a = velocitySquared - shotSpeed * shotSpeed;

            // handle similar velocities
            if (Mathf.Abs(a) < 0.001f)
            {
                float t = -targetRelativePosition.LengthSquared() /
                    (2f * targetRelativeVelocity.Dot(targetRelativePosition));
                return Mathf.Max(t, 0f); // don't shoot back in time
            }

            float b = 2f * targetRelativeVelocity.Dot(targetRelativePosition);
            float c = targetRelativePosition.LengthSquared();
            float determinant = b * b - 4f * a * c;

            if (determinant > 0f)
            { // determinant > 0; two intercept paths (most common)
                float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a);
                float t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
                if (t1 > 0f)
                {
                    if (t2 > 0f)
                        return Mathf.Min(t1, t2); // both are positive
                    else
                        return t1; // only t1 is positive
                }
                else
                    return Mathf.Max(t2, 0f); // don't shoot back in time
            }
            else if (determinant < 0f) // determinant < 0; no intercept path
                return 0f;
            else // determinant = 0; one intercept path, pretty much never happens
                return Mathf.Max(-b / (2f * a), 0f); // don't shoot back in time
        }

        public static void TurnTowards(ref SFS.Plane plane, 
            Vector3 targetRotation, Vector3 targetPosition, float inputVal = 1)
        {
            //var planePos = plane.GlobalPosition;
            //var error = targetPosition - planePos;
            //error = plane.Quaternion.Inverse() * error;
            //
            //var errorDir = error.Normalized();
            //var pitchError = new Vector3(0, error.Y, error.Z).Normalized();
            //var rollError = new Vector3(error.X, error.Y, 0).Normalized();
            //var yawError = new Vector3(error.X, 0, error.Z).Normalized();
            //
            //Godot.Vector3 input = Vector3.Zero;
            //
            //var pitch = plane.Basis.Z.SignedAngleTo(pitchError, plane.Basis.X);
            //if (-pitch < Mathf.DegToRad(90)) 
            //    pitch += Mathf.DegToRad(360);
            //input.X = pitch;
            //
            //if (plane.Basis.Z.AngleTo(errorDir) < Mathf.DegToRad(30))
            //{
            //    var yaw = plane.Basis.Z.SignedAngleTo(yawError, plane.Basis.Y);
            //    input.Y = yaw;
            //}
            //else
            //{
            //    var roll = plane.Basis.Y.SignedAngleTo(rollError, plane.Basis.Z);
            //    input.Z = roll;
            //}
            //
            //input = input.Clamp(-inputVal, inputVal);
            //
            //plane.m_controlInput = input;

            Godot.Vector3 error = targetRotation - plane.GlobalRotation;
            
            Godot.Vector3 input = Vector3.Zero;
            
            if (Mathf.IsEqualApprox(error.X, 0))
                error.X = 0;
            if (Mathf.IsEqualApprox(error.Y, 0))
                error.Y = 0;
            if (Mathf.IsEqualApprox(error.Z, 0))
                error.Z = 0;
            
            if (IsFacing(plane, targetPosition))
            {
                if (error.X > 0)
                    input.X = inputVal;
                else if (error.X < 0)
                    input.X = -inputVal;
                else 
                    input.X = 0;
            
                if (error.Y > 0)
                    input.Y = inputVal;
                else if (error.Y < 0)
                    input.Y = -inputVal;
                else 
                    input.Y = 0;
            }
            else
            {
                input.X = -inputVal;
            }
            
            plane.m_controlInput = input;
        }

        public static Vector3 GetRotationTo(Node3D from, Vector3 to)
        {
            // Make sure the node isn't right on top of the "to" position,
            // as this causes problems with the LookingAt() func.
            if (from.GlobalPosition.X == to.X && from.GlobalPosition.Z == to.Z)
                to.X += 5;

            Transform3D newTransform = from.Transform.LookingAt(to, null, true);
            Vector3 targetRotation = newTransform.Basis.GetEuler();
            return targetRotation;
        }

        public static bool IsFacing(Node3D source, Vector3 target)
        {
            Vector3 diff = source.GlobalPosition.DirectionTo(target);
            if (source.GlobalBasis.Z.Dot(diff) > 0)
                return true;
            return false;
        }

        public static Node3D GetFirstNode3D(Node caller)
        {
            var children = caller.GetTree().Root.GetChildren();
            foreach (var child in children)
            {
                if (child is Node3D)                
                    return child as Node3D;                
            }
            return null;
        }

        public static T GetFirstChildOfType<T>(Node caller) where T : Node
        {
            if (caller == null)
                return null;

            var children = caller.GetChildren();

            if (children.Count == 0)
                return null;

            foreach(Node child in children)
            {
                if (child is T)                
                    return child as T;
            }
            return null;
        }

        public static bool VariantIsType<[MustBeVariant] T>(Variant data, out T obj)
        {
            if (data.Obj is T)
            {
                obj = data.As<T>();
                return obj != null;
            }
            obj = default;
            return false;
        }
    }

}
