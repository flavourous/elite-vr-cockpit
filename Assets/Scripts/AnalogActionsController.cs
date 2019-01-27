using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Hand = EVRC.ActionsController.Hand;

namespace EVRC
{
    using Events = SteamVR_Events;

    public class AnalogActionsController : MonoBehaviour
    {
        public enum DirectionAction
        {
            D1,
            D2,
        }

        public class AnalogDirectionAction
        {
            public Hand hand;
            public DirectionAction button;
            public float x, y;

            public AnalogDirectionAction(Hand hand, DirectionAction button, float x, float y)
            {
                this.hand = hand;
                this.button = button;
                this.x = x;
                this.y = y;
            }
        }

        protected Action UnpressTouchpadHandler;
        protected Dictionary<Hand, short> trackpadTouchingCoroutineId = new Dictionary<Hand, short>()
        {
            { Hand.Left, 0 },
            { Hand.Right, 0 },
        };

        public static Events.Event<AnalogDirectionAction> AnalogDirectionActionUpdate = new Events.Event<AnalogDirectionAction>();
      
        public enum Direction : byte
        {
            Up,
            Right,
            Down,
            Left
        }

        void OnEnable()
        {
            Events.System(EVREventType.VREvent_ButtonPress).Listen(OnButtonPress);
            Events.System(EVREventType.VREvent_ButtonUnpress).Listen(OnButtonUnpress);
            Events.System(EVREventType.VREvent_ButtonTouch).Listen(OnButtonTouch);
            Events.System(EVREventType.VREvent_ButtonUntouch).Listen(OnButtonUntouch);
        }

        void OnDisable()
        {
            Events.System(EVREventType.VREvent_ButtonPress).Remove(OnButtonPress);
            Events.System(EVREventType.VREvent_ButtonUnpress).Remove(OnButtonPress);
            Events.System(EVREventType.VREvent_ButtonTouch).Remove(OnButtonTouch);
            Events.System(EVREventType.VREvent_ButtonUntouch).Remove(OnButtonUntouch);
        }

        void OnButtonPress(VREvent_t ev)
        {
            var hand = GetHandForDevice(ev.trackedDeviceIndex);
            var button = (EVRButtonId)ev.data.controller.button;

            if (button == EVRButtonId.k_EButton_SteamVR_Touchpad)
            {
                var vr = OpenVR.System;
                // For now this only handles the SteamVR Touchpad
                // In the future Joysticks and small WMR touchpads should be supported
                // Though it's probably easiest to switch to get the SteamVR Input API working to replace this first
                var err = ETrackedPropertyError.TrackedProp_Success;
                var axisTypeInt = vr.GetInt32TrackedDeviceProperty(ev.trackedDeviceIndex, ETrackedDeviceProperty.Prop_Axis0Type_Int32, ref err);
                if (err == ETrackedPropertyError.TrackedProp_Success)
                {
                    var axisType = (EVRControllerAxisType)axisTypeInt;
                    if (axisType == EVRControllerAxisType.k_eControllerAxis_TrackPad || axisType == EVRControllerAxisType.k_eControllerAxis_Joystick)
                    {
                        var state = new VRControllerState_t();
                        var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));
                        if (vr.GetControllerState(ev.trackedDeviceIndex, ref state, size))
                        {
                            var dirAna = new AnalogDirectionAction(hand, DirectionAction.D1, state.rAxis0.x, state.rAxis0.y);
                            AnalogDirectionActionUpdate.Send(dirAna);
                            UnpressTouchpadHandler = () =>
                            {
                                dirAna = new AnalogDirectionAction(hand, DirectionAction.D1, 0, 0);
                                AnalogDirectionActionUpdate.Send(dirAna);
                            };
                        }
                    }
                }
            }
        }

        void OnButtonUnpress(VREvent_t ev)
        {
            var hand = GetHandForDevice(ev.trackedDeviceIndex);
            var button = (EVRButtonId)ev.data.controller.button;

            if (button == EVRButtonId.k_EButton_SteamVR_Touchpad)
            {
                if (UnpressTouchpadHandler != null)
                {
                    UnpressTouchpadHandler();
                    UnpressTouchpadHandler = null;
                }
            }
        }

        private void OnButtonTouch(VREvent_t ev)
        {
            var hand = GetHandForDevice(ev.trackedDeviceIndex);
            var button = (EVRButtonId)ev.data.controller.button;

            if (button == EVRButtonId.k_EButton_SteamVR_Touchpad)
            {
                // For now this only handles the SteamVR Touchpad
                // In the future Joysticks and small WMR touchpads should be supported
                // Though it's probably easiest to switch to get the SteamVR Input API working to replace this first
                var err = ETrackedPropertyError.TrackedProp_Success;
                var axisTypeInt = OpenVR.System.GetInt32TrackedDeviceProperty(ev.trackedDeviceIndex, ETrackedDeviceProperty.Prop_Axis0Type_Int32, ref err);
                if (err == ETrackedPropertyError.TrackedProp_Success)
                {
                    var axisType = (EVRControllerAxisType)axisTypeInt;
                    if (axisType == EVRControllerAxisType.k_eControllerAxis_TrackPad || axisType == EVRControllerAxisType.k_eControllerAxis_Joystick)
                    {
                        trackpadTouchingCoroutineId[hand]++;
                        StartCoroutine(WhileTouchingTouchpadAxis0(ev.trackedDeviceIndex, hand, trackpadTouchingCoroutineId[hand]));
                    }
                }
            }
        }

        private void OnButtonUntouch(VREvent_t ev)
        {
            var hand = GetHandForDevice(ev.trackedDeviceIndex);
            var button = (EVRButtonId)ev.data.controller.button;

            if (button == EVRButtonId.k_EButton_SteamVR_Touchpad)
            {
                if (trackpadTouchingCoroutineId.ContainsKey(hand))
                {
                    // Increment the Id so the coroutine stops
                    trackpadTouchingCoroutineId[hand]++;
                }
            }
        }

        private IEnumerator WhileTouchingTouchpadAxis0(uint deviceIndex, Hand hand, short coroutineId)
        {
            var vr = OpenVR.System;
            var state = new VRControllerState_t();
            var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));

            while (vr.GetControllerState(deviceIndex, ref state, size))
            {
                var dirAna = new AnalogDirectionAction(hand, DirectionAction.D1, state.rAxis0.x, state.rAxis0.y);
                AnalogDirectionActionUpdate.Send(dirAna);

                yield return null;

                var current = trackpadTouchingCoroutineId[hand];
                if (current != coroutineId)
                {
                    if (current - coroutineId == 1)
                    {
                        dirAna = new AnalogDirectionAction(hand, DirectionAction.D1, 0, 0);
                        AnalogDirectionActionUpdate.Send(dirAna);
                    }

                    yield break;
                }
            }

            Debug.LogWarningFormat("Failed to get controller state for device {0}", deviceIndex);
        }

        public static Hand GetHandForDevice(uint deviceIndex)
        {
            var role = OpenVR.System.GetControllerRoleForTrackedDeviceIndex(deviceIndex);
            switch (role)
            {
                case ETrackedControllerRole.LeftHand: return Hand.Left;
                case ETrackedControllerRole.RightHand: return Hand.Right;
                default: return Hand.Unknown;
            }
        }
    }
}
