using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.MicrophoneBlowDetector
{
    public class MicrophoneBlowDetectorNoSource : MicrophoneBlowDetector
    {
        public override bool IsMicrophoneBlocked => !m_isMicrophoneInitialized || !m_clip || m_clip.length == 0 || m_initializingMicrophone;

        public override void SetClipToAudioSource(AudioClip _newClip, bool _playIt = true)
        {
            if (_newClip)
                m_clip = _newClip;
        }

        protected override void AnalyzeSound()
        {
            int micPosition = GetMicrophoneCurrentPosition();

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
    }
}