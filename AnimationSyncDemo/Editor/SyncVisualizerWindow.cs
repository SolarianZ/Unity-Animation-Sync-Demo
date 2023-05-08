using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AnimationSyncDemo.Editor
{
    public class SyncVisualizerWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Bamboo/Animation Sync Visualizer")]
        public static void Open()
        {
            GetWindow<SyncVisualizerWindow>("Animation Sync Visualizer");
        }


        [SerializeField]
        private AnimationSyncMarkerAsset _markerAssetA;
        [SerializeField]
        private AnimationSyncMarkerAsset _markerAssetB;
        [SerializeField]
        private float _alpha;

        private Toolbar _toolbar;
        private Label _deltaTimeLabel;

        private ObjectField _syncMarkerA;
        private ObjectField _syncMarkerB;
        private SyncMarkerTimeline _timelineA;
        private SyncMarkerTimeline _timelineB;

        private AnimationSyncMarkerAsset SyncMarkerAssetA => _syncMarkerA.value as AnimationSyncMarkerAsset;
        private AnimationSyncMarkerAsset SyncMarkerAssetB => _syncMarkerB.value as AnimationSyncMarkerAsset;

        [SerializeField]
        private bool _isPlaying;
        private bool _movingStep;
        [SerializeField]
        private bool _isLooping = true;
        [SerializeField]
        private float _playbackSpeed = 1f;
        [SerializeField]
        private int _maxFps = 30;
        private double _lastFrameTime;


        private void OnEnable()
        {
            // Data
            _lastFrameTime = EditorApplication.timeSinceStartup;

            // Toolbar
            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // Play/Pause button
            var playPauseButton = new ToolbarButton
            {
                style =
                {
                    minWidth=24,
                    backgroundImage = GetPlayPauseButtonImage(),
                }
            };
            playPauseButton.clicked += () =>
            {
                _isPlaying = !_isPlaying;
                playPauseButton.style.backgroundImage = GetPlayPauseButtonImage();
            };
            _toolbar.Add(playPauseButton);

            // Next step button
            var nextStepButton = new ToolbarButton()
            {
                style =
                {
                    minWidth=24,
                    backgroundImage = GetNextStepButtonImage(),
                }
            };
            nextStepButton.clicked += () =>
            {
                _movingStep = true;
                _isPlaying = false;
                playPauseButton.style.backgroundImage = GetPlayPauseButtonImage();
            };
            _toolbar.Add(nextStepButton);

            // Loop toggle
            var loopToggle = new ToolbarToggle()
            {
                text = "Loop",
                value = _isLooping,
            };
            loopToggle.RegisterValueChangedCallback(evt => _isLooping = evt.newValue);
            _toolbar.Add(loopToggle);

            // Playback speed field
            var playbackSpeedField = new FloatField("Speed")
            {
                value = _playbackSpeed,
                isDelayed = true
            };
            playbackSpeedField.RegisterValueChangedCallback(evt => _playbackSpeed = evt.newValue);
            playbackSpeedField.Q<Label>().style.minWidth = StyleKeyword.Auto;
            playbackSpeedField.Q("unity-text-input").style.minWidth = 40;
            _toolbar.Add(playbackSpeedField);

            // Max fps field
            var maxFpsField = new IntegerField("Max FPS")
            {
                value = _maxFps,
                isDelayed = true
            };
            maxFpsField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 1)
                {
                    _maxFps = 1;
                    maxFpsField.SetValueWithoutNotify(1);
                }
                else
                {
                    _maxFps = evt.newValue;
                }
            });
            maxFpsField.Q<Label>().style.minWidth = StyleKeyword.Auto;
            maxFpsField.Q("unity-text-input").style.minWidth = 40;
            _toolbar.Add(maxFpsField);

            // Reset time button
            var resetTimeButton = new ToolbarButton { text = "Reset Time", };
            resetTimeButton.clicked += OnResetTime;
            _toolbar.Add(resetTimeButton);

            // Delta time label
            _deltaTimeLabel = new Label("Delta Time(ms): -")
            {
                style =
                {
                    width=110,
                    unityTextAlign=TextAnchor.MiddleLeft,
                }
            };
            _toolbar.Add(_deltaTimeLabel);

            // Alpha slider
            var alphaSlider = new Slider("Alpha", 0, 1)
            {
                value = _alpha,
                showInputField = true,
                style =
                {
                    flexGrow=1,
                }
            };
            alphaSlider.RegisterValueChangedCallback(OnAlphaChanged);
            alphaSlider.Q<Label>().style.minWidth = StyleKeyword.Auto;
            _toolbar.Add(alphaSlider);

            // Sync marker asset A
            _syncMarkerA = new ObjectField($"Marker A ({(1 - _alpha) * 100:F2}%, {(_alpha < 0.5f ? "L" : "F")})")
            {
                objectType = typeof(AnimationSyncMarkerAsset),
                value = _markerAssetA,
            };
            _syncMarkerA.RegisterValueChangedCallback(OnSyncMarkerAChanged);
            rootVisualElement.Add(_syncMarkerA);

            // Sync marker timeline A
            var tmlContainerA = new VisualElement
            {
                name = "TimelineContainer",
                style =
                {
                    paddingLeft = 36,
                    paddingRight = 36,
                    backgroundColor = SyncMarkerTimeline.BackgroundColor,
                    minHeight = SyncMarkerTimeline.MinHeight,
                }
            };
            _timelineA = new SyncMarkerTimeline();
            _timelineA.OnBeforeModifyMarkers += RecordMarkerAssetModificationUndoA;
            _timelineA.OnAfterModifyMarkers += SetMarkerAssetDirtyA;
            tmlContainerA.Add(_timelineA);
            rootVisualElement.Add(tmlContainerA);

            // Sync marker asset B
            _syncMarkerB = new ObjectField($"Marker B ({_alpha * 100:F2}%, {(_alpha >= 0.5f ? "L" : "F")})")
            {
                objectType = typeof(AnimationSyncMarkerAsset),
                value = _markerAssetB,
            };
            _syncMarkerB.RegisterValueChangedCallback(OnSyncMarkerBChanged);
            rootVisualElement.Add(_syncMarkerB);

            // Sync marker timeline B
            var tmlContainerB = new VisualElement
            {
                name = "TimelineContainer",
                style =
                {
                    paddingLeft = 36,
                    paddingRight = 36,
                    backgroundColor = SyncMarkerTimeline.BackgroundColor,
                    minHeight = SyncMarkerTimeline.MinHeight,
                }
            };
            _timelineB = new SyncMarkerTimeline();
            _timelineB.OnBeforeModifyMarkers += RecordMarkerAssetModificationUndoB;
            _timelineB.OnAfterModifyMarkers += SetMarkerAssetDirtyB;
            tmlContainerB.Add(_timelineB);
            rootVisualElement.Add(tmlContainerB);
        }

        private Texture2D GetPlayPauseButtonImage()
        {
            if (_isPlaying)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return (Texture2D)EditorGUIUtility.Load("d_PauseButton@2x");
                }

                return (Texture2D)EditorGUIUtility.Load("PauseButton@2x");
            }

            if (EditorGUIUtility.isProSkin)
            {
                return (Texture2D)EditorGUIUtility.Load("d_PlayButton@2x");
            }

            return (Texture2D)EditorGUIUtility.Load("PlayButton@2x");
        }

        private Texture2D GetNextStepButtonImage()
        {
            if (EditorGUIUtility.isProSkin)
            {
                return (Texture2D)EditorGUIUtility.Load("d_StepButton@2x");
            }

            return (Texture2D)EditorGUIUtility.Load("StepButton@2x");
        }

        private void Update()
        {
            UpdateTimelines();
            UpdateAnimationTime();
        }

        private void UpdateTimelines()
        {
            _timelineA.DisplayMarkerTime = _displayMarkerTime;
            _timelineA.Markers = SyncMarkerAssetA ? SyncMarkerAssetA.SyncMarkers : null;
            _timelineA.Update();

            _timelineB.DisplayMarkerTime = _displayMarkerTime;
            _timelineB.Markers = SyncMarkerAssetB ? SyncMarkerAssetB.SyncMarkers : null;
            _timelineB.Update();
        }

        private void UpdateAnimationTime()
        {
            SyncMarkerTimeline leader, follower;
            AnimationSyncMarkerAsset leaderAsset, followerAsset;
            if (_alpha < 0.5f)
            {
                leader = _timelineA;
                follower = _timelineB;
                leaderAsset = _markerAssetA;
                followerAsset = _markerAssetB;
            }
            else
            {
                leader = _timelineB;
                follower = _timelineA;
                leaderAsset = _markerAssetB;
                followerAsset = _markerAssetA;
            }

            var currFrameTime = EditorApplication.timeSinceStartup;
            var deltaTime = (float)(currFrameTime - _lastFrameTime);
            if (deltaTime < 1f / _maxFps)
            {
                return;
            }

            _lastFrameTime = currFrameTime;
            _deltaTimeLabel.text = $"Delta Time(ms): {(int)(deltaTime * 1000)}";

            if (!_isPlaying && !_movingStep)
            {
                return;
            }

            if (!_markerAssetA || !_markerAssetB)
            {
                return;
            }

            leader.Time += deltaTime * _playbackSpeed;
            leaderAsset.TryGetSyncLeaderInfo(leader.Time, _playbackSpeed,
                out var markedPos, out var prevMarkerName, out var nextMarkerName);
            var leaderInfo = new AnimationSyncInfo(leader.Time, markedPos, prevMarkerName, nextMarkerName);

            follower.Time = (float)followerAsset.GetSyncFollowerPosition(follower.Time, _playbackSpeed, _isLooping, leaderInfo);

            _movingStep = false;
        }


        private void OnResetTime()
        {
            _timelineA.Time = 0;
            _timelineB.Time = 0;
        }

        private void OnAlphaChanged(ChangeEvent<float> evt)
        {
            _alpha = evt.newValue;
            _syncMarkerA.label = $"Marker A ({(1 - _alpha) * 100:F2}%, {(_alpha < 0.5f ? "L" : "F")})";
            _syncMarkerB.label = $"Marker B ({_alpha * 100:F2}%, {(_alpha >= 0.5f ? "L" : "F")})";
        }

        private void OnSyncMarkerAChanged(ChangeEvent<Object> evt)
        {
            _markerAssetA = (AnimationSyncMarkerAsset)evt.newValue;
        }

        private void OnSyncMarkerBChanged(ChangeEvent<Object> evt)
        {
            _markerAssetB = (AnimationSyncMarkerAsset)evt.newValue;
        }

        private void RecordMarkerAssetModificationUndoA()
        {
            Undo.RecordObject(_markerAssetA, "Modify Sync Marker");
        }

        private void SetMarkerAssetDirtyA()
        {
            _markerAssetA.EnsureMarkersInAscendOrder();
            EditorUtility.SetDirty(_markerAssetA);
        }

        private void RecordMarkerAssetModificationUndoB()
        {
            Undo.RecordObject(_markerAssetB, "Modify Sync Marker");
        }

        private void SetMarkerAssetDirtyB()
        {
            _markerAssetB.EnsureMarkersInAscendOrder();
            EditorUtility.SetDirty(_markerAssetB);
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