using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    public class SyncMarkerDragger : MouseManipulator
    {
        private float _startTime;
        private bool _active;

        public SyncMarkerDragger()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }


        private void OnMouseDown(MouseDownEvent e)
        {
            if (_active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            var markerElement = target as SyncMarkerElement;
            if (markerElement == null || !markerElement.Draggable)
            {
                return;
            }

            _startTime = e.localMousePosition.x;
            _active = true;
            markerElement.CaptureMouse();
            e.StopPropagation();

            markerElement.BringToFront();
            markerElement.StartDragging();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!_active)
            {
                return;
            }

            var markerElement = target as SyncMarkerElement;
            if (markerElement == null || !markerElement.Draggable)
            {
                return;
            }

            Assert.IsTrue(markerElement.resolvedStyle.position == Position.Absolute);

            var diff = e.localMousePosition.x - _startTime;
            diff *= markerElement.transform.scale.x;

            var parentWidth = markerElement.parent.worldBound.width;
            var position = markerElement.layout.x;
            position = Mathf.Clamp(position + diff, 0, parentWidth);
            markerElement.style.left = position;

            var time = position / parentWidth;
            markerElement.Time = time;

            e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!_active || !CanStopManipulation(e))
            {
                return;
            }

            var markerElement = target as SyncMarkerElement;
            if (markerElement == null || !markerElement.Draggable)
            {
                return;
            }

            _active = false;
            markerElement.ReleaseMouse();
            e.StopPropagation();

            markerElement.EndDragging();
        }
    }
}