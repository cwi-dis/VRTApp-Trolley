using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace VRT.Pilots.Trolley
{
    public class TrainController : MonoBehaviour
    {
        [Header("Train")]
        [SerializeField] Transform train;
        [SerializeField] float modelForwardYaw = 180f;

        [Header("Rail")]
        [Tooltip("SplineContainer with 2 splines: index 0 = straight (inaction), index 1 = branch (action).")]
        [SerializeField] SplineContainer rail;

        [Header("Movement")]
        [SerializeField] float trainSpeed = 3f;

        Spline _current;
        float _t;
        bool _moving;

        public void StartApproach()
        {
            if (rail == null || rail.Splines.Count == 0) return;
            _current = rail.Splines[0];

            float3 localPos = rail.transform.InverseTransformPoint(train.position);
            SplineUtility.GetNearestPoint(_current, localPos, out _, out _t);

            _moving = true;
        }

        public void ExecuteAction()
        {
            if (rail == null || rail.Splines.Count < 2) return;
            float3 localPos = rail.transform.InverseTransformPoint(train.position);
            SplineUtility.GetNearestPoint(rail.Splines[1], localPos, out _, out _t);
            _current = rail.Splines[1];
        }

        public void ExecuteInaction() { }

        void Update()
        {
            if (!_moving || _current == null || train == null) return;

            float len = _current.GetLength();
            if (len < 0.01f) return;

            _t += (trainSpeed / len) * Time.deltaTime;

            if (_t >= 1f)
            {
                _t = 1f;
                _moving = false;
                return;
            }

            train.position = EvaluateWorld(_t);

            float3 tangent = SplineUtility.EvaluateTangent(_current, _t);
            Vector3 worldTangent = rail.transform.TransformDirection(tangent);
            if (worldTangent.sqrMagnitude > 0.001f)
                train.rotation = Quaternion.Slerp(train.rotation,
                    Quaternion.LookRotation(worldTangent) * Quaternion.Euler(0f, modelForwardYaw, 0f),
                    10f * Time.deltaTime);
        }

        Vector3 EvaluateWorld(float t)
        {
            float3 local = SplineUtility.EvaluatePosition(_current, t);
            return rail.transform.TransformPoint(local);
        }
    }
}
