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

### How can I record these facial capture changes?

Unfortunately this plugin does not record morph changes right now.  You will have to get creative with how these morph changes get stored to a scene.  Message me if you have any ideas on how to do this.

### Adjust jaw physics

The jaw physics on Person atoms is often too tight which can cause the motion capture to perform poorly.  Try playing around with the `Jaw Hold Spring`, `Jaw Hold Damper` and even `Tongue Collision`.

![](images/jaw_physics.png?raw=true)

### Turn off auto expressions

Many auto expressions will get in the way of motion capture.  Try playing around with the settings.

![](images/auto_behaviors.png?raw=true)

# Credits

Icon: Face Recognition by mungang kim from the Noun Project

