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
        private List<FloatParamFrame> _recordedFrames = new List<FloatParamFrame>();

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
            _recordedFrames = new List<FloatParamFrame>();
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
                if(_currentFrameId == 0) {
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
                if(_currentFrameId == 0) {
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
            var groupedFrames = new Dictionary<string, List<FloatParamFrame>>();
            int maxFrameNumber = 0;
            float frameDuration = _initialDeltaTime;
            lock(_recordedFrames) {
                maxFrameNumber = _recordedFrames.Max(f => f.Number);
                // group frames by (storable,name) -> values[]
                groupedFrames = _recordedFrames
                    .GroupBy(x => $"{x.StorableName}_{x.Name}")
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

            foreach(var morphFrames in groupedFrames) {
                var storable = morphFrames.Value.First().StorableName;
                var name = morphFrames.Value.First().Name;
                var frames = morphFrames.Value;

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

            animation["Clips"].Add(animationClip);

            return animation;
        }

        public int NextFrame() {
            _currentFrameId++;
            return _currentFrameId;
        }
    }
}

