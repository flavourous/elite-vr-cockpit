using System;
using System.Collections.Generic;
using UnityEngine;
using static EVRC.ActionsController;
using static EVRC.PressManager;
using static EVRC.vJoyInterface;

namespace EVRC
{
    public class VirtualEmptyHand : VirtualControlButtons
    {
        public Hand hand;
        private ActionsControllerPressManager actionsPressManager;

        // Map of abstracted BtnAction presses to vJoy joystick button numbers
        private static Dictionary<Hand, Dictionary<BtnAction, uint>> joyBtnMap = new Dictionary<Hand, Dictionary<BtnAction, uint>>
        {
            {
                Hand.Left,
                new Dictionary<BtnAction, uint>
                {
                    { BtnAction.Trigger, 1 },
                    { BtnAction.Secondary, 2 },
                    { BtnAction.Alt, 3 },
                    { BtnAction.D1, 4 },
                    { BtnAction.D2, 5 }
                }
            },
            {
                Hand.Right,
                new Dictionary<BtnAction, uint>
                {
                    { BtnAction.Trigger, 7 },
                    { BtnAction.Secondary, 8 },
                    { BtnAction.Alt, 9 },
                    { BtnAction.D1, 10 },
                    { BtnAction.D2, 11 }
                }
            },
        };
        private static Dictionary<Hand, Dictionary<DirectionAction, uint>> joyHatMap = new Dictionary<Hand, Dictionary<DirectionAction, uint>>
        {
            {
                Hand.Left,
                new Dictionary<DirectionAction, uint>
                {
                    { DirectionAction.D1, 1 },
                    { DirectionAction.D2, 2 }
                }
            },
            {
                Hand.Right,
                new Dictionary<DirectionAction, uint>
                {
                    { DirectionAction.D1, 3 },
                    { DirectionAction.D2, 4 },
                }
            }
        };
        private static Dictionary<Direction, HatDirection> directionMap = new Dictionary<Direction, HatDirection>()
        {
            { Direction.Up, HatDirection.Up },
            { Direction.Right, HatDirection.Right },
            { Direction.Down, HatDirection.Down },
            { Direction.Left, HatDirection.Left },
        };

        private bool HandsEqual(TrackedHand.Hand tHand, ActionsController.Hand aHand)
        {
            switch (tHand)
            {
                case TrackedHand.Hand.Left: return aHand == Hand.Left;
                case TrackedHand.Hand.Right: return aHand == Hand.Right;
                default: return false;
            }
        }

        private void OnEnable()
        {
            ControllerInteractionPoint.GrabbableUnGrabed.AddListener(ObjectsUngrabbed);
            ControllerInteractionPoint.GrabbableGrabed.AddListener(ObjectsGrabbed);
            StartListening();
        }

        private void OnDisable()
        {
            ControllerInteractionPoint.GrabbableUnGrabed.RemoveListener(ObjectsUngrabbed);
            ControllerInteractionPoint.GrabbableGrabed.RemoveListener(ObjectsGrabbed);
            StopListening();
        }

        private void ObjectsUngrabbed(ControllerInteractionPoint.GrabableGrab args)
        {
            if (HandsEqual(args.hand, hand) && !args.grabbed)
            {
                StartListening();
            }
        }

        private void ObjectsGrabbed(ControllerInteractionPoint.GrabableGrab args)
        {
            if (HandsEqual(args.hand, hand) && args.grabbed)
            {
                StopListening();
            }
        }

        private void StartListening()
        {
            actionsPressManager = new ActionsControllerPressManager(this)
                  .ButtonAction(OnActionPress)
                  .DirectionAction(OnDirectionPress);
            AnalogActionsController.AnalogDirectionActionUpdate.AddListener(OnAnalogUpdate);
        }

        private void StopListening()
        {
            actionsPressManager.Clear();
            AnalogActionsController.AnalogDirectionActionUpdate.RemoveListener(OnAnalogUpdate);
            ReleaseAllInputs();
        }

        private void SetAnalogMode(bool isAnalog)
        {
            isAnalogMode = isAnalog;
            output.SetStickAxis(new VirtualJoystick.StickAxis(0, 0, 0));
        }

        private bool isAnalogMode = false;

        private void OnAnalogUpdate(AnalogActionsController.AnalogDirectionAction args)
        {
            if (!isAnalogMode) return;
            output.SetStickAxis(new VirtualJoystick.StickAxis(args.y * 180f, args.x * 180f, 0));
        }

        private static int nDpress = 0;

        private UnpressHandlerDelegate<ButtonActionsPress> OnActionPress(ButtonActionsPress pEv)
        {
            if (pEv.hand == hand && joyBtnMap.TryGetValue(hand, out var map) && map.TryGetValue(pEv.button, out uint vJoyButton))
            {
                var isDpress = pEv.button == BtnAction.D1 || pEv.button == BtnAction.D2;

                if (isDpress) nDpress++;

                PressButton(vJoyButton);

                if(nDpress==2)
                {
                    isAnalogMode = !isAnalogMode;
                }

                return unpress =>
                {
                    if (isDpress) nDpress--;
                    UnpressButton(vJoyButton);
                };
            }

            return delegate { };
        }

        private UnpressHandlerDelegate<DirectionActionsPress> OnDirectionPress(DirectionActionsPress pEv)
        {
            if (isAnalogMode) return delegate { };

            if (pEv.hand == hand && joyHatMap.TryGetValue(Hand.Right, out var map) && map.TryGetValue(pEv.button, out var vJoyButton) && directionMap.TryGetValue(pEv.direction, out var vJoyHat))
            {
                SetHatDirection(vJoyButton, vJoyHat);
                return unpress => ReleaseHatDirection(vJoyButton);
            }

            return delegate { };
        }
    }
}
