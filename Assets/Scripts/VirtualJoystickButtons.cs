﻿using System.Collections.Generic;
using UnityEngine;

namespace EVRC
{
    using Hand = ActionsController.Hand;
    using BtnAction = ActionsController.BtnAction;
    using ButtonActionsPress = ActionsController.ButtonActionsPress;
    using DirectionAction = ActionsController.DirectionAction;
    using Direction = ActionsController.Direction;
    using HatDirection = vJoyInterface.HatDirection;

    /**
     * Outputs joystick buttons to vJoy when the associated joystick is grabbed
     */
    public class VirtualJoystickButtons : MonoBehaviour
    {
        public vJoyInterface output;
        // Map of abstractes BtnAction presses to vJoy joystick button numbers
        private static Dictionary<BtnAction, uint> joyBtnMap = new Dictionary<BtnAction, uint>()
        {
            { BtnAction.Trigger, 1 },
            { BtnAction.Secondary, 2 },
            { BtnAction.Alt, 3 },
            { BtnAction.D1, 4 },
            { BtnAction.D2, 5 },
        };
        private static Dictionary<DirectionAction, uint> joyHatMap = new Dictionary<DirectionAction, uint>()
        {
            { DirectionAction.D1, 1 },
            { DirectionAction.D2, 2 },
        };
        private static Dictionary<Direction, HatDirection> directionMap = new Dictionary<Direction, HatDirection>()
        {
            { Direction.Up, HatDirection.Up },
            { Direction.Right, HatDirection.Right },
            { Direction.Down, HatDirection.Down },
            { Direction.Left, HatDirection.Left },
        };
        // The hand of the controller grabbing the joystick, Unknown is considered "not grabbing"
        private Hand grabbedHand = Hand.Unknown;

        private void OnEnable()
        {
            ActionsController.ButtonActionPress.Listen(OnActionPress);
            ActionsController.ButtonActionUnpress.Listen(OnActionUnpress);
            ActionsController.DirectionActionPress.Listen(OnDirectionPress);
            ActionsController.DirectionActionUnpress.Listen(OnDirectionUnpress);
        }

        private void OnDisable()
        {
            ActionsController.ButtonActionPress.Remove(OnActionPress);
            ActionsController.ButtonActionUnpress.Remove(OnActionUnpress);
            ActionsController.DirectionActionPress.Remove(OnDirectionPress);
            ActionsController.DirectionActionUnpress.Remove(OnDirectionUnpress);
        }

        public void Grabbed(Hand hand)
        {
            grabbedHand = hand;
        }

        public void Ungrabbed()
        {
            grabbedHand = Hand.Unknown;
            // @todo Release all buttons when ungrabbed
        }

        private void OnActionPress(ButtonActionsPress ev)
        {
            if (grabbedHand == Hand.Unknown) return; // not grabbing
            if (grabbedHand != ev.hand) return; // wrong hand

            if (joyBtnMap.ContainsKey(ev.button))
            {
                uint btnIndex = joyBtnMap[ev.button];
                if (output)
                {
                    output.SetButton(btnIndex, true);
                }
            }
        }

        private void OnActionUnpress(ButtonActionsPress ev)
        {
            if (grabbedHand == Hand.Unknown) return; // not grabbing
            if (grabbedHand != ev.hand) return; // wrong hand

            if (joyBtnMap.ContainsKey(ev.button))
            {
                uint btnNumber = joyBtnMap[ev.button];
                if (output)
                {
                    output.SetButton(btnNumber, false);
                }
            }
        }

        private void OnDirectionPress(ActionsController.DirectionActionsPress ev)
        {
            if (grabbedHand == Hand.Unknown) return; // not grabbing
            if (grabbedHand != ev.hand) return; // wrong hand

            if (joyHatMap.ContainsKey(ev.button))
            {
                uint hatNumber = joyHatMap[ev.button];
                if (output)
                {
                    output.SetHatDirection(hatNumber, directionMap[ev.direction]);
                }
            }
        }

        private void OnDirectionUnpress(ActionsController.DirectionActionsPress ev)
        {
            if (grabbedHand == Hand.Unknown) return; // not grabbing
            if (grabbedHand != ev.hand) return; // wrong hand

            if (joyHatMap.ContainsKey(ev.button))
            {
                uint hatNumber = joyHatMap[ev.button];
                if (output)
                {
                    output.SetHatDirection(hatNumber, HatDirection.Neutral);
                }
            }
        }
    }
}
