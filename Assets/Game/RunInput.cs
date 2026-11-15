// Touch swipes first (it's a phone game), with keyboard and gamepad for the editor.
// Reads devices directly through the Input System - no .inputactions asset to
// drift out of step with the code.
//
// Swipe left/right = lane, up = jump, down = slide, double-tap = special,
// single tap = confirm (start, retry). Everything latches until Consume() so a
// swipe between two sim ticks is never lost.

using PeachMe.SimCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeachMe.Game
{
    // Before RunDriver (-100), so this frame's swipe reaches this frame's step.
    [DefaultExecutionOrder(-200)]
    public sealed class RunInput : MonoBehaviour
    {
        [Tooltip("Swipe distance as a fraction of the screen's shorter side.")]
        public float SwipeFraction = 0.06f;

        [Tooltip("A touch shorter than this, that barely moved, is a tap.")]
        public float TapSeconds = 0.25f;

        [Tooltip("Two taps within this window are a double-tap (special).")]
        public float DoubleTapSeconds = 0.3f;

        private SimInput _pending;
        private bool _confirm;

        private bool _touching;
        private bool _swipeUsed;
        private Vector2 _touchStart;
        private float _touchStartTime;
        private float _lastTapTime = -10f;

        public SimInput Consume()
        {
            var p = _pending;
            _pending = SimInput.None;
            return p;
        }

        public bool ConsumeConfirm()
        {
            bool c = _confirm;
            _confirm = false;
            return c;
        }

        public void Clear()
        {
            _pending = SimInput.None;
            _confirm = false;
        }

        private void Update()
        {
            ReadKeyboard();
            ReadGamepad();
            ReadTouch();
        }

        private void ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return; // no keyboard on a phone: normal, not an error
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) _pending.LaneDelta = -1;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) _pending.LaneDelta = 1;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) _pending.Jump = true;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) _pending.Slide = true;
            if (kb.eKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame) _pending.Special = true;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) _confirm = true;
        }

        private void ReadGamepad()
        {
            var pad = Gamepad.current;
            if (pad == null) return; // no pad connected: normal
            if (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame) _pending.LaneDelta = -1;
            if (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame) _pending.LaneDelta = 1;
            if (pad.buttonSouth.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame) _pending.Jump = true;
            if (pad.buttonEast.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame) _pending.Slide = true;
            if (pad.buttonWest.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame) _pending.Special = true;
            if (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame) _confirm = true;
        }

        private void ReadTouch()
        {
            var ts = Touchscreen.current;
            if (ts == null) return; // editor without a touchscreen: normal
            var touch = ts.primaryTouch;
            bool pressed = touch.press.isPressed;
            Vector2 pos = touch.position.ReadValue();

            if (pressed && !_touching)
            {
                _touching = true;
                _swipeUsed = false;
                _touchStart = pos;
                _touchStartTime = Time.unscaledTime;
            }
            else if (pressed && _touching && !_swipeUsed)
            {
                // Fire the swipe as soon as it's long enough - waiting for the
                // finger to lift feels laggy in a runner.
                Vector2 d = pos - _touchStart;
                float threshold = SwipeFraction * Mathf.Min(Screen.width, Screen.height);
                if (d.magnitude >= threshold)
                {
                    _swipeUsed = true;
                    if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) _pending.LaneDelta = d.x > 0 ? 1 : -1;
                    else if (d.y > 0) _pending.Jump = true;
                    else _pending.Slide = true;
                }
            }
            else if (!pressed && _touching)
            {
                _touching = false;
                bool quick = Time.unscaledTime - _touchStartTime <= TapSeconds;
                if (!_swipeUsed && quick)
                {
                    _confirm = true;
                    if (Time.unscaledTime - _lastTapTime <= DoubleTapSeconds)
                    {
                        _pending.Special = true;
                        _lastTapTime = -10f;
                    }
                    else
                    {
                        _lastTapTime = Time.unscaledTime;
                    }
                }
            }
        }
    }
}
