using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Extensions;

namespace LFE.FacialMotionCapture.Controllers {

    public class SettingsController {

        private SimpleJSON.JSONClass _settings;
        private Plugin _plugin;

        public string DefaultSettingsFile { get; private set; }
        public string GlobalSettingsFile { get; private set; }

        public const string CLIENT_IP_KEY = "clientIp";
        public const string SERVER_IP_KEY = "serverIp";
        public const string DEVICE_KEY = "device";
        public const string GROUP_TOGGLE_KEY = "groupToggle";

        public SettingsController(Plugin plugin)
        {
            _plugin = plugin;
            DefaultSettingsFile = $"{_plugin.GetPluginPath()}defaults.json";
            GlobalSettingsFile = "Saves\\lfe_facialmotioncapture.json";
            LoadFromGlobal();
        }

        public string GetIpAddress()
        {
            return _settings[CLIENT_IP_KEY]?.Value;
        }

        public void SetIpAddress(string ip) {
            _settings[CLIENT_IP_KEY] = ip;
        }

        public string GetLocalServerIpAddress() {
            return _settings[SERVER_IP_KEY]?.Value;
        }

        public void SetLocalServerIpAddress(string ip) {
            _settings[SERVER_IP_KEY] = ip;
        }

        public string GetDevice() {
            return _settings[DEVICE_KEY]?.Value;
        }

        public void SetDevice(string device) {
            _settings[DEVICE_KEY] = device;
        }

        public bool GetGroupEnabled(string groupName) {
            if(_settings[GROUP_TOGGLE_KEY]?.AsObject?.HasKey(groupName) ?? false) {
                return _settings[GROUP_TOGGLE_KEY][groupName].AsBool;
            }
            return true;
        }

        public void SetGroupEnabled(string groupName, bool isEnabled) {
            _settings[GROUP_TOGGLE_KEY][groupName].AsBool = isEnabled;
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

        public SettingsController LoadFromGlobal()
        {
            if(MVR.FileManagementSecure.FileManagerSecure.FileExists(GlobalSettingsFile, onlySystemFiles: true)) {
                return LoadFrom(GlobalSettingsFile);
            }
            return LoadFrom(DefaultSettingsFile);
        }

        public SettingsController LoadFrom(string fileName) {
            return LoadFrom(SuperController.singleton.LoadJSON(DefaultSettingsFile)?.AsObject);
        }

        public SettingsController LoadFrom(SimpleJSON.JSONClass jsonClass) {
            _settings = jsonClass;
            return this;
        }

        public SettingsController SaveToGlobal()
        {
            return SaveTo(GlobalSettingsFile);
        }

        public SettingsController SaveTo(string fileName) {
            SuperController.singleton.SaveJSON(_settings, fileName);
            return this;
        }

        public SimpleJSON.JSONClass ToJSONClass() {
            return _settings;
        }
    }
}
