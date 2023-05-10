using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    public class SyncMarkerElement : VisualElement
    {
        public static Color NormalColor => new Color(0f, 1f, 0f, 0.75f);
        public static Color WarningColor => new Color(1f, 0.76f, 0.03f, 0.75f);


        private string _name;
        private float _time;
        private bool _displayTime;
        private readonly Label _label;

        public int Index { get; set; } = -1;

        public bool DisplayTime
        {
            get => _displayTime;
            set
            {
                if (_displayTime == value) return;
                _displayTime = value;
                _label.text = _displayTime ? tooltip : _name;
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                tooltip = $"{_name}@{_time:F3}";
                _label.text = _displayTime ? tooltip : _name;
            }
        }

        public float Time
        {
            get => _time;
            set
            {
                if (Mathf.Abs(_time - value) < MathTool.Epsilon) return;
                _time = value;
                tooltip = $"{_name}@{_time:F3}";
                if (_displayTime) _label.text = tooltip;
            }
        }

        public bool Draggable { get; set; } = true;

        public event Action<SyncMarkerElement> OnDragStart;
        public event Action<SyncMarkerElement> OnDragEnd;
        public event Action<SyncMarkerElement> OnWantsToRename;
        public event Action<SyncMarkerElement> OnWantsToDelete;


        public SyncMarkerElement()
        {
            style.width = StyleKeyword.Auto;
            style.height = 14;
            style.borderTopRightRadius = 4;
            style.borderBottomRightRadius = 4;
            style.position = Position.Absolute;
            style.backgroundColor = NormalColor;

            _label = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    color = Color.black,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            Add(_label);

            RegisterCallback<ContextClickEvent>(OnContextClick);
        }

        public void SetColor(Color color)
        {
            style.backgroundColor = color;
        }

        public void StartDragging()
        {
            OnDragStart?.Invoke(this);
        }

        public void EndDragging()
        {
            OnDragEnd?.Invoke(this);
        }


        private void OnContextClick(ContextClickEvent evt)
        {
            evt.StopPropagation();

            var mousePosition = evt.mousePosition;
            var menu = new GenericDropdownMenu();
            menu.AddItem("Rename", false, PerformRename);
            menu.AddItem("Delete", false, () => OnWantsToDelete?.Invoke(this));
            menu.DropDown(new Rect(mousePosition, Vector2.zero), this);
        }

        private void PerformRename()
        {
            var nameField = new TextField
            {
                value = Name,
                isDelayed = true,
                style =
                {
                    minHeight = 16,
                }
            };
            nameField.RegisterCallback<FocusOutEvent>(e =>
            {
                Remove(nameField);
            });
            nameField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == e.previousValue)
                {
                    return;
                }

                if (string.IsNullOrEmpty(e.newValue))
                {
                    Debug.LogError("Sync marker name cannot be empty.");
                    return;
                }

                Name = e.newValue;
                Remove(nameField);

                OnWantsToRename?.Invoke(this);
            });
            Add(nameField);
            nameField.Focus();
        }
    }
}