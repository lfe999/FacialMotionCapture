using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LFE.FacialMotionCapture.Main {

	public class Plugin : MVRScript {

        private const string SAVE_FILE = "Saves\\lfe_facialmotioncapture.json";
        private IFacialCaptureClient client;

        public Atom SelectedPerson;
        public FreeControllerV3 EyeController;
        public GenerateDAZMorphsControlUI MorphControl;

        public SimpleJSON.JSONClass Settings;

        public List<MorphFrame> RecordedFrames = new List<MorphFrame>();
        public Dictionary<int, BlendShapeReceivedEventArgs> ShapeEventsForFrame = new Dictionary<int, BlendShapeReceivedEventArgs>();

		void Start() {
            RecordedFrames = new List<MorphFrame>();
            recording = false;
            recordedFrameId = 0;

            if (containingAtom && containingAtom.type == "Person")
            {
                SelectedPerson = containingAtom;
                MorphControl = SelectedPerson.GetMorphsControlUI();
                EyeController = SelectedPerson.GetStorableByID("eyeTargetControl") as FreeControllerV3;
                Settings = SettingsLoad();
            }
            else
            {
                SuperController.LogError("This plugin must be placed on a person", false);
                return;
            }

            InitPluginUI();
		}

        private DAZMorph RunMorphChange(BlendShapeReceivedEventArgs item) {
            var morph = GetMorph(MapShapeToMorph(item.Shape));
            UIDynamicSlider sliderUI;
            if(morph != null) {
                // update the morph
                morph.morphValueAdjustLimits = item.Value * shapeMultipliers[item.Shape.Id].val;

                // update slider color in the UI
                shapeMultiplierSliders.TryGetValue(item.Shape.Id, out sliderUI);
                if(sliderUI != null) {
                    sliderUI.slider.image.color = Color.Lerp(Color.white, Color.green, Math.Abs(item.Value));
                }
                return morph;
            }
            else {
                shapeMultiplierSliders.TryGetValue(item.Shape.Id, out sliderUI);
                if(sliderUI != null) {
                    sliderUI.slider.image.color = Color.Lerp(Color.white, Color.black, Math.Abs(item.Value));
                }
                return null;
            }
        }

        int recordedFrameId = 0;
        bool recording = false;
        float firstDeltaTime = 0;
		void FixedUpdate() {
            Dictionary<int, BlendShapeReceivedEventArgs> changes;
            lock(ShapeEventsForFrame) {
                changes = new Dictionary<int, BlendShapeReceivedEventArgs>(ShapeEventsForFrame);
                ShapeEventsForFrame.Clear();
            }

            foreach(var change in changes) {
                var changedMorph = RunMorphChange(change.Value);
                if(recording) {
                    if(changedMorph != null) {
                        lock(RecordedFrames) {
                            if(RecordedFrames.Count == 0) {
                                firstDeltaTime = Time.deltaTime;
                            }
                            RecordedFrames.Add(new MorphFrame {
                                Number = recordedFrameId + 1,
                                MorphName = changedMorph.displayName,
                                Value = changedMorph.morphValue
                            });
                        }
                    }
                }
            }

            if(recording) {
                recordedFrameId++;
            }
		}

		void OnDestroy() {
            client?.Disconnect();
            client = null;
		}

        // -------------------------------------------------------------
        // -------------------------------------------------------------
        // -------------------------------------------------------------

        private string SaveAndClearRecording() {
            recording = false;

            string recordingId = DateTime.Now.ToString("yyMMddTHHmmss");

            var savedir = "Saves\\animations\\mocap";
            var savefile = $"{savedir}\\face_{recordingId}.json";
            if(!MVR.FileManagementSecure.FileManagerSecure.DirectoryExists(savedir)) {
                MVR.FileManagementSecure.FileManagerSecure.CreateDirectory(savedir);
            }

            var framesByMorph = new Dictionary<string, List<MorphFrame>>();
            int maxFrameNumber = 0;
            float frameDuration = firstDeltaTime;
            lock(RecordedFrames) {
                recordedFrameId = 0;

                if(RecordedFrames.Count <= 0) {
                    return null;
                }

                maxFrameNumber = RecordedFrames.Max(f => f.Number);

                framesByMorph = RecordedFrames
                    .GroupBy(x => x.MorphName)
                    .ToDictionary(x => x.Key, x => x.ToList());

                RecordedFrames.Clear();
            }

            var animation = new SimpleJSON.JSONClass();
            animation["Speed"] = "1";
            animation["InterpolationTimeout"] = "0.25"; // ??
            animation["InterpolationSpeed"] = "1"; // ??
            animation["AtomType"] = "Person";
            animation["Clips"] = new SimpleJSON.JSONArray();

            var animationClip = new SimpleJSON.JSONClass();
            animationClip["AnimationName"] = $"Mocap - {recordingId}";
            animationClip["AnimationLength"] = (maxFrameNumber * frameDuration).ToString();
            animationClip["BlendDuration"] = "0.25"; // ??
            animationClip["Loop"] = "1";
            animationClip["Transition"] = "0"; // ??
            animationClip["EnsureQuaternionContinuity"] = "1";
            animationClip["Controllers"] = new SimpleJSON.JSONArray();
            animationClip["FloatParams"] = new SimpleJSON.JSONArray();

            foreach(var morphFrames in framesByMorph) {
                var morphName = morphFrames.Key;
                var frames = morphFrames.Value;

                var floatParam = new SimpleJSON.JSONClass();
                floatParam["Storable"] = "geometry";
                floatParam["Name"] = morphName;
                floatParam["Value"] = new SimpleJSON.JSONArray();
                foreach(var frame in frames) {
                    var jsonEntry = new SimpleJSON.JSONClass();
                    jsonEntry["t"] = ((frame.Number - 1) * frameDuration).ToString(); // consider each frame as 0.1
                    jsonEntry["v"] = frame.Value.ToString();
                    jsonEntry["ti"] = "0";
                    jsonEntry["to"] = "0";
                    jsonEntry["c"] = "0";

                    floatParam["Value"].Add(jsonEntry);
                }

                animationClip["FloatParams"].Add(floatParam);
            }

            animation["Clips"].Add(animationClip);

            SuperController.singleton.SaveJSON(animation, savefile);

            return savefile;
        }

        private void SettingsSave(SimpleJSON.JSONClass settings) {
            SuperController.singleton.SaveJSON(settings, SAVE_FILE);
        }

        private SimpleJSON.JSONClass SettingsLoad() {

            SimpleJSON.JSONClass settings;
            if(MVR.FileManagementSecure.FileManagerSecure.FileExists(SAVE_FILE, onlySystemFiles: true)) {
                settings = SuperController.singleton.LoadJSON(SAVE_FILE)?.AsObject;
            }
            else {
                settings = SuperController.singleton.LoadJSON($"{GetPluginPath()}defaults.json")?.AsObject;
            }

            return settings;
        }

        private Dictionary<int, JSONStorableFloat> shapeMultipliers = new Dictionary<int, JSONStorableFloat>();
        private Dictionary<int, UIDynamicSlider> shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        private UIDynamicButton recordButton;
        private UIDynamicTextField recordMessage;
        private void InitPluginUI() {

            string ipAddress = "Enter IP for Live Face app";
            if(!string.IsNullOrEmpty(Settings["clientIp"])) {
                ipAddress = Settings["clientIp"].Value;
            }

            var ipAddressStorable = new JSONStorableString("IP Address", ipAddress);

            // enter ip address textfield
            var targetValuesTextField = CreateTextField(ipAddressStorable);
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
                Settings["clientIp"] = value;
                SettingsSave(Settings);
            });


            // start/stop server button
            var serverConnectUi = CreateButton("Connect to LIVE Face", rightSide: true);
            serverConnectUi.buttonColor = Color.green;
            serverConnectUi.height = 50;
            serverConnectUi.button.onClick.AddListener(() =>
            {
                // stop the server if it is started
                if(client != null)
                {
                    try {
                        client.Disconnect();
                        client = null;
                        serverConnectUi.label = "Connect to LIVE Face";
                        serverConnectUi.buttonColor = Color.green;
                        StopRecording();
                    }
                    catch(Exception e) {
                        SuperController.LogError(e.ToString());
                    }
                    DestroyMorphMultipliers();
                }
                // start the server if it is not started yet
                else
                {
                    try {
                        client?.Disconnect();
                        client = new RealIllusionLiveFaceClient(ipAddressStorable.val, this);
                        client.BlendShapeReceived += (sender, args) => {
                            lock(ShapeEventsForFrame) {
                                ShapeEventsForFrame[args.Shape.Id] = args;
                            }
                        };

                        client.Connect();
                        serverConnectUi.label = "Disconnect";
                        serverConnectUi.buttonColor = Color.red;

                        CreateMorphMultipliers();
                    }
                    catch(FormatException ex) {
                        SuperController.LogError($"{ex.Message}: {ipAddressStorable.val}");
                        DestroyMorphMultipliers();
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
                        client?.Disconnect();
                        DestroyMorphMultipliers();
                        return;
                    }
                }
            });
        }

        private void StopRecording() {
            recording = false;
            var saved = SaveAndClearRecording();
            if(saved != null && recordMessage != null) {
                recordMessage.text = $"Saved {saved}";
            }

            if(recordButton != null) {
                recordButton.label = "Start Recording";
            }
        }

        private void StartRecording() {
            recording = true;
            if(recordButton != null) {
                recordButton.label = "Stop Recording";
            }
            if(recordMessage != null) {
                recordMessage.text = "recording...";
            }
        }

        private void CreateMorphMultipliers() {

            recordMessage = CreateTextField(new JSONStorableString("recordMessage", ""));
            recordMessage.SetLayoutHeight(75);

            recordButton = CreateButton("Start Recording", rightSide: true);
            recordButton.height = 75;
            recordButton.button.onClick.AddListener(() =>
            {
                if(recording) {
                    StopRecording();
                }
                else {
                    StartRecording();
                }
            });


            foreach(var name in CBlendShape.Names()) {
                var shapeId = CBlendShape.NameToId(name) ?? 0;
                var multiplier = 1.0f;
                if(Settings["mappings"]?.AsObject[name]?.AsObject != null) {
                    multiplier = Settings["mappings"][name]["strength"].AsFloat;
                }
                shapeMultipliers[shapeId] = new JSONStorableFloat(
                    $"{name} Strength Multiplier",
                    multiplier,
                    (float value) => {
                        Settings["mappings"][name]["strength"] = new SimpleJSON.JSONData(value);
                        SettingsSave(Settings);
                    },
                    -10, 10, true, true
                );
                shapeMultiplierSliders[shapeId] = CreateSlider(shapeMultipliers[shapeId], rightSide: (shapeId % 2 == 0));
            }
        }

        private void DestroyMorphMultipliers() {
            RemoveTextField(recordMessage);
            RemoveButton(recordButton);

            foreach(var item in shapeMultiplierSliders){
                RemoveSlider(item.Value);
            }
            foreach(var item in shapeMultipliers){
                RemoveSlider(item.Value);
            }
            shapeMultipliers = new Dictionary<int, JSONStorableFloat>();
            shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        }

        private string MapShapeToMorph(BlendShape shape) {
            if(shape == null) {
                return null;
            }

            if(Settings["mappings"].AsObject.HasKey(shape.Name)) {
                return Settings["mappings"][shape.Name]["morph"].Value;
            }
            return null;
        }

        private DAZMorph GetMorph(string name) {
            if(name == null) {
                return null;
            }
            return MorphControl?.GetMorphByDisplayName(name);
        }

        public string GetPluginPath()
        {
            string id = this.name.Substring(0, this.name.IndexOf('_'));
            string filename = this.manager.GetJSON()["plugins"][id].Value;
            string path = filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            return path;
        }
	}

    public static class ComponentExtensions {
        public static void SetLayoutHeight(this Component component, float height)
        {
            var layoutElement = component.GetComponent<LayoutElement>();
            if(layoutElement != null) {
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = height;
            }
        }
    }

    public static class AtomExtensions
    {
        /// <summary>
        /// Extension method to get the morph control ui on an atom
        ///
        /// Throws InvalidOperationException if the atom doesn't make sense
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static GenerateDAZMorphsControlUI GetMorphsControlUI(this Atom atom)
        {
            JSONStorable geometry = atom.GetStorableByID("geometry");
            if (geometry == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            DAZCharacterSelector dcs = geometry as DAZCharacterSelector;
            if (dcs == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            return dcs.morphsControlUI;
        }
    }

    public class MorphFrame {
        public int Number { get; set; }
        public string MorphName { get; set; }
        public float Value { get; set; }
    }
}
