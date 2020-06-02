using LFE.FacialMotionCapture.Devices;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LFE.FacialMotionCapture.Main {

	public class Plugin : MVRScript {

        private IFacialCaptureClient client;
        private Queue<BlendShapeReceivedEventArgs> changes = new Queue<BlendShapeReceivedEventArgs>();

        public Atom SelectedPerson;
        public FreeControllerV3 EyeController;
        public GenerateDAZMorphsControlUI MorphControl;

		public override void Init() {
			// TODO: create the UI
		}

		// Start is called once before Update or FixedUpdate is called and after Init()
		void Start() {
            if (containingAtom && containingAtom.type == "Person")
            {
                SelectedPerson = containingAtom;
                MorphControl = SelectedPerson.GetMorphsControlUI();
                EyeController = SelectedPerson.GetStorableByID("eyeTargetControl") as FreeControllerV3;
            }
            else
            {
                SuperController.LogError("This plugin must be placed on a person", false);
                return;
            }

            InitPluginUI();

		}

		// Update is called with each rendered frame by Unity

        private IEnumerable<BlendShapeReceivedEventArgs> AllUpdates(bool latestOnly = false) {
            List<BlendShapeReceivedEventArgs> shapes = new List<BlendShapeReceivedEventArgs>();
            lock(changes){
                shapes = changes.ToList();
                changes.Clear();
            }

            if(latestOnly) {
                var seen = new HashSet<int>();
                for(var i = shapes.Count - 1; i > 0; i--) {
                    var item = shapes[i];
                    if(seen.Contains(item.Shape.Id)) {
                        continue;
                    }
                    seen.Add(item.Shape.Id);
                    yield return item;
                }
                yield break;
            }
            else {
                foreach(var item in shapes) {
                    yield return item;
                }
            }

        }

        private void RunMorphChange(BlendShapeReceivedEventArgs item) {
            var morph = GetMorph(MapShapeToMorph(item.Shape));
            UIDynamicSlider sliderUI;
            if(morph != null) {
                // this will let you set a value outside of the range
                morph.SetValue(item.Value * shapeMultipliers[item.Shape.Id].val);

                // these keep you within the morph range
                // morph.jsonFloat.SetVal(item.Value * shapeMultipliers[item.Shape.Id].val);
                // morph.jsonFloat.val = item.Value * shapeMultipliers[item.Shape.Id].val;

                // update slider color in the UI
                shapeMultiplierSliders.TryGetValue(item.Shape.Id, out sliderUI);
                if(sliderUI != null) {
                    sliderUI.slider.image.color = Color.Lerp(Color.white, Color.green, Math.Abs(item.Value));
                }
            }
            else {
                shapeMultiplierSliders.TryGetValue(item.Shape.Id, out sliderUI);
                if(sliderUI != null) {
                    sliderUI.slider.image.color = Color.Lerp(Color.white, Color.black, Math.Abs(item.Value));
                }
            }
        }

		void Update() {
            // try {
            //     foreach(var item in AllUpdates(latestOnly: false)) {
            //         RunMorphChange(item);
            //     }
            // }
            // catch(Exception e){
            //     SuperController.LogError(e.ToString(), false);
            // }
		}

		void OnDestroy() {
            client?.Disconnect();
            client = null;
		}

        // -------------------------------------------------------------
        // -------------------------------------------------------------
        // -------------------------------------------------------------

        private Dictionary<int, JSONStorableFloat> shapeMultipliers = new Dictionary<int, JSONStorableFloat>();
        private Dictionary<int, UIDynamicSlider> shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        private void InitPluginUI() {

            var ipAddressStorable = new JSONStorableString("IP Address", "Enter IP for Live Face app");
            // var ipAddressStorable = new JSONStorableString("IP Address", "192.168.7.20");
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
                            RunMorphChange(args);
                            // lock(changes) {
                            //     changes.Enqueue(args);
                            // }
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

        private void CreateMorphMultipliers() {
            // experiment with showing morph controls
            foreach(var name in CBlendShape.Names()) {
                var shapeId = CBlendShape.NameToId(name) ?? 0;
                shapeMultipliers[shapeId] = new JSONStorableFloat($"{name} Strength Multiplier", DefaultShapeMultiplier(shapeId), -10, 10, true, true);
                shapeMultiplierSliders[shapeId] = CreateSlider(shapeMultipliers[shapeId], rightSide: (shapeId % 2 == 0));
            }
        }

        private void DestroyMorphMultipliers() {
            foreach(var item in shapeMultiplierSliders){
                RemoveSlider(item.Value);
            }
            foreach(var item in shapeMultipliers){
                RemoveSlider(item.Value);
            }
            shapeMultipliers = new Dictionary<int, JSONStorableFloat>();
            shapeMultiplierSliders = new Dictionary<int, UIDynamicSlider>();
        }

        private float DefaultShapeMultiplier(int id) {
            switch(id) {
                case CBlendShape.MOUTH_TONGUE_OUT:
                case CBlendShape.MOUTH_PRESS_LEFT:
                case CBlendShape.MOUTH_PRESS_RIGHT:
                    return -1;
                default:
                    return 1;
            }
        }

        private string MapShapeToMorph(BlendShape shape) {
            if(shape == null) {
                return null;
            }

            switch(shape.Id) {
                case CBlendShape.BROW_INNER_UP:
                    return "Brow Inner Up";
                case CBlendShape.BROW_DOWN_LEFT:
                    return "Brow Down Left";
                case CBlendShape.BROW_DOWN_RIGHT:
                    return "Brow Down Right";
                case CBlendShape.BROW_OUTER_UP_LEFT:
                    return "Brow Outer Up Left";
                case CBlendShape.BROW_OUTER_UP_RIGHT:
                    return "Brow Outer Up Right";
                case CBlendShape.EYE_BLINK_LEFT:
                    return "Eyes Closed Left";
                case CBlendShape.EYE_BLINK_RIGHT:
                    return "Eyes Closed Right";
                case CBlendShape.EYE_SQUINT_LEFT:
                    return "Eyes Squint Left";
                case CBlendShape.EYE_SQUINT_RIGHT:
                    return "Eyes Squint Right";
                case CBlendShape.EYE_WIDE_LEFT:
                case CBlendShape.EYE_WIDE_RIGHT:
                    // consider AAeyeswide3 ?
                    return null;
                case CBlendShape.CHEEK_PUFF:
                    return "Cheeks Balloon";
                case CBlendShape.CHEEK_SQUINT_LEFT:
                    return "Cheek Flex Left";
                case CBlendShape.CHEEK_SQUINT_RIGHT:
                    return "Cheek Flex Right";
                case CBlendShape.NOSE_SNEER_LEFT:
                case CBlendShape.NOSE_SNEER_RIGHT:
                    return null;
                case CBlendShape.JAW_OPEN:
                    return "Mouth Open Wide 2";
                case CBlendShape.JAW_FORWARD:
                    return "Jaw In-Out";
                case CBlendShape.MOUTH_DIMPLE_LEFT:
                    return "Cheeks Dimple Crease Left";
                case CBlendShape.MOUTH_DIMPLE_RIGHT:
                    return "Cheeks Dimple Crease Right";
                case CBlendShape.MOUTH_FUNNEL:
                    return "OW";
                case CBlendShape.MOUTH_PUCKER:
                    return "Lips Pucker";
                case CBlendShape.MOUTH_LEFT:
                    return "Mouth Side-Side Left";
                case CBlendShape.MOUTH_RIGHT:
                    return "Mouth Side-Side Right";
                case CBlendShape.MOUTH_ROLL_UPPER:
                    return "Lip Top Down";
                case CBlendShape.MOUTH_ROLL_LOWER:
                    return "Lip Botton In";
                case CBlendShape.MOUTH_SHRUG_LOWER:
                    return "Lip Bottom Out";
                case CBlendShape.MOUTH_SHRUG_UPPER:
                    return "Lip Top Up";
                case CBlendShape.MOUTH_SMILE_LEFT:
                    return "Mouth Smile Simple Left";
                case CBlendShape.MOUTH_SMILE_RIGHT:
                    return "Mouth Smile Simple Right";
                case CBlendShape.MOUTH_FROWN_LEFT:
                case CBlendShape.MOUTH_FROWN_RIGHT:
                case CBlendShape.MOUTH_UPPER_LEFT:
                case CBlendShape.MOUTH_UPPER_RIGHT:
                case CBlendShape.MOUTH_LOWER_DOWN_LEFT:
                case CBlendShape.MOUTH_LOWER_DOWN_RIGHT:
                case CBlendShape.MOUTH_STRETCH_LEFT:
                case CBlendShape.MOUTH_STRETCH_RIGHT:
                    return null;
                case CBlendShape.MOUTH_PRESS_LEFT:
                    return "Mouth Narrow Left";
                case CBlendShape.MOUTH_PRESS_RIGHT:
                    return "Mouth Narrow Right";
                case CBlendShape.MOUTH_TONGUE_OUT:
                    return "Tongue In-Out";
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

        // Get path prefix of the package that contains our plugin.
        public string GetPackagePath()
        {
            string filename = this.GetPluginPath();
            int idx = filename.IndexOf(":/");
            string path = filename;
            if (idx >= 0)
                path = filename.Substring(0, idx + 2);
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
}
