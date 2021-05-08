# Virt-A-Mate Facial Motion Capture

Facial motion capture on newer iOS devices

![](images/demo.gif?raw=true)

## Quickstart

- [Install LIVE Face phone app](https://itunes.apple.com/us/app/live-face/id1357551209) on your iPhone. This app is not written by, nor supported by me.
- [Connect LIVE Face app to your network](https://manual.reallusion.com/Motion_LIVE_Plugin/ENU/Content/iClone_7/Pro_7.0/29_Plug_in/Motion_Live/Connecting_to_LIVE_Face.htm) either with USB or WiFi
- Add the VaM Facial Motion Capture plugin to a Person Atom
- Type in the IP Address listed in the "LIVE Face" app and click connect
- Have fun

## Installing Plugin

Requires VaM 1.19 or newer.

Download `LFE.FacialMotionCapture.(version).var` from [Releases](https://github.com/lfe999/FacialMotionCapture/releases)

Save the `.var` file in the `(VAM_ROOT)\AddonPackages`.

If you have VaM running already, click on the *Main UI > File (Open/Save) > Rescan Add-on Packages* button so that this plugin shows up.

## Tips

### How can I change what morph is used for each "blendshape"?

After you connect for the first time to the phone, a configuration file will be created that you can edit.  In the future this will be easier but sorry, it is by hand right now.

**Saves\PluginData\lfe_facialmotioncaptuire.json**
```
{
   "clientIp" : "192.168.1.2", 
   "mappings" : { 
      "Brow Down Left" : { 
         "morph" : "Put whatever morph name you want here", 
         "strength" : "1"
      }, 
      "Brow Down Right" : { 
         "morph" : "or here", 
         "strength" : "1"
      }, 
      "Brow Inner Up" : { 
         "morph" : "",  // empty means disabled
         "strength" : "1"
      }, 
      ...
}
```

Reload the plugin after editing this file.

### How can I playback my recorded captures?

The recordings can be imported into the Timeline plugin by Acid Bubbles.  You can get this plugin here. https://hub.virtamate.com/resources/timeline.94/

### Adjust jaw physics

The jaw physics on Person atoms is often too tight which can cause the motion capture to perform poorly.  Try playing around with the `Jaw Hold Spring`, `Jaw Hold Damper` and even `Tongue Collision`.

![](images/jaw_physics.png?raw=true)

### Turn off auto expressions

Many auto expressions will get in the way of motion capture.  Try playing around with the settings.

![](images/auto_behaviors.png?raw=true)

# TODO

- easier way for you to say what morph is used for each phone change that comes in
- find morphs better suited for motion capture. anyone can convert these to G2F/G2M? https://sharecg.com/v/92621/browse/21/DAZ-Studio/FaceShifter-For-Genesis-8-Female

# Credits

Icon: Face Recognition by mungang kim from the Noun Project

