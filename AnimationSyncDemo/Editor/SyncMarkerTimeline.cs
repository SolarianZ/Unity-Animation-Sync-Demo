using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    public class SyncMarkerTimeline : VisualElement
    {
        public static readonly Color BackgroundColor = Color.gray;
        public const float MinHeight = 40f;
        private const float StartTime = 0f;
        private const float EndTime = 1f;
        private const float TickStep = 0.1f;
        private const float Thickness = 1f;

        private readonly VisualElement _baseline;
        private readonly List<ScaleLine> _scaleLines = new();
        private readonly Slider _timeSlider;
        private readonly VisualElement _timeSliderDragger;
        private readonly Label _timeSliderLabel;

        private readonly List<SyncMarkerElement> _markerElements = new();
        public List<AnimationSyncMarker> Markers { get; set; }

        private bool _displayMarkerTime;
        public bool DisplayMarkerTime
        {
            get => _displayMarkerTime;
            set
            {
                if (_displayMarkerTime == value) return;
                _displayMarkerTime = value;
                foreach (var markerElement in _markerElements)
                {
                    markerElement.DisplayTime = _displayMarkerTime;
                }
            }
        }


        private float _time;
        public float Time
        {
            get => _time;
            set
            {
                _time = value;
                _timeSlider.SetValueWithoutNotify((float)MathTool.Wrap01(_time));
                _timeSliderLabel.text = _time.ToString("F3");
            }
        }

        public bool EnableTimeSlider
        {
            get => _timeSlider.enabledSelf;
            set => _timeSlider.SetEnabled(value);
        }

        public bool EnableMarkerEditing { get; set; } = true;

        private bool _isDraggingMarker;

        public event Action<float> OnTimeInternallyChanged;

        public Action OnBeforeModifyMarkers;
        public Action OnAfterModifyMarkers;


        public SyncMarkerTimeline()
        {
            // Style
            style.width = Length.Percent(100);
            style.height = 50;
            style.minHeight = MinHeight;
            style.marginLeft = 3;
            style.marginRight = 3;
            style.marginTop = 2;
            style.marginBottom = 2;
            style.backgroundColor = BackgroundColor;
            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.SpaceBetween;

            // Baseline
            _baseline = new VisualElement
            {
                name = "Baseline",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    backgroundColor = Color.white,
                    width = Length.Percent(100),
                    height = Thickness,
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = Length.Percent(60),
                },
            };
            Add(_baseline);

            // Scale line
            CreateScaleLines();

            // Time slider
            _timeSlider = new Slider(0f, 1f)
            {
                style =
                {
                    width = Length.Percent(100),
                    height = StyleKeyword.Auto,
                    position= Position.Absolute,
                    top = Length.Percent(60),
                    bottom = Length.Percent(30),
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                }
            };
            // Set slider content to the center
            var sliderDragContainer = _timeSlider.Q("unity-drag-container");
            sliderDragContainer.style.justifyContent = Justify.Center;
            // Hide slider tracker
            var sliderTracker = _timeSlider.Q("unity-tracker");
            sliderTracker.style.display = DisplayStyle.None;
            // Hide slider dragger border
            var sliderDraggerBorder = _timeSlider.Q("unity-dragger-border");
            sliderDraggerBorder.style.display = DisplayStyle.None;
            // Set the style of slider dragger
            _timeSliderDragger = _timeSlider.Q("unity-dragger");
            _timeSliderDragger.style.borderBottomLeftRadius = 0;
            _timeSliderDragger.style.borderBottomRightRadius = 0;
            _timeSliderDragger.style.borderTopLeftRadius = 0;
            _timeSliderDragger.style.borderTopRightRadius = 0;
            _timeSliderDragger.style.width = 6;
            _timeSliderDragger.style.flexShrink = 0;
            _timeSliderDragger.style.backgroundColor = Color.blue;
            // Add slider dragger label
            _timeSliderLabel = new Label(_timeSlider.value.ToString("F3"))
            {
                name = "DraggerLabel",
                style =
                {
                    color = Color.white,
                    fontSize = 10,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    position = Position.Absolute,
                    bottom = Length.Percent(85),
                }
            };
            _timeSliderDragger.style.flexDirection = FlexDirection.Row;
            _timeSliderDragger.style.justifyContent = Justify.Center;
            _timeSliderDragger.Add(_timeSliderLabel);
            _timeSliderDragger.RegisterCallback<ContextClickEvent>(OnTimeCursorContextClick);
            _timeSlider.RegisterValueChangedCallback(evt =>
            {
                _time = evt.newValue;
                _timeSliderLabel.text = _time.ToString("F3");
                OnTimeInternallyChanged?.Invoke(_time);
            });
            Add(_timeSlider);

            RegisterCallback<ContextClickEvent>(OnTimelineContextClick);
        }

        private void CreateScaleLines()
        {
            // Clear old scale lines
            foreach (var tick in _scaleLines) Remove(tick);
            _scaleLines.Clear();

            // Create scale lines
            int index = 0;
            while (true)
            {
                var value = StartTime + index * TickStep;
                if (value > EndTime + MathTool.Epsilon)
                {
                    break;
                }

                var scaleLine = new ScaleLine(Thickness, value)
                {
                    name = $"ScaleLine@{value:F3}",
                };
                _scaleLines.Add(scaleLine);
                Add(scaleLine);

                index++;
            }
        }


        public void Update()
        {
            if (!_isDraggingMarker)
            {
                UpdateMarkers();
            }
        }

        private void UpdateMarkers()
        {
            var markerCount = Markers?.Count ?? 0;
            var markerElementCount = _markerElements.Count;
            if (markerCount < markerElementCount)
            {
                for (int i = markerElementCount - 1; i > markerCount - 1; i--)
                {
                    Remove(_markerElements[i]);
                    _markerElements.RemoveAt(i);
                }
            }
            else if (markerCount > markerElementCount)
            {
                for (int i = 0; i < markerCount - markerElementCount; i++)
                {
                    var markerElement = new SyncMarkerElement();
                    _markerElements.Add(markerElement);
                    Add(markerElement);

                    markerElement.AddManipulator(new SyncMarkerDragger());
                    markerElement.OnDragStart += OnDragMarkerStart;
                    markerElement.OnDragEnd += OnDragMarkerEnd;
                    markerElement.OnWantsToRename += OnWantsToRenameMarker;
                    markerElement.OnWantsToDelete += OnWantsToDeleteMarker;
                }
            }

            for (int i = 0; i < markerCount; i++)
            {
                var marker = Markers[i];
                var markerElement = _markerElements[i];
                markerElement.style.left = Length.Percent((marker.Time - StartTime) / (EndTime - StartTime) * 100);
                markerElement.Index = i;
                markerElement.Name = marker.Name;
                markerElement.Time = marker.Time;
                markerElement.DisplayTime = _displayMarkerTime;
                markerElement.Draggable = EnableMarkerEditing;
            }
        }


        private void OnDragMarkerStart(SyncMarkerElement markerElement)
        {
            Assert.IsFalse(_isDraggingMarker);
            _isDraggingMarker = true;
        }

        private void OnDragMarkerEnd(SyncMarkerElement markerElement)
        {
            Assert.IsTrue(_isDraggingMarker);
            _isDraggingMarker = false;

            var marker = Markers[markerElement.Index];
            if (Mathf.Approximately(marker.Time, markerElement.Time))
            {
                return;
            }

            OnBeforeModifyMarkers?.Invoke();
            marker.Time = markerElement.Time;
            OnAfterModifyMarkers?.Invoke();
        }

        private void OnWantsToRenameMarker(SyncMarkerElement markerElement)
        {
            var marker = Markers[markerElement.Index];
            if (marker.Name == markerElement.name)
            {
                return;
            }

            OnBeforeModifyMarkers?.Invoke();
            marker.Name = markerElement.Name;
            OnAfterModifyMarkers?.Invoke();
        }

        private void OnWantsToDeleteMarker(SyncMarkerElement markerElement)
        {
            OnBeforeModifyMarkers?.Invoke();
            Markers.RemoveAt(markerElement.Index);
            OnAfterModifyMarkers?.Invoke();
        }

        private void OnTimelineContextClick(ContextClickEvent evt)
        {
            if (!EnableMarkerEditing || Markers == null)
            {
                return;
            }

            evt.StopPropagation();

            var localMousePosX = evt.localMousePosition.x;
            var menu = new GenericDropdownMenu();
            menu.AddItem("Add Marker", false, () => PerformAddMarker(localMousePosX / layout.width));
            menu.DropDown(new Rect(evt.mousePosition, Vector2.zero), this);
        }

        private void OnTimeCursorContextClick(ContextClickEvent evt)
        {
            if (!EnableMarkerEditing || Markers == null)
            {
                return;
            }

            evt.StopPropagation();

            var menu = new GenericDropdownMenu();
            menu.AddItem("Add Marker", false, () => PerformAddMarker(_timeSlider.value));
            menu.DropDown(new Rect(evt.mousePosition, Vector2.zero), _timeSliderDragger);
        }

        private void PerformAddMarker(float time)
        {
            var addMarkerField = new AddMarkerField(time, (markerName, markerTime) =>
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(markerName) && (markerTime >= 0 && markerTime <= 1));
                OnBeforeModifyMarkers?.Invoke();

                var marker = new AnimationSyncMarker
                {
                    Name = markerName,
                    Time = markerTime,
                };
                Markers.Add(marker);
                Markers.Sort((a, b) =>
                {
                    if (a.Time < b.Time) return -1;
                    if (a.Time > b.Time) return 1;
                    return 0;
                });

                OnAfterModifyMarkers?.Invoke();
            });

            Add(addMarkerField);
            addMarkerField.FocusNameInput();
        }


        class AddMarkerField : VisualElement
        {
            private readonly TextField _nameField;
            private readonly FloatField _timeField;
            private readonly Button _submitButton;
            private readonly Action<string, float> _onSubmitNewMarker;


            public AddMarkerField(float time, Action<string, float> onSubmitNewMarker)
            {
                _onSubmitNewMarker = onSubmitNewMarker;

                style.translate = new Translate(Length.Percent(-50), Length.Percent(-50), 0);
                style.position = Position.Absolute;
                style.top = Length.Percent(50);
                style.left = Length.Percent(50);
                style.flexDirection = FlexDirection.Row;
                style.backgroundColor = new Color(60f / 255, 60f / 255, 60f / 255, 1f);
                style.paddingLeft = 1;
                style.paddingRight = 1;
                style.paddingTop = 2;
                style.paddingBottom = 2;
                style.borderTopLeftRadius = 3;
                style.borderTopRightRadius = 3;
                style.borderBottomLeftRadius = 3;
                style.borderBottomRightRadius = 3;

                // Name field
                _nameField = new TextField
                {
                    isDelayed = true,
                    style =
                    {
                        minWidth = 40,
                    }
                };
                Add(_nameField);

                // Time field
                _timeField = new FloatField
                {
                    value = time,
                    isDelayed = true,
                    style =
                    {
                        minWidth = 40,
                    }
                };
                Add(_timeField);

                // Submit button
                _submitButton = new Button(TrySubmit)
                {
                    text = "Ok",
                };
                Add(_submitButton);

                RegisterCallback<FocusOutEvent>(OnPanelLostFocus);
            }

            public void FocusNameInput()
            {
                _nameField.Focus();
            }

            public void FocusTimeInput()
            {
                _timeField.Focus();
            }


            private void OnPanelLostFocus(FocusOutEvent evt)
            {
                var element = evt.relatedTarget as VisualElement;
                if (IsElementInPanel(element))
                {
                    return;
                }

                parent.Remove(this);
            }

            private bool IsElementInPanel(VisualElement element)
            {
                while (element != null)
                {
                    if (element == this)
                    {
                        return true;
                    }

                    element = element.parent;
                }

                return false;
            }

            private void TrySubmit()
            {
                if (string.IsNullOrWhiteSpace(_nameField.value))
                {
                    EditorUtility.DisplayDialog("Error", "Sync marker name cannot be empty.", "Ok");
                    return;
                }

                if (_timeField.value < 0 || _timeField.value > 1)
                {
                    _timeField.SetValueWithoutNotify(Mathf.Clamp01(_timeField.value));
                    EditorUtility.DisplayDialog("Error", "Sync marker time must be in range of [0.0,1.0].", "Ok");
                    return;
                }

                parent.Remove(this);

                _onSubmitNewMarker?.Invoke(_nameField.value, _timeField.value);
            }
        }

        class ScaleLine : VisualElement
        {
            private readonly VisualElement _scaleLine;
            private readonly Label _label;

            public ScaleLine(float thickness, float value)
            {
                style.width = thickness;

                _scaleLine = new VisualElement
                {
                    name = "ScaleLine",
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        backgroundColor = Color.white,
                        width = thickness,
                        height = Length.Percent(100),
                    }
                };
                Add(_scaleLine);

                _label = new Label(value.ToString("F1"))
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        color = Color.white,
                        fontSize = 11,
                        unityTextAlign = TextAnchor.MiddleCenter,
                    }
                };
                Add(_label);
            }
        }
    }
}