using System.Numerics;

namespace SpaceyToast.Source.Helpers
{
    /// <summary>
    /// Helper for various transformations such as translation, rotation or scaling.
    /// </summary>
    public sealed class Transform
    {
        private static float _actualScaling = 1.0f;
        private static Matrix3x2 _scale = Matrix3x2.Identity;
        private static Matrix3x2 _rotation = Matrix3x2.Identity;
        private static Matrix3x2 _translation = Matrix3x2.Identity;

        public static Matrix4x4 ConvertToMatrix4x4(Matrix3x2 matrix)
        {
            return new Matrix4x4(
                matrix.M11, matrix.M12, 0, 0,
                matrix.M21, matrix.M22, 0, 0,
                0, 0, 1, 0,
                matrix.M31, matrix.M32, 0, 1);
        }

        public static Matrix3x2 GetFinalMatrix() 
        { 
            _scale *= _scale * _rotation * _translation;
            return _scale;
        }

        public static Matrix3x2 Translate(Vector2 newPosition) 
        { 
            _translation = Matrix3x2.CreateTranslation(newPosition);
            return _translation;
        }

        public static Matrix3x2 Scale(float factor, Vector2 centerPoint) 
        {
            _actualScaling += (factor < 1 ? -0.1f : 0.1f);
            if (_actualScaling < 0.3f || _actualScaling > 5.0f)
            {
                if (_actualScaling > 5.0f)
                { _actualScaling = 5.0f; }
                else if (_actualScaling < 0.3f)
                { _actualScaling = 0.3f; }
                return Matrix3x2.Identity; // ignore transformation
            }
            _scale = Matrix3x2.CreateScale(factor, centerPoint);
            return _scale;
        }
        
        public static Matrix3x2 Rotate(float angleInRadians, Vector2 origin) 
        {
            _rotation = Matrix3x2.CreateRotation(angleInRadians, origin);
            return _rotation;
        }
    }
}
