# Simple Blow Detector For Unity

Simple example to detect the player to blow to the microphone in Unity Engine. Made with Unity 2022.3.58f1 but should work with any version below and above. 
It's not "stuck" on any platform and it should work on PC and Mobile just fine. Consoles will require work to handle the microphone, but once that is overriden
it should be a straight forward process.

# How to use

If you plan to directly use the MicrophoneBlowDetector, just copy it to your project and attach it to a gameObject. If the m_initMicrophoneOnStart variable is true,
it will try to initialize and start recording the audio to m_clip. If you want, you can use your own stuff for recording and call SetClipToAudioSource function
to set an audioclip that will be used for the blow recognition. If you need to check if it's blowing or not, just listen to m_onBlowingStateChange event (see 
MicrophoneBlowDetectorDebugText script if you need an example.)

# Troubleshooting:

This example has one big flaw: The microphone data is set to the audioSource clip to use audioSource.GetOutputData and audioSource.GetSpectrumData. This approach
makes it "heardable", so you will need to play with AudioMixer DB values to not hear the recorded blow in-game.

# Credits:
This example is based on a ["lost" forum post]("https://web.archive.org/web/20120122003213/http://forum.unity3d.com/threads/118215-Blow-detection-(Using-iOS-Microphone)?p=802891&viewfull=1#post802891") 
and reworked for easy handle to modern Unity + C# praxis . Feel free to make any PR or modification you want.

Credits are appreciated :)
