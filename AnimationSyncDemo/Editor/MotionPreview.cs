using System;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GBG.AnimationSyncDemo.Editor
{
    public class MotionPreview
    {
        private object _avatarPreviewObj;

        private Motion _motion;
        private AnimatorController _controller;
        private AnimatorStateMachine _stateMachine;
        private AnimatorState _state;
        private bool _prevIKOnFeet;
        private bool _controllerIsDirty;
        private Action _controllerDirtyDelegate;
        private Delegate _onPreviewAvatarChangedDelegate;


        public void Initialize(Motion motion)
        {
            _motion = motion;

            if (_avatarPreviewObj == null)
            {
                _avatarPreviewObj = Reflector.CreateInstance(typeof(UnityEditor.Editor).Assembly,
                    "UnityEditor.AvatarPreview", new object[] { null, _motion });
                _avatarPreviewObj.Set("OnAvatarChangeFunc", GetOnPreviewAvatarChangedDelegate());
                _avatarPreviewObj.Invoke("ResetPreviewFocus", null);
                _prevIKOnFeet = (bool)_avatarPreviewObj.Get("IKOnFeet");
            }

            ResetStateMachine();
        }

        public void OnDisable()
        {
            ClearStateMachine();

            if (_avatarPreviewObj != null)
            {
                _avatarPreviewObj.Invoke("OnDisable", null);
                _avatarPreviewObj = null;
            }
        }

        public float GetCurrentTime()
        {
            if (_avatarPreviewObj == null)
            {
                return 0;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            return (float)timeControlObj.Get("currentTime");
        }

        public void SetCurrentTime(float time)
        {
            if (_avatarPreviewObj == null)
            {
                return;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            timeControlObj.Set("currentTime", time);
        }

        public float GetNormalizedTime()
        {
            if (_avatarPreviewObj == null)
            {
                return 0;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            return (float)timeControlObj.Get("normalizedTime");
        }

        public void SetNormalizedTime(float time)
        {
            if (_avatarPreviewObj == null)
            {
                return;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            timeControlObj.Set("normalizedTime", time);
        }

        public void SetNextNormalizedTime(float time)
        {
            if (_avatarPreviewObj == null)
            {
                return;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            timeControlObj.Set("nextCurrentTime", time);
        }

        public void ResetStateMachine()
        {
            ClearStateMachine();
            CreateStateMachine();
        }

        private void CreateStateMachine()
        {
            if (_avatarPreviewObj == null) return;

            var previewAnimator = _avatarPreviewObj.Get("Animator") as Animator;
            if (!previewAnimator) return;

            if (_controller == null)
            {
                _controller = new AnimatorController();
                _controller.Set("pushUndo", false);
                _controller.AddLayer("preview");
                _stateMachine = _controller.layers[0].stateMachine;
                _stateMachine.Set("pushUndo", false);

                _state = _stateMachine.AddState("preview");
                _state.Set("pushUndo", false);
                _state.motion = _motion;
                _state.iKOnFeet = (bool)_avatarPreviewObj.Get("IKOnFeet");

                _state.hideFlags = HideFlags.HideAndDontSave;
                _controller.hideFlags = HideFlags.HideAndDontSave;
                _stateMachine.hideFlags = HideFlags.HideAndDontSave;

                AnimatorController.SetAnimatorController(previewAnimator, _controller);
                _controller.Set("OnAnimatorControllerDirty", GetControllerDirtyDelegate());

                _controllerIsDirty = false;
            }

            var effectiveAnimatorController = Reflector.Invoke(typeof(AnimatorController), null,
                "GetEffectiveAnimatorController", new object[] { previewAnimator });
            if (!ReferenceEquals(effectiveAnimatorController, _controller))
                AnimatorController.SetAnimatorController(previewAnimator, _controller);
        }

        private void ClearStateMachine()
        {
            if (_avatarPreviewObj != null)
            {
                var previewAnimator = _avatarPreviewObj.Get("Animator") as Animator;
                if (previewAnimator)
                {
                    AnimatorController.SetAnimatorController(previewAnimator, null);
                }
            }

            if (_controller)
            {
                _controller.Set("OnAnimatorControllerDirty", null);
            }

            Object.DestroyImmediate(_controller);
            Object.DestroyImmediate(_state);
            _stateMachine = null;
            _controller = null;
            _state = null;
        }


        protected virtual void ControllerDirty()
        {
            _controllerIsDirty = true;
        }

        private void OnPreviewAvatarChanged()
        {
            ResetStateMachine();
        }

        private Action GetControllerDirtyDelegate()
        {
            if (_controllerDirtyDelegate == null)
            {
                _controllerDirtyDelegate = ControllerDirty;
            }

            return _controllerDirtyDelegate;
        }

        private Delegate GetOnPreviewAvatarChangedDelegate()
        {
            if (_onPreviewAvatarChangedDelegate == null)
            {
                //m_onPreviewAvatarChangedDelegate = OnPreviewAvatarChanged;
                var delegateType = Reflector.GetType(typeof(UnityEditor.Editor).Assembly, "UnityEditor.AvatarPreview+OnAvatarChange");
                _onPreviewAvatarChangedDelegate = this.CreateDelegate(nameof(OnPreviewAvatarChanged), delegateType);
            }

            return _onPreviewAvatarChangedDelegate;
        }


        public bool HasPreviewGUI()
        {
            return true;
        }

        public void OnPreviewSettings()
        {
            if (_avatarPreviewObj == null)
            {
                return;
            }

            _avatarPreviewObj.Invoke("DoPreviewSettings", null);
        }

        public void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (_avatarPreviewObj == null)
            {
                return;
            }

            UpdateAvatarState();

            _avatarPreviewObj.Invoke("DoAvatarPreview", new object[] { r, background });
        }

        private void UpdateAvatarState()
        {
            if (UnityEngine.Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (_avatarPreviewObj.Get("PreviewObject") == null || _controllerIsDirty)
            {
                _avatarPreviewObj.Invoke("ResetPreviewInstance", null);
                if (_avatarPreviewObj.Get("PreviewObject") != null)
                {
                    ResetStateMachine();
                }
            }

            var previewAnimator = _avatarPreviewObj.Get("Animator") as Animator;
            if (!previewAnimator)
            {
                return;
            }

            var timeControlObj = _avatarPreviewObj.Get("timeControl");
            var ikOnFeet = (bool)_avatarPreviewObj.Get("IKOnFeet");
            if (_prevIKOnFeet != ikOnFeet)
            {
                _prevIKOnFeet = ikOnFeet;
                Vector3 prevPos = previewAnimator.rootPosition;
                Quaternion prevRotation = previewAnimator.rootRotation;
                ResetStateMachine();
                previewAnimator.Update(GetCurrentTime());
                previewAnimator.Update(0); // forces deltaPos/Rot to 0,0,0
                previewAnimator.rootPosition = prevPos;
                previewAnimator.rootRotation = prevRotation;
            }

            timeControlObj.Set("loop", true);

            float stateLength = 1.0f;
            float stateTime = 0.0f;

            if (previewAnimator.layerCount > 0)
            {
                AnimatorStateInfo stateInfo = previewAnimator.GetCurrentAnimatorStateInfo(0);
                stateLength = stateInfo.length;
                stateTime = stateInfo.normalizedTime;
            }

            timeControlObj.Set("startTime", 0f);
            timeControlObj.Set("stopTime", stateLength);
            timeControlObj.Invoke("Update", null);

            float deltaTime = (float)timeControlObj.Get("deltaTime");

            if (!_motion || !_motion.isLooping)
            {
                if (stateTime >= 1.0f)
                {
                    deltaTime -= stateLength;
                }
                else if (stateTime < 0.0f)
                {
                    deltaTime += stateLength;
                }
            }

            previewAnimator.Update(deltaTime);
        }
    }
}