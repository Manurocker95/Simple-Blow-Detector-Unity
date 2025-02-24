using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace VirtualPhenix.MicrophoneBlowDetector
{
    public class MicrophoneBlowDetectorAudioMixer : MicrophoneBlowDetector
    {
        [Header("Audio Mixer"), Space]
        [SerializeField] private AudioMixerGroup m_mixerGroup;

        protected override void InitializeDetector()
        {
            base.InitializeDetector();

            if (m_audioSource && m_mixerGroup)
            {
                m_audioSource.outputAudioMixerGroup = m_mixerGroup;
            }
        }
    }
}