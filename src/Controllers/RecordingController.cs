using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFE.FacialMotionCapture.Controllers {
    public class RecordingController {
        private int _currentFrameId = 0;
        private float _initialDeltaTime = 0;
        private List<ITimelineFrame> _recordedFrames = new List<ITimelineFrame>();

        public Plugin Plugin { get; private set; }
        public bool IsRecording { get; private set; }
        public string DefaultSettingsFile {
            get {
                string recordingId = DateTime.Now.ToString("yyMMddTHHmmss");
                return $"Saves\\animations\\mocap\\face_{recordingId}.json";
            }
        }

        public RecordingController(Plugin plugin)
        {
            Plugin = plugin;
            Reset();
        }

        public void Reset() {
            IsRecording = false;
            _initialDeltaTime = 0;
            _currentFrameId = 0;
            lock(_recordedFrames) {
                _recordedFrames = new List<ITimelineFrame>();
            }
        }

        public void Start() {
            Reset();
            IsRecording = true;
        }

        public void Stop() {
            IsRecording = false;
        }

        public void RecordMorphValue(string morphName, float value) {
            lock(_recordedFrames) {
                if(_initialDeltaTime == 0) {
                    _initialDeltaTime = Time.deltaTime;
                }
                _recordedFrames.Add(new FloatParamFrame {
                    StorableName = "geometry",
                    Number = _currentFrameId + 1,
                    Name = morphName,
                    Value = value
                });
            }
        }

        public void RecordEyeValue(string eyeParamName, float value) {
            if(!Plugin.EyePluginInstalled()) {
                return;
            }

            lock(_recordedFrames) {
                if(_initialDeltaTime == 0) {
                    _initialDeltaTime = Time.deltaTime;
                }
                _recordedFrames.Add(new FloatParamFrame {
                    StorableName = Plugin.EyePlugin.name,
                    Number = _currentFrameId + 1,
                    Name = eyeParamName,
                    Value = value
                });
            }
        }

        public void RecordHeadRotationValue(Quaternion rotation) {
            if(Plugin.HeadController == null) {
                return;
            }
            lock(_recordedFrames) {
                if(_initialDeltaTime == 0) {
                    _initialDeltaTime = Time.deltaTime;
                }
                // head position needs to be there for timeline in the first frame always. force it
                Vector3? position = null;
                if(_currentFrameId == 0) {
                    position = Plugin.HeadController.transform.position;
                }
                _recordedFrames.Add(new ControllerFrame {
                    Number = _currentFrameId + 1,
                    ControllerName = "headControl",
                    Rotation = rotation,
                    Position = position
                });
            }
        }

        public string SaveTimelineAnimation() {
            var timelineAnimation = GetTimelineAnimation();

            if(timelineAnimation == null) {
                return null;
            }

            var filename = DefaultSettingsFile;

            // make sure the path for the file exists
            var savedir = filename.Substring(0, filename.LastIndexOf("\\"));
            if(!MVR.FileManagementSecure.FileManagerSecure.DirectoryExists(savedir)) {
                MVR.FileManagementSecure.FileManagerSecure.CreateDirectory(savedir);
            }

            SuperController.singleton.SaveJSON(timelineAnimation, filename);

            return filename;
        }

        public SimpleJSON.JSONClass GetTimelineAnimation() {

            // snapshot the current recorded frames
            var groupedFrames = new Dictionary<string, List<ITimelineFrame>>();
            int maxFrameNumber = 0;
            float frameDuration = _initialDeltaTime;
            lock(_recordedFrames) {
                maxFrameNumber = _recordedFrames.Max(f => f.Number);
                groupedFrames = _recordedFrames
                    .GroupBy(x => x.GetGroupName())
                    .ToDictionary(x => x.Key, x => x.ToList());
            }

            if(groupedFrames.Count == 0) {
                return null;
            }

            string recordingId = DateTime.Now.ToString("yyMMddTHHmmss");

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

            foreach(var frameGroup in groupedFrames) {
                var firstFrame = frameGroup.Value.First();
                if(firstFrame is FloatParamFrame) {
                    var storable = ((FloatParamFrame)firstFrame).StorableName;
                    var name = ((FloatParamFrame)firstFrame).Name;
                    var frames = frameGroup.Value.Cast<FloatParamFrame>();

                    var floatParam = new SimpleJSON.JSONClass();
                    floatParam["Storable"] = storable;
                    floatParam["Name"] = name;
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
                else if(firstFrame is ControllerFrame) {
                    var controllerName = ((ControllerFrame)firstFrame).ControllerName;
                    var frames = frameGroup.Value.Cast<ControllerFrame>();

                    var controller = new SimpleJSON.JSONClass();
                    controller["Controller"] = controllerName;

                    controller["X"] = new SimpleJSON.JSONArray();
                    controller["Y"] = new SimpleJSON.JSONArray();
                    controller["Z"] = new SimpleJSON.JSONArray();
                    controller["RotX"] = new SimpleJSON.JSONArray();
                    controller["RotY"] = new SimpleJSON.JSONArray();
                    controller["RotZ"] = new SimpleJSON.JSONArray();
                    controller["RotW"] = new SimpleJSON.JSONArray();
                    foreach(var frame in frames) {
                        string t = ((frame.Number - 1) * frameDuration).ToString();
                        string ti = "0";
                        string to = "0";
                        string c = "3";
                        if(frame.Position.HasValue) {
                            var pos = frame.Position.Value;
                            foreach(var a in new string[] {"X", "Y", "Z"} ) {
                                string value = null;
                                switch(a) {
                                    case "X": value = pos.x.ToString(); break;
                                    case "Y": value = pos.y.ToString(); break;
                                    case "Z": value = pos.z.ToString(); break;
                                }
                                if(value != null) {
                                    var entry = new SimpleJSON.JSONClass();
                                    entry["v"] = value;
                                    entry["t"] = t;
                                    entry["ti"] = ti;
                                    entry["to"] = to;
                                    entry["c"] = c;
                                    controller[a].Add(entry);
                                }
                            }
                        }
                        if(frame.Rotation.HasValue) {
                            var rot = frame.Rotation.Value;
                            foreach(var a in new string[] {"RotX", "RotY", "RotZ", "RotW"} ) {
                                string value = null;
                                switch(a) {
                                    case "RotX": value = rot.x.ToString(); break;
                                    case "RotY": value = rot.y.ToString(); break;
                                    case "RotZ": value = rot.z.ToString(); break;
                                    case "RotW": value = rot.w.ToString(); break;
                                }
                                if(value != null) {
                                    var entry = new SimpleJSON.JSONClass();
                                    entry["v"] = value;
                                    entry["t"] = t;
                                    entry["ti"] = ti;
                                    entry["to"] = to;
                                    entry["c"] = c;
                                    controller[a].Add(entry);
                                }
                            }
                        }
                    }

                    animationClip["Controllers"].Add(controller);
                }

            }

            animation["Clips"].Add(animationClip);

            return animation;
        }

        public int NextFrame() {
            _currentFrameId++;
            return _currentFrameId;
        }
    }
}

