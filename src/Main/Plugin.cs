// #define LFE_DEBUG

using LFE.FacialMotionCapture.Controllers;
using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Models;
using LFE.FacialMotionCapture.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

namespace LFE.FacialMotionCapture.Main {

	public class Plugin : MVRScript {

        public SettingsController SettingsController;
        public RecordingController RecordingController;
        public UIController UIController;
        public DeviceController DeviceController;

        public Dictionary<int, BlendShapeReceivedEventArgs> ShapeEventsForFrame = new Dictionary<int, BlendShapeReceivedEventArgs>();

        public FreeControllerV3 HeadController;
        public Quaternion OriginalHeadRotation;

		public override void Init() {
#if LFE_DEBUG
            SuperController.LogMessage($"Init()");
#endif

            if (!containingAtom || containingAtom.type != "Person")
            {
                SuperController.LogError("This plugin must be placed on a person", false);
                enabled = false;
                return;
            }

            HeadController = containingAtom.freeControllers.Single(fc => fc.name == "headControl");
            OriginalHeadRotation = HeadController.transform.rotation;

            DeviceController = new DeviceController(this, null);
            SettingsController = new SettingsController(this);
            RecordingController = new RecordingController(this);
            UIController = new UIController(this);
		}

        // loading scene
        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
#if LFE_DEBUG
            SuperController.LogMessage($"RestoreFromJSON()");
#endif

            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

            try {
                if(jc.HasKey("Settings")) {
                    var settings = jc["Settings"].AsObject;
                    if(settings != null) {
                        SettingsController.LoadFrom(settings);
                        SettingsController.SetDevice(
                            SettingsController.GlobalSettings?.AsObject[SettingsController.DEVICE_KEY]?.Value ?? String.Empty
                        );
                        SettingsController.SetIpAddress(
                            SettingsController.GlobalSettings?.AsObject[SettingsController.CLIENT_IP_KEY]?.Value ?? String.Empty
                        );
                        SettingsController.SetLocalServerIpAddress(
                            SettingsController.GlobalSettings?.AsObject[SettingsController.SERVER_IP_KEY]?.Value ?? String.Empty
                        );
                    }
                }
                if(UIController != null) {
                    foreach(var item in UIController.StorableIsGroupEnabled) {
#if LFE_DEBUG
                        SuperController.LogMessage($"{item}");
#endif
                        if(jc.HasKey(item.Key)) {
                            item.Value.val = jc[item.Key]?.AsBool ?? true;
#if LFE_DEBUG
                            SuperController.LogMessage($"setting group value: {item.Key} to {item.Value.val}");
#endif
                        }
                    }
                }
                return;
            }
            catch(Exception e) {
                SuperController.LogError($"Load settings failed: {e.ToString()}");
            }
        }

