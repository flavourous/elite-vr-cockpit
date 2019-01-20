using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace EVRC
{
    using ButtonPress = ActionsController.ButtonPress;
    using Hand = TrackedHand.Hand;
    using Events = SteamVR_Events;

    public class ControllerInteractionPoint : MonoBehaviour
    {
        public float toggleGrabPressTiming = 0.35f;
        public TooltipDisplay tooltipDisplay;

        private TrackedHand trackedHand;
        private HashSet<IGrabable> intersectingGrababales = new HashSet<IGrabable>();
        private HashSet<IActivateable> intersectingActivatables = new HashSet<IActivateable>();
        private Dictionary<IActivateable, Action> pressedActivatableReleases = new Dictionary<IActivateable, Action>();
        private readonly HashSet<IGrabable> grabbing = new HashSet<IGrabable>();
        private readonly HashSet<IGrabable> toggleGrabbing = new HashSet<IGrabable>();
        private ITooltip tooltip;

        private float lastGrabPressTime;

        public struct GrabableGrab
        {
            public Hand hand;
            public bool grabbed;

            public GrabableGrab(Hand hand, bool grabbed)
            {
                this.hand = hand;
                this.grabbed = grabbed;
            }
        }

        public Hand Hand
        {
            get
            {
                return trackedHand.hand;
            }
        }

        public static Events.Event<GrabableGrab> GrabbableGrabed = new Events.Event<GrabableGrab>();
        public static Events.Event<GrabableGrab> GrabbableUnGrabed = new Events.Event<GrabableGrab>();

        void Start()
        {
            trackedHand = GetComponentInParent<TrackedHand>();
        }

        void OnEnable()
        {
            ActionsController.TriggerPress.Listen(OnTriggerPress);
            ActionsController.TriggerUnpress.Listen(OnTriggerUnpress);
            ActionsController.GrabPress.Listen(OnGrabPress);
            ActionsController.GrabUnpress.Listen(OnGrabUnpress);
            Tooltip.TooltipUpdated.Listen(OnTooltipUpdate);
        }

        void OnDisable()
        {
            ActionsController.TriggerPress.Remove(OnTriggerPress);
            ActionsController.TriggerUnpress.Remove(OnTriggerUnpress);
            ActionsController.GrabPress.Remove(OnGrabPress);
            ActionsController.GrabUnpress.Remove(OnGrabUnpress);
            Tooltip.TooltipUpdated.Remove(OnTooltipUpdate);
        }

        private void OnTooltipUpdate(ITooltip tooltip, string text)
        {
            if (this.tooltip == tooltip && tooltipDisplay.text != text)
            {
                tooltipDisplay.text = text;
                tooltipDisplay.Refresh();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var hoverable = other.GetComponent<IHoverable>();
            if (hoverable != null)
            {
                hoverable.Hover(this);
            }

            var grabables = other.GetComponents<IGrabable>();
            foreach (var grabable in grabables)
            {
                intersectingGrababales.Add(grabable);
            }

            var activatable = other.GetComponent<IActivateable>();
            if (activatable != null)
            {
                intersectingActivatables.Add(activatable);
            }

            var tt = other.GetComponent<ITooltip>();
            if (tt != null)
            {
                tooltip = tt;
                if (tooltipDisplay)
                {
                    tooltipDisplay.text = tooltip.GetTooltipText();
                    tooltipDisplay.enabled = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var hoverable = other.GetComponent<IHoverable>();
            if (hoverable != null)
            {
                hoverable.Unhover(this);
            }

            var grabables = other.GetComponents<IGrabable>();
            foreach (var grabable in grabables)
            {
                intersectingGrababales.Remove(grabable);
            }

            var activatable = other.GetComponent<IActivateable>();
            if (activatable != null)
            {
                intersectingActivatables.Remove(activatable);
                if (pressedActivatableReleases.ContainsKey(activatable))
                {
                    var unpress = pressedActivatableReleases[activatable];
                    unpress();
                    pressedActivatableReleases.Remove(activatable);
                }
            }

            var tt = other.GetComponent<ITooltip>();
            if (tt == tooltip)
            {
                tooltip = null;
                tooltipDisplay.enabled = false;
            }
        }

        private bool IsSameHand(Hand tHand, ActionsController.Hand bHand)
        {
            if (tHand == Hand.Left)
            {
                return bHand == ActionsController.Hand.Left;
            }
            else if (tHand == Hand.Right)
            {
                return bHand == ActionsController.Hand.Right;
            }
            return false;
        }

        private void OnTriggerPress(ButtonPress btn)
        {
            if (!IsSameHand(trackedHand.hand, btn.hand)) return;

            foreach (IActivateable button in intersectingActivatables)
            {
                var unpress = button.Activate(this);
                pressedActivatableReleases.Add(button, unpress);
            }
        }

        private void OnTriggerUnpress(ButtonPress btn)
        {
            if (!IsSameHand(trackedHand.hand, btn.hand)) return;

            foreach (var pressedActivatable in pressedActivatableReleases)
            {
                var unpress = pressedActivatable.Value;
                unpress();
                
            }

            pressedActivatableReleases.Clear();
        }

        /**
         * Force an activatable to be unpressed even when the user has not released it.
         * Normally used when a button is about to be hidden.
         */
        public void ForceUnpress(IActivateable button)
        {
            if (pressedActivatableReleases.ContainsKey(button))
            {
                var unpress = pressedActivatableReleases[button];
                unpress();
                pressedActivatableReleases.Remove(button);
            }
        }

        private void OnGrabPress(ButtonPress btn)
        {
            if (!IsSameHand(trackedHand.hand, btn.hand)) return;

            foreach (IGrabable grabable in intersectingGrababales)
            {
                var canGrab = !grabbing.Contains(grabable) &&
                    grabable.GetGrabMode().HasFlag(GrabMode.Grabable);
                if (canGrab && grabable.Grabbed(this))
                {
                    grabbing.Add(grabable);
                    GrabbableGrabed.Send(new GrabableGrab(trackedHand.hand, true));
                }
            }

            lastGrabPressTime = Time.time;
        }

        private void OnGrabUnpress(ButtonPress btn)
        {
            if (!IsSameHand(trackedHand.hand, btn.hand)) return;

            var delta = Time.time - lastGrabPressTime;
            var isUnderGrabToggleTiming = delta < toggleGrabPressTiming;

            bool wasUngrabbed = false;

            foreach (IGrabable grabable in grabbing)
            {
                // If we are toggle grabbing the object we should also remove it from the list
                if (toggleGrabbing.Contains(grabable))
                {
                    toggleGrabbing.Remove(grabable);
                    grabable.Ungrabbed(this);
                    wasUngrabbed = true;
                }
                // if this might be a grab toggle and the surface allows it,
                // add it to the toggle-grabbing list. otherwise, whether it's
                // not a grab toggle or the surface won't be toggle-grabbed,
                // ungrab the surface
                else if (isUnderGrabToggleTiming && grabable.GetGrabMode().HasFlag(GrabMode.ToggleGrabable))
                {
                    toggleGrabbing.Add(grabable);
                }
                else
                {
                    grabable.Ungrabbed(this);
                    wasUngrabbed = true;
                }
            }

            grabbing.Clear();
            foreach (var grabable in toggleGrabbing)
            {
                grabbing.Add(grabable);
            }

            if(wasUngrabbed)
            {
                GrabbableUnGrabed.Send(new GrabableGrab(trackedHand.hand, grabbing.Count > 0));
            }
        }

        /**
         * Force a grabbable to be ungrabbed even when the user has not released it.
         * Normally used when a control is about to be hidden.
         */
        public void ForceUngrab(IGrabable grabable)
        {
            if (toggleGrabbing.Contains(grabable))
            {
                toggleGrabbing.Remove(grabable);
            }
            if (grabbing.Contains(grabable))
            {
                grabable.Ungrabbed(this);
                grabbing.Remove(grabable);
            }
        }
    }
}
