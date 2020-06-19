using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Extensions;
using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LFE.FacialMotionCapture.Controllers {
    public class UIController {
        private List<UIDynamic> groupUiElements = new List<UIDynamic>();
        private Dictionary<int, UIDynamicSlider> shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        private UIDynamicToggle recordButton;
        private UIDynamicTextField recordMessage;
        private UIDynamicTextField _ipAddress;


        public Plugin Plugin { get; private set; }
        public JSONStorableString StorableRecordingMessage;
        public JSONStorableBool StorableIsRecording;
        public Dictionary<string, JSONStorableBool> StorableIsGroupEnabled = new Dictionary<string, JSONStorableBool>();
        public Dictionary<int, JSONStorableFloat> StorableBlendShapeStrength = new Dictionary<int, JSONStorableFloat>();
        public UIController(Plugin plugin)
        {
            Plugin = plugin;
            InitializeStorables();
            InitPluginUI();
        }

        private void InitializeStorables() {
            StorableRecordingMessage = new JSONStorableString("recordMessage", "");

            StorableIsRecording = new JSONStorableBool("recording", false, (bool value) => {
                if(value) { Plugin.StartRecording(); }
                else { Plugin.StopRecording(); }
            });

            // group is enabled toggles
            foreach(var groupName in CBlendShape.Groups()) {
                StorableIsGroupEnabled[groupName] = new JSONStorableBool($"{groupName}Enabled", true, (bool value) => {
                    CreateBlendShapeUI();
                });
            }

            // blendshape strength values
            foreach(var shapeName in CBlendShape.Names()) {
                var shapeId = CBlendShape.NameToId(shapeName) ?? -1;
                var multiplier = Plugin.SettingsController.GetShapeStrength(shapeName) ?? 1.0f;
                StorableBlendShapeStrength[shapeId] = new JSONStorableFloat(
                    $"{shapeName}",
                    multiplier,
                    (float value) => {
                        Plugin.SettingsController.SetShapeStrength(shapeName, value);
                        Plugin.SettingsController.Save();
                    },
                    -10, 10, true, true
                );
            }
        }

        private void InitPluginUI() {

            var ipAddress = string.IsNullOrEmpty(Plugin.SettingsController.GetIpAddress()) ? "Enter IP for Live Face app" : Plugin.SettingsController.GetIpAddress();
            var ipAddressStorable = new JSONStorableString("IP Address", ipAddress);

            // enter ip address textfield
            var targetValuesTextField = Plugin.CreateTextField(ipAddressStorable);
            targetValuesTextField.backgroundImage.color = Color.white;
            targetValuesTextField.SetLayoutHeight(50);
            var targetValuesInput = targetValuesTextField.gameObject.AddComponent<InputField>();
            targetValuesTextField.height = 50;
            targetValuesInput.textComponent = targetValuesTextField.UItext;
            targetValuesInput.textComponent.fontSize = 40;
            targetValuesInput.text = ipAddressStorable.defaultVal;
            targetValuesInput.image = targetValuesTextField.backgroundImage;
            targetValuesInput.onValueChanged.AddListener((string value) => {
                ipAddressStorable.val = value;
                Plugin.SettingsController.SetIpAddress(value);
                Plugin.SettingsController.Save();
            });

            _ipAddress = targetValuesTextField;

            // start/stop server button
            var serverConnectUi = Plugin.CreateButton("Connect to LIVE Face", rightSide: true);
            serverConnectUi.buttonColor = Color.green;
            serverConnectUi.height = 50;
            serverConnectUi.button.onClick.AddListener(() =>
            {
                // stop the server if it is started
                if(Plugin.DeviceController.IsConnected())
                {
                    try {
                        Plugin.DeviceController.Destroy();
                        serverConnectUi.label = "Connect to LIVE Face";
                        serverConnectUi.buttonColor = Color.green;
                        StorableIsRecording.val = false;
                    }
                    catch(Exception e) {
                        SuperController.LogError(e.ToString());
                    }
                    ClearBlendShapeUI();
                }
                // start the server if it is not started yet
                else
                {
                    try {
                        Plugin.DeviceController = new DeviceController(
                            Plugin,
                            new RealIllusionLiveFaceClient(ipAddressStorable.val, Plugin)
                        );
                        Plugin.DeviceController.Connect();
                        serverConnectUi.label = "Disconnect";
                        serverConnectUi.buttonColor = Color.red;
                        CreateBlendShapeUI();
                    }
                    catch(FormatException ex) {
                        SuperController.LogError($"{ex.Message}: {ipAddressStorable.val}");
                        Plugin.DeviceController.Destroy();
                        ClearBlendShapeUI();
                        return;
                    }
                    catch(Exception e) {
                        if(e.Message.Contains("No connection could be made")) {
                            SuperController.LogError("Unable to connect.  Make sure you have the facial capture client running on your phone.", false);
                        }
                        else if(e.Message.Contains("connected host has failed to respond")) {
                            SuperController.LogError("Connection timeout.  Make sure you have the facial capture client running on your phone.", false);
                        }
                        else {
                            SuperController.LogError(e.ToString());
                        }
                        Plugin.DeviceController.Destroy();
                        ClearBlendShapeUI();
                        return;
                    }
                }
            });
        }

        private void CreateBlendShapeUI() {
            ClearBlendShapeUI();

            recordMessage = Plugin.CreateTextField(StorableRecordingMessage);
            recordMessage.SetLayoutHeight(75);

            recordButton = Plugin.CreateToggle(StorableIsRecording, rightSide: true);
            recordButton.height = 75;
            recordButton.label = GetRecordingLabel();

            foreach(var group in CBlendShape.Groups()) {

                var isEnabledStorable = StorableIsGroupEnabled[group];

                var toggle = Plugin.CreateToggle(isEnabledStorable);
                var space = Plugin.CreateSpacer(rightSide: true);
                space.height = toggle.height;

                groupUiElements.Add(toggle);
                groupUiElements.Add(space);

                if(isEnabledStorable.val) {
                    var i = 0;
                    var shapesInGroup = CBlendShape.IdsInGroup(group).ToList();
                    foreach(var shapeId in shapesInGroup)
                    {
                        shapeMultiplierSliders[shapeId] = Plugin.CreateSlider(StorableBlendShapeStrength[shapeId], rightSide: (i % 2 != 0));
                        i++;
                    }
                    // if there are an odd number of shapes in this group, add a spacer that is the same height
                    // and one of the sliders
                    if(shapesInGroup.Count % 2 != 0 && shapesInGroup.Count > 0) {
                        var spacer = Plugin.CreateSpacer(rightSide: true);
                        spacer.height = shapeMultiplierSliders.FirstOrDefault().Value.height;
                        groupUiElements.Add(spacer);
                    }
                }

            }
        }

        private void ClearBlendShapeUI() {
            if(recordMessage != null) {
                Plugin.RemoveTextField(recordMessage);
            }
            if(recordButton != null) {
                Plugin.RemoveToggle(recordButton);
            }
            foreach(var item in shapeMultiplierSliders){
                Plugin.RemoveSlider(item.Value);
            }
            foreach(var item in groupUiElements) {
                if(item is UIDynamicToggle) {
                    Plugin.RemoveToggle((UIDynamicToggle)item);
                }
                else {
                    Plugin.RemoveSpacer(item);
                }
            }
            shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
            groupUiElements = new List<UIDynamic>();
        }

        public string GetRecordingLabel() {
            if(StorableIsRecording.val) { return "Stop Recording"; }
            else { return "Start Recording"; }
        }

        public bool IsUiActive() {
            return _ipAddress.isActiveAndEnabled;
        }

        private UIDynamicSlider tempSliderUI;
        public void SetShapeSliderColor(int id, Color color) {
            if(IsUiActive()) {
                shapeMultiplierSliders.TryGetValue(id, out tempSliderUI);
                if(tempSliderUI != null) {
                    tempSliderUI.slider.image.color = color;
                }
            }
        }

        public void SetRecordingMessage(string message) {
            if(recordMessage == null) {
                return;
            }
            if(message == null) {
                return;
            }
            recordMessage.text = message;
        }

        public void UpdateRecordingButtonText() {
            if(recordButton == null) {
                return;
            }
            recordButton.label = GetRecordingLabel();
        }
    }
}
