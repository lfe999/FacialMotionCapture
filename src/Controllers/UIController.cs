using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Extensions;
using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LFE.FacialMotionCapture.Controllers {
    public class UIController {

        private const string DEVICE_FACECAP = "Face Cap (iOS)";
        private const string DEVICE_LIVEFACE = "LIVE Face (iOS)";

        private List<UIDynamic> groupUiElements = new List<UIDynamic>();
        private Dictionary<int, UIDynamicSlider> shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        private UIDynamicToggle recordButton;
        private UIDynamicTextField recordMessage;


        public Plugin Plugin { get; private set; }
        public JSONStorableString StorableRecordingMessage;
        public JSONStorableBool StorableIsRecording;
        public Dictionary<string, JSONStorableBool> StorableIsGroupEnabled = new Dictionary<string, JSONStorableBool>();
        public Dictionary<int, JSONStorableFloat> StorableBlendShapeStrength = new Dictionary<int, JSONStorableFloat>();
        public JSONStorableStringChooser StorableDeviceType;
        public JSONStorableStringChooser StorableServerIp;
        public JSONStorableString StorableClientIp;
        public UIController(Plugin plugin)
        {
            Plugin = plugin;
            InitializeStorables();
            RenderUI();
        }

        private void InitializeStorables() {
            var clients = new List<string> {
                "",
                DEVICE_LIVEFACE,
                DEVICE_FACECAP
            };
            StorableDeviceType = new JSONStorableStringChooser("Device", clients, Plugin.SettingsController.GetDevice() ?? "", "Choose Device");

            var ips = Plugin.DeviceController.IPEndPoints.Select(x => x.ToString()).ToList();
            StorableServerIp = new JSONStorableStringChooser("Server IP", ips, Plugin.SettingsController.GetLocalServerIpAddress() ?? "", "Server IP");

            var ipAddress = string.IsNullOrEmpty(Plugin.SettingsController.GetIpAddress()) ? "Enter IP address" : Plugin.SettingsController.GetIpAddress();
            StorableClientIp = new JSONStorableString("IP Address", ipAddress);

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

        private UIDynamicPopup _clientsChooser;
        private UIDynamicPopup _localIpsChooser;
        private UIDynamicTextField _targetValuesTextField;
        private UIDynamicButton _serverConnectUi;
        private UIDynamicTextField _instructions;
        private void ClearUI() {
            if(_clientsChooser != null) {
                Plugin.RemovePopup(_clientsChooser);
            }
            if(_localIpsChooser != null) {
                Plugin.RemovePopup(_localIpsChooser);
            }
            if(_targetValuesTextField != null) {
                Plugin.RemoveTextField(_targetValuesTextField);
            }
            if(_serverConnectUi != null) {
                Plugin.RemoveButton(_serverConnectUi);
            }
            if(_instructions != null) {
                Plugin.RemoveTextField(_instructions);
            }

            ClearBlendShapeUI();
        }

        private void RerenderUI() {
            ClearUI();
            RenderUI();
        }

        private void RenderUI() {
            _clientsChooser = Plugin.CreatePopup(StorableDeviceType);
            _clientsChooser.popup.onValueChangeHandlers += (string val) => {
                Plugin.SettingsController.SetDevice(val);
                Plugin.SettingsController.Save();
                Plugin.DeviceController.Destroy();
                RerenderUI();
            };

            if(StorableDeviceType.val == DEVICE_FACECAP) {
                _localIpsChooser = Plugin.CreatePopup(StorableServerIp);
                _localIpsChooser.height = _clientsChooser.height;
                _localIpsChooser.popup.onValueChangeHandlers += (string val) => {
                    var ip = val.ToIPEndPoint();
                    if(ip != null) {
                        Plugin.SettingsController.SetLocalServerIpAddress(val);
                        Plugin.SettingsController.Save();
                    }
                    Plugin.DeviceController.Destroy();
                    RerenderUI();
                };
            }

            if(StorableDeviceType.val == DEVICE_LIVEFACE) {
                // enter ip address textfield
                _targetValuesTextField = Plugin.CreateTextField(StorableClientIp);
                _targetValuesTextField.backgroundImage.color = Color.white;
                _targetValuesTextField.SetLayoutHeight(_clientsChooser.height);
                var targetValuesInput = _targetValuesTextField.gameObject.AddComponent<InputField>();
                _targetValuesTextField.height = _clientsChooser.height;
                targetValuesInput.textComponent = _targetValuesTextField.UItext;
                targetValuesInput.textComponent.fontSize = 40;
                targetValuesInput.text = StorableClientIp.val;
                targetValuesInput.image = _targetValuesTextField.backgroundImage;
                targetValuesInput.onValueChanged.AddListener((string value) => {
                    StorableClientIp.val = value;
                    var ip = value.ToIPAddress();
                    if(ip != null) {
                        Plugin.SettingsController.SetIpAddress(value);
                        Plugin.SettingsController.Save();
                    }
                });
                targetValuesInput.onEndEdit.AddListener((string value) => {
                    var ip = value.ToIPAddress();
                    if(ip == null) {
                        StorableClientIp.val = "Enter IP Address";
                    }
                    Plugin.DeviceController.Destroy();
                    RerenderUI();
                });
            }

            // start/stop server button
            var deviceConfigLooksValid =
                (StorableDeviceType.val == DEVICE_FACECAP && StorableServerIp.val.ToIPEndPoint() != null) ||
                (StorableDeviceType.val == DEVICE_LIVEFACE && StorableClientIp.val.ToIPAddress() != null);
            if(deviceConfigLooksValid) {
                var isConnected = Plugin?.DeviceController?.IsConnected() ?? false;
                if(isConnected) {
                    _serverConnectUi = Plugin.CreateButton(StorableDeviceType.val == DEVICE_FACECAP ? "Stop Server" : "Disconnect");
                    _serverConnectUi.buttonColor = Color.red;
                }
                else {
                    _serverConnectUi = Plugin.CreateButton(StorableDeviceType.val == DEVICE_FACECAP ? "Start Server" : "Connect");
                    _serverConnectUi.buttonColor = Color.green;
                }
            }
            else {
                _serverConnectUi = Plugin.CreateButton(StorableDeviceType.val == DEVICE_FACECAP ? "Start Server" : "Connect");
                _serverConnectUi.buttonColor = Color.grey;
                _serverConnectUi.button.enabled = false;
            }
            _serverConnectUi.height = _clientsChooser.height;
            _serverConnectUi.button.onClick.AddListener(() =>
            {
                // stop the server if it is started
                if(Plugin.DeviceController.IsConnected())
                {
                    try {
                        Plugin.DeviceController.Destroy();
                        StorableIsRecording.val = false;
                    }
                    catch(Exception e) {
                        SuperController.LogError(e.ToString());
                    }
                    RerenderUI();
                }
                // start the server if it is not started yet
                else
                {
                    try {
                        if(StorableDeviceType.val == DEVICE_FACECAP) {
                            Plugin.DeviceController = new DeviceController(
                                Plugin,
                                new BannaflakFaceCapClient(StorableServerIp.val.ToIPEndPoint(), Plugin)
                            );
                        }
                        else if(StorableDeviceType.val == DEVICE_LIVEFACE) {
                            Plugin.DeviceController = new DeviceController(
                                Plugin,
                                new RealIllusionLiveFaceClient(StorableClientIp.val.ToIPAddress(), Plugin)
                            );
                        }
                        Plugin.DeviceController.Connect();
                        RerenderUI();
                    }
                    catch(FormatException ex) {
                        SuperController.LogError($"{ex.Message}: {StorableClientIp.val}");
                        Plugin.DeviceController.Destroy();
                        RerenderUI();
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
                        RerenderUI();
                        return;
                    }
                }
            });

            _instructions = Plugin.CreateTextField(new JSONStorableString("", ""), rightSide: true);
            _instructions.height = (_clientsChooser.height * 3) + (10 * 3);
            if(StorableDeviceType.val == DEVICE_FACECAP) {
                _instructions.text =
                    "Face Cap by BannaFlak (iOS)\n" +
                    "https://apps.apple.com/us/app/id1373155478\n\n" +
                    "<b>App</b>: Click 'Go Live' and 'Connect/Disconnect'\n" +
                    "<b>VaM Plugin</b>: Click 'Start Server'\n" +
                    "<b>App</b>: Enter IP/Port from <b>VaM Plugin</b>";
            }
            else if(StorableDeviceType.val == DEVICE_LIVEFACE) {
                _instructions.text =
                    "LIVE Face by RealIllusion (iOS)\n" +
                    "https://apps.apple.com/us/app/id1357551209\n\n" +
                    "<b>App</b>: Note IP address on screen\n" +
                    "<b>VaM Plugin</b>: Enter IP from <b>App</b>\n" +
                    "<b>VaM Plugin</b>: Click 'Connect'";
            }

            if(Plugin.DeviceController?.IsConnected() ?? false) {
                CreateBlendShapeUI();
            }
        }

        private void CreateBlendShapeUI() {
            ClearBlendShapeUI();

            recordButton = Plugin.CreateToggle(StorableIsRecording);
            recordButton.height = 75;
            recordButton.label = GetRecordingLabel();

            recordMessage = Plugin.CreateTextField(StorableRecordingMessage, rightSide: true);
            recordMessage.SetLayoutHeight(75);

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
            if(StorableIsRecording.val) { return "Recording"; }
            else { return "Recording"; }
        }

        public bool IsUiActive() {
            return _targetValuesTextField.isActiveAndEnabled;
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
