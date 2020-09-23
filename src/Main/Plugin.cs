using LFE.FacialMotionCapture.Controllers;
using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Models;
using LFE.FacialMotionCapture.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFE.FacialMotionCapture.Main {

	public class Plugin : MVRScript {

        public SettingsController SettingsController;
        public RecordingController RecordingController;
        public UIController UIController;
        public DeviceController DeviceController;

        public Dictionary<int, BlendShapeReceivedEventArgs> ShapeEventsForFrame = new Dictionary<int, BlendShapeReceivedEventArgs>();

		void Start() {

            if (!containingAtom || containingAtom.type != "Person")
            {
                SuperController.LogError("This plugin must be placed on a person", false);
                enabled = false;
                return;
            }

            DeviceController = new DeviceController(this, null);
            SettingsController = new SettingsController(this);
            RecordingController = new RecordingController(this);
            UIController = new UIController(this);
		}

		void FixedUpdate() {
            var changes = DeviceController.GetChanges();

            foreach(var change in changes) {
                if(!ShapeEnabled(change.Value.Shape)) {
                    continue;
                }

                var changedMorph = RunMorphChange(change.Value);
                if(changedMorph != null) {
                    if(RecordingController.IsRecording) {
                        RecordingController.RecordMorphValue(changedMorph.displayName, changedMorph.morphValue);
                    }
                }

                if(changedMorph == null) {
                    var changedEye = RunEyeChange(change.Value);
                    if(changedEye != null) {
                        if(RecordingController.IsRecording) {
                            RecordingController.RecordEyeValue(changedEye.name, changedEye.val);
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
                morph.morphValueAdjustLimits = item.Value * UIController.StorableBlendShapeStrength[item.Shape.Id].val;
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
