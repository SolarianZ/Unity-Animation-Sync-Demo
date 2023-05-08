using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    [CustomEditor(typeof(AnimationSyncMarkerAsset))]
    public class SyncMarkerAssetInspector : UnityEditor.Editor, IHasCustomMenu
    {
        private VisualElement _defaultInspectorContainer;

        private SyncMarkerTimeline _timeline;

        private AnimationClip _previewClip;

        private MotionPreview _animPreview;


        private void OnEnable()
        {
            _previewClip = PreviewClipCacheTable.GetPreviewClip((AnimationSyncMarkerAsset)target);
            _animPreview ??= new MotionPreview();
            _animPreview.Initialize(_previewClip);

            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;

            _animPreview.OnDisable();
        }


        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement
            {
                name = "InspectorRoot",
            };

            // Default inspector
            _defaultInspectorContainer = new VisualElement
            {
                name = "DefaultInspector",
                style =
                {
                    display = _displayDefaultInspector ? DisplayStyle.Flex : DisplayStyle.None,
                }
            };
            InspectorElement.FillDefaultInspector(_defaultInspectorContainer, serializedObject, this);
            root.Add(_defaultInspectorContainer);

            // Timeline
            var tmlContainer = new VisualElement
            {
                name = "TimelineContainer",
                style =
                {
                    marginTop = 16,
                    paddingLeft = 40,
                    paddingRight = 40,
                    backgroundColor = SyncMarkerTimeline.BackgroundColor,
                    minHeight = SyncMarkerTimeline.MinHeight,
                }
            };
            _timeline = new SyncMarkerTimeline();
            _timeline.Markers = ((AnimationSyncMarkerAsset)target).SyncMarkers;
            _timeline.Update();
            _timeline.OnBeforeModifyMarkers += RecordMarkerAssetModificationUndo;
            _timeline.OnAfterModifyMarkers += SetMarkerAssetDirty;
            tmlContainer.Add(_timeline);
            root.Add(tmlContainer);

            return root;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewSettings()
        {
            _animPreview.OnPreviewSettings();
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            EditorGUI.BeginChangeCheck();
            _previewClip = (AnimationClip)EditorGUILayout.ObjectField("Preview Clip", _previewClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                PreviewClipCacheTable.SetPreviewClipCache((AnimationSyncMarkerAsset)target, _previewClip);
                _animPreview.Initialize(_previewClip);
            }

            _animPreview.OnInteractivePreviewGUI(r, background);
        }


        private void Update()
        {
            // Update timeline
            if (_timeline != null)
            {
                _timeline.Time = _animPreview.GetNormalizedTime();
                _timeline.DisplayMarkerTime = _displayMarkerTime;
                _timeline.Update();
            }
        }

        private void RecordMarkerAssetModificationUndo()
        {
            Undo.RecordObject(((AnimationSyncMarkerAsset)target), "Modify Sync Marker");
        }

        private void SetMarkerAssetDirty()
        {
            ((AnimationSyncMarkerAsset)target).EnsureMarkersInAscendOrder();
            EditorUtility.SetDirty(target);
        }


        #region Preview Clip Cache

        [Serializable]
        class PreviewClipCacheTable
        {
            [SerializeField]
            private List<PreviewClipCachePair> _items = new();


            public static AnimationClip GetPreviewClip(AnimationSyncMarkerAsset marker)
            {
                var markerPath = AssetDatabase.GetAssetPath(marker);
                if (string.IsNullOrEmpty(markerPath))
                {
                    return null;
                }

                var instance = GetInstance();
                var clipCaches = instance._items;
                var markerGuid = AssetDatabase.AssetPathToGUID(markerPath);
                var clipCacheIndex = clipCaches.FindIndex(item => item.MarkerGuid == markerGuid);
                if (clipCacheIndex == -1)
                {
                    return null;
                }

                var clipCache = clipCaches[clipCacheIndex];
                var clipPath = AssetDatabase.GUIDToAssetPath(clipCache.ClipGuid);
                if (string.IsNullOrEmpty(clipPath))
                {
                    clipCaches.RemoveAt(clipCacheIndex);
                    SaveInstance(instance);
                    return null;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (!clip)
                {
                    clipCaches.RemoveAt(clipCacheIndex);
                    SaveInstance(instance);
                    return null;
                }

                return clip;
            }

            public static void SetPreviewClipCache(AnimationSyncMarkerAsset marker, AnimationClip clip)
            {
                var markerPath = AssetDatabase.GetAssetPath(marker);
                if (string.IsNullOrEmpty(markerPath))
                {
                    return;
                }

                string clipGuid;
                var clipPath = AssetDatabase.GetAssetPath(clip);
                if (string.IsNullOrEmpty(clipPath))
                {
                    clipGuid = null;
                }
                else
                {
                    clipGuid = AssetDatabase.AssetPathToGUID(clipPath);
                }

                var instance = GetInstance();
                var markerGuid = AssetDatabase.AssetPathToGUID(markerPath);
                var clipCaches = instance._items;
                var clipCacheIndex = clipCaches.FindIndex(item => item.MarkerGuid == markerGuid);

                if (string.IsNullOrEmpty(clipGuid))
                {
                    if (clipCacheIndex == -1)
                    {
                        return;
                    }

                    clipCaches.RemoveAt(clipCacheIndex);
                    SaveInstance(instance);
                    return;
                }

                if (clipCacheIndex != -1)
                {
                    clipCaches[clipCacheIndex].ClipGuid = clipGuid;
                }
                else
                {
                    clipCaches.Add(new PreviewClipCachePair { MarkerGuid = markerGuid, ClipGuid = clipGuid, });
                }

                SaveInstance(instance);
            }

            public static void DeleteAllCaches()
            {
                EditorPrefs.DeleteKey(GetPreviewClipCacheTableKey());
            }


            private static string GetPreviewClipCacheTableKey()
            {
                return $"SyncMarkerPreviewClipCacheTable@{Application.companyName}@{Application.productName}";
            }

            private static PreviewClipCacheTable GetInstance()
            {
                var json = EditorPrefs.GetString(GetPreviewClipCacheTableKey());
                if (string.IsNullOrEmpty(json))
                {
                    return new PreviewClipCacheTable();
                }

                return JsonUtility.FromJson<PreviewClipCacheTable>(json);
            }

            private static void SaveInstance(PreviewClipCacheTable instance)
            {
                var json = JsonUtility.ToJson(instance);
                EditorPrefs.SetString(GetPreviewClipCacheTableKey(), json);
            }


            [Serializable]
            class PreviewClipCachePair
            {
                public string MarkerGuid;
                public string ClipGuid;

                public override string ToString()
                {
                    return $"{{MarkerGuid:\"{MarkerGuid}\",ClipGuid:\"{ClipGuid}\"}}";
                }
            }
        }

        #endregion


        #region Custom Menu

        [SerializeField]
        private bool _displayMarkerTime = true;

        [SerializeField]
        private bool _displayDefaultInspector;

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Display marker time"), _displayMarkerTime, () =>
            {
                _displayMarkerTime = !_displayMarkerTime;
            });

            menu.AddItem(new GUIContent("Display default inspector"), _displayDefaultInspector, () =>
            {
                _displayDefaultInspector = !_displayDefaultInspector;
                _defaultInspectorContainer.style.display = _displayDefaultInspector ? DisplayStyle.Flex : DisplayStyle.None;
            });

            menu.AddItem(new GUIContent("Delete all preview clip caches"), false, () =>
            {
                PreviewClipCacheTable.DeleteAllCaches();
            });
        }

        #endregion
    }
}
