using System.Collections;
using UnityEngine;

namespace _scripts.controllers
{
    public class LinkController : MonoBehaviour
    {
        [SerializeField] private Mode _mode=Mode.Joint;
        private MotionType _motionType = MotionType.Revolution;
        private float _rotation;
        private Coroutine _motionCoroutine;
        private float _minimumAngle;
        private float _maximumAngle;
        private float _speed;

        internal Mode mode
        {
            get => _mode;
            set => _mode = value;
        }


        private enum MotionType
        {
            Linear,
            Revolution,
        }

        internal enum Mode
        {
            Joint,
            World
        }

        internal float minimumAngle
        {
            get => _minimumAngle;
            set => _minimumAngle = value;
        }

        internal float maximumAngle
        {
            get => _maximumAngle;
            set => _maximumAngle = value;
        }

        internal float speed
        {
            get => _speed;
            set => _speed = value;
        }

        internal float currentRotation => _rotation;

        private void FixedUpdate()
        {
            switch (_mode)
            {
                case Mode.Joint:
                    switch (_motionType)
                    {
                        case MotionType.Linear:
                            break;
                        case MotionType.Revolution:
                            transform.localRotation = Quaternion.Euler(0, _rotation, 0);
                            break;
                    }
                    break;
                case Mode.World:
                    break;
                
            }
        }

        internal void StartContinuousMotion(bool direction)
        {
            StopMotion();
            _motionCoroutine = StartCoroutine(ContinuousMotionCoroutine(direction));
        }
        
        internal void StartStepMotion(float target, float step)
        {
            StopMotion();
            _motionCoroutine = StartCoroutine(StepMotionCoroutine(target, step));
        }

        internal void StopMotion()
        {
            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
                _motionCoroutine = null;
            }
        }
        internal bool IsMoving()
        {
            return _motionCoroutine != null;
        }
        private IEnumerator ContinuousMotionCoroutine(bool direction)
        {
            yield return new WaitForSeconds(0.3f);
            
            if (_speed <= 0)
            {
                Debug.LogWarning("Speed not set or invalid");
                _motionCoroutine = null;
                yield break;
            }

            while (true)
            {
                var increment = direction ? 0.25f : -0.25f;
                var newRotation = _rotation + increment;
                
                if (newRotation >= _minimumAngle && newRotation <= _maximumAngle)
                {
                    _rotation = newRotation;
                }
                else
                {
                    _rotation = Mathf.Clamp(_rotation, _minimumAngle, _maximumAngle);
                    _motionCoroutine = null;
                    yield break;
                }
                
                yield return new WaitForSeconds(_speed);
            }
        }
        
        private IEnumerator StepMotionCoroutine(float target, float step)
        {
            yield return new WaitForSeconds(0.3f);

            if (_speed <= 0)
            {
                Debug.LogWarning("Speed not set or invalid");
                _motionCoroutine = null;
                yield break;
            }

            var startPosition = _rotation;
            var endPosition = startPosition + target;
            endPosition = Mathf.Clamp(endPosition, _minimumAngle, _maximumAngle);
            var currentPosition = startPosition;

            while (Mathf.Abs(endPosition - currentPosition) > 0.1f)
            {
                var direction = Mathf.Sign(endPosition - currentPosition);
                currentPosition += step * direction;

                if (direction > 0)
                    currentPosition = Mathf.Min(currentPosition, endPosition);
                else
                    currentPosition = Mathf.Max(currentPosition, endPosition);

                _rotation = Mathf.Clamp(currentPosition, _minimumAngle, _maximumAngle);

                yield return new WaitForSeconds(_speed);
            }

            _rotation = Mathf.Clamp(endPosition, _minimumAngle, _maximumAngle);
            _motionCoroutine = null;
        }
        
        internal void SetRotation(float angle)
        {
            _rotation = Mathf.Clamp(angle, _minimumAngle, _maximumAngle);
        }
        
        internal float GetNormalizedAngle()
        {
            var angle = _rotation;
            if (angle > 180f) angle -= 360f;
            return angle;
        }
    }
}