        // saving scene
        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false) {
            var json = base.GetJSON(includePhysical, includeAppearance, forceStore);

            try {
                var settings = SettingsController.ToJSONClass();
                if(settings != null) {
                    json["Settings"] = settings;
                    json["Settings"].Remove(SettingsController.DEVICE_KEY);
                    json["Settings"].Remove(SettingsController.CLIENT_IP_KEY);
                    json["Settings"].Remove(SettingsController.SERVER_IP_KEY);
                    needsStore = true;
                }
            }
            catch(Exception e) {
                SuperController.LogError($"Save settings failed: {e.ToString()}");
            }

            return json;
        }

        private Dictionary<int, float> _smoothingVelocities = new Dictionary<int, float>();
		void FixedUpdate() {
            var changes = DeviceController.GetChanges();

            foreach(var change in changes) {
                DAZMorph changedMorph = null;
                JSONStorableFloat changedEye = null;
                Quaternion? changedHeadRotation = null;

                if(!ShapeEnabled(change.Value.Shape)) {
                    continue;
                }

                changedMorph = RunMorphChange(change.Value);
                if(changedMorph != null) {
                    if(RecordingController.IsRecording) {
                        RecordingController.RecordMorphValue(changedMorph.displayName, changedMorph.morphValue);
                    }
                }

                if(changedMorph == null) {
                    changedEye = RunEyeChange(change.Value);
                    if(changedEye != null) {
                        if(RecordingController.IsRecording) {
                            RecordingController.RecordEyeValue(changedEye.name, changedEye.val);
                        }
                    }
                }

                if(changedEye == null) {
                    changedHeadRotation = RunHeadRotationChange(change.Value, OriginalHeadRotation);
                    if(changedHeadRotation.HasValue) {
                        if(RecordingController.IsRecording) {
                            RecordingController.RecordHeadRotationValue(changedHeadRotation.Value);
                        }
                    }
                }
            }

            if(RecordingController.IsRecording) {
                RecordingController.NextFrame();
            }
		}

		void OnDestroy() {
            DeviceController.Disconnect();
            DeviceController = new DeviceController(this, null);
		}

        public void StopRecording() {
            RecordingController.Stop();
            var saved = RecordingController.SaveTimelineAnimation();
            RecordingController.Reset();
            if(saved != null) {
                UIController.SetRecordingMessage($"Saved {saved}");
            }
            UIController.UpdateRecordingButtonText();
        }

        public void StartRecording() {
            RecordingController.Start();
            UIController.SetRecordingMessage($"recording...");
            UIController.UpdateRecordingButtonText();
        }

        private Dictionary<string, DAZMorph> _morphNameLookup = new Dictionary<string, DAZMorph>();
        public DAZMorph GetMorph(string name) {
            if(name == null) {
                return null;
            }
            if(!_morphNameLookup.ContainsKey(name)) {
                _morphNameLookup[name] = containingAtom?.GetMorphsControlUI()?.GetMorphByDisplayName(name);
            }
            return _morphNameLookup[name];
        }

        private JSONStorable _eyePlugin;
        private bool _eyePluginChecked = false;
        public JSONStorable EyePlugin {
            get {
                if(_eyePluginChecked == false && _eyePlugin == null) {
                    _eyePlugin = containingAtom.GetStorableIDs()
                        .Where(id => id.EndsWith("_LFE.EyeGazeControl"))
                        .Select(id => containingAtom.GetStorableByID(id))
                        .FirstOrDefault();
                    _eyePluginChecked = true;
                }
                return _eyePlugin;
            }
        }

        public bool EyePluginInstalled() {
            return EyePlugin != null;
        }

        private DAZMorph RunMorphChange(BlendShapeReceivedEventArgs item) {
            var morph = GetMorph(SettingsController.GetShapeMorph(item?.Shape?.Name));
            if(morph != null) {
                var multiplier = UIController.StorableBlendShapeStrength[item.Shape.Id].val;
                var newValue = item.Value * multiplier;
                var oldValue = morph.morphValue;

                // smoothing strategy
                if(SettingsController.GetSmoothingMultiplier() > 0) {
                    if(!_smoothingVelocities.ContainsKey(item.Shape.Id)) {
                        _smoothingVelocities[item.Shape.Id] = 0f;
                    }
                    float changePct = Mathf.Abs(oldValue - newValue) / Mathf.Abs(multiplier);
                    float velocity = _smoothingVelocities[item.Shape.Id];
                    morph.morphValueAdjustLimits = Mathf.SmoothDamp(oldValue, newValue, ref velocity, Time.fixedDeltaTime * SettingsController.GetSmoothingMultiplier());
                    _smoothingVelocities[item.Shape.Id] = velocity;

                }
                // just set it strategy
                else {
                    morph.morphValueAdjustLimits = newValue;
                }


                UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.green, Math.Abs(item.Value)));
                return morph;
            }
            else {
                UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.black, Math.Abs(item.Value)));
                return null;
            }
        }

        private JSONStorableFloat RunEyeChange(BlendShapeReceivedEventArgs item) {
            if(!EyePluginInstalled()) {
                // the eye plugin is not installed on this person
                return null;
            }

            string paramName = null;
            float paramValue = 0;
            float multiplier = UIController.StorableBlendShapeStrength[item.Shape.Id].val;
            switch(item.Shape.Id) {
                case CBlendShape.EYE_LOOK_DOWN_LEFT:
                    paramName = "lEyeUpDown";
                    paramValue = -30 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_UP_LEFT:
                    paramName = "lEyeUpDown";
                    paramValue = 30 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_IN_LEFT:
                    paramName = "lEyeRightLeft";
                    paramValue = 45 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_OUT_LEFT:
                    paramName = "lEyeRightLeft";
                    paramValue = -45 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_DOWN_RIGHT:
                    paramName = "rEyeUpDown";
                    paramValue = -30 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_UP_RIGHT:
                    paramName = "rEyeUpDown";
                    paramValue = 30 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_IN_RIGHT:
                    paramName = "rEyeRightLeft";
                    paramValue = -45 * item.Value * multiplier;
                    break;
                case CBlendShape.EYE_LOOK_OUT_RIGHT:
                    paramName = "rEyeRightLeft";
                    paramValue = 45 * item.Value * multiplier;
                    break;
                default:
                    break;
            }

            if(paramName != null) {
                var floatParam = EyePlugin.GetFloatJSONParam(paramName);
                if(floatParam != null) {
                    // update eye
                    if(item.Value > 0) {
                        floatParam.val = paramValue;
                    }

                    // update slider color in the UI
                    UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.green, Math.Abs(item.Value)));
                    return floatParam;
                }
                else {
                    UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.black, Math.Abs(item.Value)));
                    return null;
                }
            }

            return null;
        }

        Vector3 _latestHeadRotationValues = Vector3.zero;
        private Quaternion? RunHeadRotationChange(BlendShapeReceivedEventArgs item, Quaternion originalRotation) {
            float multiplier = UIController.StorableBlendShapeStrength[item.Shape.Id].val;
            switch(item.Shape.Id) {
                case CBlendShape.HEAD_ROTATION_RIGHT:
                    _latestHeadRotationValues.y = item.Value * multiplier;
                    break;
                case CBlendShape.HEAD_ROTATION_LEFT:
                    _latestHeadRotationValues.y = item.Value * -1 * multiplier;
                    break;
                case CBlendShape.HEAD_ROTATION_UP:
                    _latestHeadRotationValues.x = item.Value * -1 * multiplier;
                    break;
                case CBlendShape.HEAD_ROTATION_DOWN:
                    _latestHeadRotationValues.x = item.Value * multiplier;
                    break;
                case CBlendShape.HEAD_ROTATION_TILT_LEFT:
                    _latestHeadRotationValues.z = item.Value * multiplier;
                    break;
                case CBlendShape.HEAD_ROTATION_TILT_RIGHT:
                    _latestHeadRotationValues.z = item.Value * -1 * multiplier;
                    break;
                default:
                    return null;
            }

            const int degMult = 90;
            var newRotation = OriginalHeadRotation;
            newRotation *= Quaternion.Euler(new Vector3(1, 0, 0) * _latestHeadRotationValues.x * degMult);
            newRotation *= Quaternion.Euler(new Vector3(0, 1, 0) * _latestHeadRotationValues.y * degMult);
            newRotation *= Quaternion.Euler(new Vector3(0, 0, 1) * _latestHeadRotationValues.z * degMult);

            if(HeadController != null) {
                HeadController.transform.rotation = newRotation;
                UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.green, Math.Abs(item.Value)));
                return newRotation;
            }
            else {
                UIController.SetShapeSliderColor(item.Shape.Id, Color.Lerp(Color.white, Color.black, Math.Abs(item.Value)));
                return null;
            }
        }

        private bool ShapeEnabled(BlendShape shape) {
            var shapeGroup = shape.Group;

            // shape group that this belongs to is disabled?
            if(UIController.StorableIsGroupEnabled.ContainsKey(shapeGroup)) {
                if(UIController.StorableIsGroupEnabled[shapeGroup].val == false) {
                    return false;
                }
            }

            // is the multiplier 0?
            if(UIController.StorableBlendShapeStrength.ContainsKey(shape.Id)) {
                if(UIController.StorableBlendShapeStrength[shape.Id].val == 0) {
                    return false;
                }
            }

            return true;
        }

	}
}
