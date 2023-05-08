using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    public class SyncMarkerEditorWindow : EditorWindow, IHasCustomMenu
    {
        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("Tools/Bamboo/Animation Sync Marker Editor")]
        public static SyncMarkerEditorWindow Open()
        {
            return GetWindow<SyncMarkerEditorWindow>("Animation Sync Marker Editor");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject is AnimationSyncMarkerAsset syncMarkerAsset)
            {
                var window = Open();
                window.SetTargetAsset(syncMarkerAsset);
                window.Focus();

                return true;
            }

            return false;
        }


        private ObjectField _assetField;

        private ObjectField _clipField;

        [SerializeField]
        private AnimationSyncMarkerAsset _markerAsset;

        [SerializeField]
        private AnimationClip _previewClip;

        private SyncMarkerTimeline _timeline;

        private IMGUIContainer _animPreviewContainer;

        private MotionPreview _animPreview;

        private GUIStyle _animPreviewStyle;


        private AnimationSyncMarkerAsset TargetAsset => _assetField.value as AnimationSyncMarkerAsset;


        public void SetTargetAsset(AnimationSyncMarkerAsset asset)
        {
            _assetField.value = asset;
        }

        private void OnEnable()
        {
            // Target asset field
            _assetField = new ObjectField("Target Asset")
            {
                objectType = typeof(AnimationSyncMarkerAsset),
                value = _markerAsset,
                style =
                {
                    marginTop = 16,
                    marginBottom = 4,
                },
            };
            _assetField.RegisterValueChangedCallback(OnTargetAssetChanged);
            rootVisualElement.Add(_assetField);

            // Timeline
            var tmlContainer = new VisualElement
            {
                name = "TimelineContainer",
                style =
                {
                    paddingLeft = 40,
                    paddingRight = 40,
                    backgroundColor = SyncMarkerTimeline.BackgroundColor,
                    minHeight = SyncMarkerTimeline.MinHeight,
                }
            };
            _timeline = new SyncMarkerTimeline();
            _timeline.OnBeforeModifyMarkers += RecordMarkerAssetModificationUndo;
            _timeline.OnAfterModifyMarkers += SetMarkerAssetDirty;
            tmlContainer.Add(_timeline);
            rootVisualElement.Add(tmlContainer);

            // Animation preview clip
            _clipField = new ObjectField("Preview Clip")
            {
                objectType = typeof(AnimationClip),
                value = _previewClip,
                style =
                {
                    marginTop = 16,
                    marginBottom = 4,
                },
            };
            _clipField.RegisterValueChangedCallback(OnPreviewClipChanged);
            rootVisualElement.Add(_clipField);

            // Animation preview gui
            _animPreview ??= new MotionPreview();
            _animPreview.Initialize(_previewClip);
            _animPreview.SetNormalizedTime(_timeline.Time);
            _animPreviewContainer = new IMGUIContainer(DrawAnimPreviewGUI)
            {
                style =
                {
                    flexGrow = 1,
                }
            };
            rootVisualElement.Add(_animPreviewContainer);
        }


        private void Update()
        {
            // Update timeline
            _timeline.DisplayMarkerTime = _displayMarkerTime;
            _timeline.Markers = TargetAsset ? TargetAsset.SyncMarkers : null;
            _timeline.Update();

            // Update animation preview
            _animPreviewContainer.MarkDirtyRepaint();

            // Time slider
            _timeline.Time = _animPreview.GetNormalizedTime();
        }

        private void OnDisable()
        {
            _animPreview.OnDisable();
        }


        private void DrawAnimPreviewGUI()
        {
            _animPreviewStyle ??= "PreBackgroundSolid";

            _animPreview.OnPreviewSettings();
            _animPreview.OnInteractivePreviewGUI(_animPreviewContainer.worldBound, _animPreviewStyle);
        }

        private void RecordMarkerAssetModificationUndo()
        {
            Undo.RecordObject(_markerAsset, "Modify Sync Marker");
        }

        private void SetMarkerAssetDirty()
        {
            _markerAsset.EnsureMarkersInAscendOrder();
            EditorUtility.SetDirty(_markerAsset);
        }

        private void OnTargetAssetChanged(ChangeEvent<Object> evt)
        {
            _markerAsset = (AnimationSyncMarkerAsset)evt.newValue;
        }

        private void OnPreviewClipChanged(ChangeEvent<Object> evt)
        {
            _previewClip = (AnimationClip)evt.newValue;
            _animPreview.Initialize(_previewClip);
        }


        #region Custom Menu

        [SerializeField]
        private bool _displayMarkerTime = true;

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Display Marker Time"), _displayMarkerTime, () =>
            {
                _displayMarkerTime = !_displayMarkerTime;
            });
        }

        #endregion
    }
}