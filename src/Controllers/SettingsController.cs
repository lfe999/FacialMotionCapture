using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Extensions;

namespace LFE.FacialMotionCapture.Controllers {

    public class SettingsController {

        private SimpleJSON.JSONClass _settings;
        private Plugin _plugin;

        public string DefaultSettingsFile { get; private set; }
        public string SettingsFile { get; private set; }


        public SettingsController(Plugin plugin)
        {
            _plugin = plugin;
            DefaultSettingsFile = $"{_plugin.GetPluginPath()}defaults.json";
            SettingsFile = "Saves\\lfe_facialmotioncapture.json";
            Load();
        }

        public string GetIpAddress()
        {
            return _settings["clientIp"]?.Value;
        }

        public void SetIpAddress(string ip) {
            _settings["clientIp"] = ip;
        }

        public string GetLocalServerIpAddress() {
            return _settings["serverIp"]?.Value;
        }

        public void SetLocalServerIpAddress(string ip) {
            _settings["serverIp"] = ip;
        }

        public string GetDevice() {
            return _settings["device"]?.Value;
        }

        public void SetDevice(string device) {
            _settings["device"] = device;
        }

        public float? GetShapeStrength(string name) {
            if(_settings["mappings"]?.AsObject[name]?.AsObject != null) {
                return _settings["mappings"][name]["strength"].AsFloat;
            }
            return null;
        }

        public void SetShapeStrength(string name, float strength)
        {
            _settings["mappings"][name]["strength"] = new SimpleJSON.JSONData(strength);
        }

        public string GetShapeMorph(string name) {
            if(name == null) {
                return null;
            }
            if(_settings["mappings"].AsObject.HasKey(name)) {
                return _settings["mappings"][name]["morph"].Value;
            }
            return null;
        }

        public SettingsController Load()
        {
            if(MVR.FileManagementSecure.FileManagerSecure.FileExists(SettingsFile, onlySystemFiles: true)) {
                _settings = SuperController.singleton.LoadJSON(SettingsFile)?.AsObject;
            }
            else {
                _settings = SuperController.singleton.LoadJSON(DefaultSettingsFile)?.AsObject;
            }
            return this;
        }

        public SettingsController Save()
        {
            SuperController.singleton.SaveJSON(_settings, SettingsFile);
            return this;
        }

        public SimpleJSON.JSONClass ToJsonClass() {
            return _settings;
        }
    }
}
