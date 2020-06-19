using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LFE {

	public class EyeGazeControl : MVRScript {

        private const string LOOK_AT_PLAYER = "Player";
        private const string LOOK_AT_TARGET = "Target";


        public Atom Person {
            get { return containingAtom; }
        }

        private JSONStorableStringChooser _realLookMode;
        public JSONStorableStringChooser RealLookMode {
            get {
                if(_realLookMode == null) {
                    var eyes = Person.GetStorableByID("Eyes");
                    if(eyes != null) {
                        _realLookMode = eyes.GetStringChooserJSONParam("lookMode");
                    }
                }
                return _realLookMode;
            }
        }

        public string LookMode {
            get { return EyesRelativeToStorable?.val; }
            set {
                if(EyesRelativeToStorable != null) {
                    EyesRelativeToStorable.val = value;
                }
                if(RealLookMode != null) {
                    RealLookMode.val = "None";
                }
                _currentEyeTarget = null;
            }
        }

        public Transform _currentEyeTarget;
        public Transform CurrentEyeTarget {
            get {
                if(_currentEyeTarget == null) {
                    var mode = LookMode;
                    if(mode == LOOK_AT_TARGET) {
                        var control = Person?.GetStorableByID("eyeTargetControl") as FreeControllerV3;
                        _currentEyeTarget = control?.transform;
                    }
                    else {
                        _currentEyeTarget = CameraTarget.centerTarget.transform;
                    }
                }
                return _currentEyeTarget;
            }
        }

        private DAZBone _leftEye;
        public DAZBone LeftEye {
            get {
                if(_leftEye == null) {
                    _leftEye = Person?.GetStorableByID("lEye") as DAZBone;
                }
                return _leftEye;
            }
        }

        private DAZBone _rightEye;
        public DAZBone RightEye {
            get {
                if(_rightEye == null) {
                    _rightEye = Person?.GetStorableByID("rEye") as DAZBone;
                }
                return _rightEye;
            }
        }

        public JSONStorableStringChooser EyesRelativeToStorable;
        public JSONStorableFloat LEyeUpDownStorable;
        public JSONStorableFloat LEyeRightLeftStorable;
        public JSONStorableFloat REyeUpDownStorable;
        public JSONStorableFloat REyeRightLeftStorable;
        public override void Init() {
			if (containingAtom.type != "Person") {
                SuperController.LogError($"This plugin needs to be put on a 'Person' atom only, not a '{containingAtom.type}'");
                return;
            }

            var targetChoices = new List<string>() {
                LOOK_AT_PLAYER,
                LOOK_AT_TARGET
            };

            // header row 1
            var spacer = CreateSpacer(rightSide: false);

            EyesRelativeToStorable = new JSONStorableStringChooser("Eyes Relative To", targetChoices, LOOK_AT_PLAYER, "Eyes Relative To", (string value) => {
                LookMode = value;
            });
            var eyeChooserPopup = CreatePopup(EyesRelativeToStorable, rightSide: true);
            eyeChooserPopup.height = spacer.height;
            RegisterStringChooser(EyesRelativeToStorable);
            LookMode = LOOK_AT_PLAYER;

            // left eye settings
            LEyeUpDownStorable = new JSONStorableFloat("lEyeUpDown", 0, -30, 30);
            var lEyeUpDownSlider = CreateSlider(LEyeUpDownStorable);
            RegisterFloat(LEyeUpDownStorable);

            LEyeRightLeftStorable = new JSONStorableFloat("lEyeRightLeft", 0, -45, 45);
            var lEyeInOutSlider = CreateSlider(LEyeRightLeftStorable);
            RegisterFloat(LEyeRightLeftStorable);

            // right eye settin
            REyeUpDownStorable = new JSONStorableFloat("rEyeUpDown", 0, -30, 30);
            var rEyeUpDownSlider = CreateSlider(REyeUpDownStorable, rightSide: true);
            RegisterFloat(REyeUpDownStorable);

            REyeRightLeftStorable = new JSONStorableFloat("rEyeRightLeft", 0, -45, 45);
            var rEyeRightLeftSlider = CreateSlider(REyeRightLeftStorable, rightSide: true);
            RegisterFloat(REyeRightLeftStorable);

        }

		private void Update() {
            if(CurrentEyeTarget == null || LEyeUpDownStorable == null || LEyeRightLeftStorable == null || REyeUpDownStorable == null || REyeRightLeftStorable == null) {
                return;
            }

            var lEye = LeftEye;
            var lGazeOffset = LEyeUpDownStorable.val * Mathf.Deg2Rad * Vector3.up + LEyeRightLeftStorable.val * -1 * Mathf.Deg2Rad * Vector3.left;
            var leftLookDirection = CurrentEyeTarget?.position ?? lEye.transform.position;
            var leftNewDirection = leftLookDirection + lGazeOffset;
            lEye.transform.LookAt(leftNewDirection);
            // TODO: figure out how to do maximum left/right to keep eyes from disappearing

            var rEye = RightEye;
            var rGazeOffset = REyeUpDownStorable.val * Mathf.Deg2Rad * Vector3.up + REyeRightLeftStorable.val * -1 * Mathf.Deg2Rad * Vector3.left;
            var rightLookDirection = CurrentEyeTarget?.position ?? rEye.transform.position;
            var rightNewDirection = rightLookDirection + rGazeOffset;
            rEye.transform.LookAt(rightNewDirection);
            // TODO: figure out how to do maximum up/down to keep eyes from disappearing

		}

		private void OnDestroy() {
            _currentEyeTarget = null;
            _leftEye = null;
            _rightEye = null;
		}

    }
}
