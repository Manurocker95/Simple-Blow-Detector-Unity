using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VirtualPhenix.MicrophoneBlowDetector
{
    public class MicrophoneBlowDetectorNoSource : MicrophoneBlowDetector
    {
        [Header("Debug No Source"),Space]
        [SerializeField] protected float m_recordingStartTime;

        public override bool IsMicrophoneBlocked => !m_isMicrophoneInitialized || !m_clip || m_clip.length == 0 || m_initializingMicrophone;

        public override void SetClipToAudioSource(AudioClip _newClip, bool _playIt = true)
        {
            if (_newClip)
                m_clip = _newClip;
        }
        public override void InitializeMicrophone(int _idx = 0, UnityAction _onInitCallback = null)
        {
            if (m_initializingMicrophone)
                return;

            m_microphoneDevice = Microphone.devices[_idx];
            m_clip = StartRecordingClipWithMicrophone();

            m_coroutine = StartCoroutine(WaitForMicrophoneToGetData(() =>
            {
                m_recordingStartTime = Time.time;
                m_isMicrophoneInitialized = true;
                SetClipToAudioSource(m_clip);
                _onInitCallback?.Invoke();
            }));
        }

        protected override IEnumerator WaitForMicrophoneToGetData(UnityAction _callback)
        {
            m_initializingMicrophone = true;
            while (m_clip == null || m_clip.length == 0)
            {
                yield return null;
            }
            m_initializingMicrophone = false;
            _callback?.Invoke();
        }

        protected override void AnalyzeSound()
        {
            var micPosition = (int)(((Time.time - m_recordingStartTime) * m_frequency) % m_clip.samples);

            int startPosition = micPosition - m_sampleCount;
            if (startPosition < 0)
                startPosition += m_clip.samples;
    
            m_clip.GetData(m_samples, startPosition);

            float sum = 0f;
            for (int i = 0; i < m_sampleCount; i++)
            {
                sum += Mathf.Pow(m_samples[i], 2);
            }

            m_rmsValue = Mathf.Sqrt(sum / m_sampleCount);
            m_dbValue = 20 * Mathf.Log10(m_rmsValue / m_refValue);

            m_dbValue = Mathf.Clamp(m_dbValue, -m_clampDB, m_dbValue);

            ComputeSpectrum();

            float maxV = 0f;
            int maxN = 0;

            for (int i = 0; i < m_sampleCount; i++)
            {
                if (m_spectrum[i] > maxV && m_spectrum[i] > m_amplitudeThreshold)
                {
                    maxV = m_spectrum[i];
                    maxN = i;
                }
            }

            float freqN = maxN;
            if (maxN > 0 && maxN < m_sampleCount - 1)
            {
                float dL = m_spectrum[maxN - 1] / m_spectrum[maxN];
                float dR = m_spectrum[maxN + 1] / m_spectrum[maxN];
                freqN += 0.5f * (dR * dR - dL * dL);
            }

            m_pitchValue = freqN * GetDefaultFrequency() / m_sampleCount;
        }

        protected virtual void ComputeSpectrum()
        {
            for (int k = 0; k < m_sampleCount; k++)
            {
                float real = 0f;
                float imag = 0f;
                for (int n = 0; n < m_sampleCount; n++)
                {
                    float angle = 2 * Mathf.PI * k * n / m_sampleCount;
                    real += m_samples[n] * Mathf.Cos(angle);
                    imag -= m_samples[n] * Mathf.Sin(angle);
                }
                m_spectrum[k] = Mathf.Sqrt(real * real + imag * imag);
            }
        }

        protected override bool IsBlowingTime(float _sumPitch)
        {
            return m_lowPassResults > -30 && (_sumPitch < 10 || _sumPitch > 2000);
        }
    }
}