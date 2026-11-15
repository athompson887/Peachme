// Portrait runner camera: behind and above the hero, looking down the track,
// pulled back a little when the peach is close so you can see it coming.

using UnityEngine;

namespace PeachMe.Game
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        public float Height = 4.2f;
        public float Back = 6.5f;
        public float LookAhead = 8f;
        public float LateralFollow = 0.5f;   // 0 = locked to centre, 1 = locked to hero
        public float ClosePullBack = 3f;     // extra metres back when the peach is on you
        public float Smooth = 8f;

        private RunDriver _driver;
        private float _lastZ = float.MaxValue;

        private void Start()
        {
            _driver = FindAnyObjectByType<RunDriver>();
            var cam = GetComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
        }

        private void LateUpdate()
        {
            var sim = _driver != null ? _driver.Sim : null;
            if (sim == null) return; // no run yet

            float danger = 1f - Mathf.Clamp01(sim.PeachGap / sim.Config.PeachMaxGap);
            float back = Back + ClosePullBack * danger * danger;
            float x = sim.X * LateralFollow;

            var wantPos = new Vector3(x, Height, sim.Z - back);
            var wantLook = new Vector3(x * 0.6f, 1f, sim.Z + LookAhead);

            // A new state or a retry resets Z to 0: snap rather than fly 450 m
            // backwards. Keyed on Z jumping back, not the stage index - a retry
            // at the chapter's first state keeps the same index.
            bool snap = sim.Z < _lastZ - 5f || _lastZ == float.MaxValue;
            _lastZ = sim.Z;
            float t = snap ? 1f : 1f - Mathf.Exp(-Smooth * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, wantPos, t);
            var wantRot = Quaternion.LookRotation(wantLook - wantPos, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, t);
        }
    }
}
