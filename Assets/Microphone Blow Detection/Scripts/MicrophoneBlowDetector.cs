using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VirtualPhenix.MicrophoneBlowDetector
{
    /// <summary>
    /// Sample to how to detect blowing to the microphone. You can modify it as you wish or make PR to improve this.
    /// </summary>
    public class MicrophoneBlowDetector : MonoBehaviour
    {
        [Header("Microphone"), Space]
        /// <summary>
        /// Wether we need to initialize the microphone on start to record audio
        /// </summary>
        [SerializeField] protected bool m_initMicrophoneOnStart = true;
        /// <summary>
        /// If set to false, m_frequency won't be overriden with GetDefaultFrequency() 
        /// </summary>
        [SerializeField] protected bool m_useDefaultSettingsAsFrequency = true;
        /// <summary>
        /// Index in Microphone.devices array
        /// </summary>
        [SerializeField] protected int m_microphoneIndex = 0;
        /// <summary>
        /// Microphone device name from Microphone.devices array
        /// </summary>
        [SerializeField] protected string m_microphoneDevice;
        /// <summary>
        /// How many seconds you want to record (in-loop). Take into account this will be placed into RAM memory.
        /// </summary>
        [SerializeField] protected int m_microphoneRecordTime = 60;
        /// <summary>
        /// Wether the microphone is already recording
        /// </summary>
        [SerializeField] protected bool m_isMicrophoneInitialized;

        [Header("Filter Config"),Space]
        /// <summary>
        /// Sample rate.
        /// </summary>
        [SerializeField] protected int m_frequency = 48000;
        /// <summary>
        /// Number of samples to analyze at once.
        /// </summary>
        [SerializeField] protected int m_sampleCount = 1024;
        /// <summary>
        /// Minimum amplitude to extract pitch.
        /// </summary>
        [SerializeField] protected float m_amplitudeThreshold = 0.2f;
        /// <summary>
        ///  RMS value for 0 dB.
        /// </summary>
        [SerializeField] protected float m_refValue = 0.1f;
        /// <summary>
        /// Value to clamp the minimum DB as -m_clampDB
        /// </summary>
        [SerializeField] protected float m_clampDB = 160f;
        /// <summary>
        /// Minimum amplitude to extract pitch.
        /// </summary>
        [SerializeField] protected float m_amplitudePitch = 0.02f;
        /// <summary>
        /// Alpha for the low-pass filter.
        /// </summary>
        [SerializeField] protected float m_lowPassFilterAlpha = 0.05f;
        /// <summary>
        ///  How many previous frames of sound are analyzed.
        /// </summary>
        [SerializeField] protected float m_recordedFramesLength = 50;

        [Header("Blow config"), Space]
        /// <summary>
        /// Threshold to recognize wether the system consider the user is blowing or not. It happens when m_blowingTime is higher than m_requiredBlowTime 
        /// </summary>
        [SerializeField] protected int m_blowTimeThreshold = 4;

        [Header("Audio Source Reference"), Space]
        /// <summary>
        /// Index in Microphone.devices array
        /// </summary>
        [SerializeField] protected AudioSource m_audioSource;

        [Header("Blowing Debug"), Space]
        /// <summary>
        /// Blowing time so we can control when it starts and ends based on low pass filter values
        /// </summary>
        [SerializeField] protected int m_blowingTime = 0;
        /// <summary>
        /// Wether the system consider the user is blowing or not. It happens when m_blowingTime is higher than m_requiredBlowTime 
        /// </summary>
        [SerializeField] protected bool m_isBlowing = false;
        
        [Header("Events"), Space]
        /// <summary>
        /// Event that triggers when m_isBlowing changes
        /// </summary>
        [SerializeField] protected UnityEvent<bool> m_onBlowingStateChange;
        /// <summary>
        /// Flag to check if we are currently in the process of initialization
        /// </summary>
        protected bool m_initializingMicrophone;
        /// <summary>
        /// Volume in RMS
        /// </summary>
        protected float m_rmsValue;
        /// <summary>
        /// Volume in DB
        /// </summary>
        protected float m_dbValue;
        /// <summary>
        /// Pitch in Hz
        /// </summary>
        protected float m_pitchValue;
        /// <summary>
        /// Low Pass Filter result
        /// </summary>
        protected float m_lowPassResults;
        /// <summary>
        /// Audio Samples from the AudioSource
        /// </summary>
        protected float[] m_samples;
        /// <summary>
        /// Frequency Spectrum from the AudioSource
        /// </summary>
        protected float[] m_spectrum;
        /// <summary>
        /// Clip filled from the Microphone
        /// </summary>
        protected AudioClip m_clip;
        /// <summary>
        /// Coroutine instance (to kill the process if we need it)
        /// </summary>
        protected Coroutine m_coroutine;
        /// <summary>
        /// Used to average recent volume.
        /// </summary>
        protected List<float> m_dbValues;
        /// <summary>
        /// Used to average recent pitch.
        /// </summary>
        protected List<float> m_pitchValues;

        /// <summary>
        /// Property to see if we are initializing the mic or directly, we haven't initialized it yet
        /// </summary>
        public virtual bool IsMicrophoneBlocked => !m_isMicrophoneInitialized || !m_audioSource || !m_audioSource.isPlaying || m_initializingMicrophone;
        /// <summary>
        /// Property to get the event m_onBlowingStateChange
        /// </summary>
        public virtual UnityEvent<bool> OnBlowingStateChange => m_onBlowingStateChange;
        /// <summary>
        /// Property to get m_isBlowing
        /// </summary>
        public virtual bool IsBlowing => m_isBlowing;

        /// <summary>
        /// We try to recover the audioSource reference on attach/reset
        /// </summary>
        protected virtual void Reset()
        {
            transform.TryGetComponent(out m_audioSource);
        }

        /// <summary>
        /// Detector initialization
        /// </summary>
        protected virtual void Awake()
        {
            InitializeDetector();
        }

        /// <summary>
        /// Microphone Initialization if possible
        /// </summary>
        protected virtual void Start()
        {
            if (m_initMicrophoneOnStart)
            {
                InitializeMicrophone(m_microphoneIndex);
            }
        }

        /// <summary>
        /// Destruction of the parallel initialization
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (m_coroutine != null && m_initializingMicrophone)
            {
                StopCoroutine(m_coroutine);
            }
        }

        protected virtual void InitializeDetector()
        {
            // Initialize sample array
            m_samples = new float[m_sampleCount];
            m_spectrum = new float[m_sampleCount];
            m_dbValues = new List<float>();
            m_pitchValues = new List<float>();

            m_initializingMicrophone = false;
            m_isMicrophoneInitialized = false;

            if (!m_audioSource)
            {
                transform.TryGetComponent(out m_audioSource);
            }
        }

        /// <summary>
        /// Initialize the microphone based on index and start recorting
        /// </summary>
        public virtual void InitializeMicrophone(int _idx = 0)
        {
            if (m_initializingMicrophone)
                return;

            m_microphoneDevice = Microphone.devices[_idx];
            m_clip = Microphone.Start(m_microphoneDevice, true, m_microphoneRecordTime, m_useDefaultSettingsAsFrequency ? GetDefaultFrequency() : m_frequency);

            m_coroutine = StartCoroutine(WaitForMicrophoneToGetData(() =>
            {
                if (m_audioSource != null)
                {
                    m_isMicrophoneInitialized = true;
                    SetClipToAudioSource(m_clip);
                }
            }));
        }

        /// <summary>
        /// Coroutine to initialize the microphone async
        /// </summary>
        /// <param name="_callback"></param>
        /// <returns></returns>
        protected virtual IEnumerator WaitForMicrophoneToGetData(UnityAction _callback)
        {
            m_initializingMicrophone = true;
            while (!(Microphone.GetPosition(m_microphoneDevice) > 0))
            {
                yield return null;
            }
            m_initializingMicrophone = false;
            _callback?.Invoke();
        }

        /// <summary>
        /// Set the desired clip to the audio source, this can be used for pre-recorded clips to be used
        /// </summary>
        /// <param name="_newClip"></param>
        /// <param name="_playIt"></param>
        public virtual void SetClipToAudioSource(AudioClip _newClip, bool _playIt = true)
        {
            if (m_audioSource && _newClip)
            {
                m_audioSource.clip = m_clip;
                m_audioSource.Play();
            }
        }
        /// <summary>
        /// Check for the blow every frame
        /// </summary>
        protected virtual void Update()
        {
            if (IsMicrophoneBlocked)
            {
                return;
            }

            // Update the blow recognition by analyzing the sound and check the low pass filter
            UpdateBlowRecognition();
        }

        /// <summary>
        /// Update the blow recognition by analyzing the sound and check the low pass filter
        /// </summary>
        protected virtual void UpdateBlowRecognition()
        {
            // Gets volume and pitch values
            AnalyzeSound();

            // Runs a series of algorithms to decide whether a blow is occuring.
            DeriveBlow();
        }

        /// <summary>
        /// Analyzes the sound, to get volume and pitch values.
        /// Credits to aldonaletto for the function, http://goo.gl/VGwKt
        /// </summary>
        protected virtual void AnalyzeSound()
        {
            // Get all of our samples from the mic.
            m_audioSource.GetOutputData(m_samples, 0);

            // Sums squared samples
            float sum = 0;

            for (int i = 0; i < m_sampleCount; i++)
            {
                sum += Mathf.Pow(m_samples[i], 2);
            }

            // RMS is the square root of the average value of the samples.
            m_rmsValue = Mathf.Sqrt(sum / m_sampleCount);
            m_dbValue = 20 * Mathf.Log10(m_rmsValue / m_refValue);

            // Clamp the values.
            m_dbValue = Mathf.Clamp(m_dbValue, -m_clampDB, m_dbValue);

            // Gets the sound spectrum.
            m_audioSource.GetSpectrumData(m_spectrum, 0, FFTWindow.BlackmanHarris);

            float maxV = 0;
            int maxN = 0;

            // Find the highest sample.
            for (int i = 0; i < m_sampleCount; i++)
            {
                if (m_spectrum[i] > maxV && m_spectrum[i] > m_amplitudeThreshold)
                {
                    maxV = m_spectrum[i];
                    maxN = i; // maxN is the index of max
                }
            }

            // Pass the index to a float variable
            float freqN = maxN;

            // Interpolate index using neighbours
            if (maxN > 0 && maxN < m_sampleCount - 1)
            {
                float dL = m_spectrum[maxN - 1] / m_spectrum[maxN];
                float dR = m_spectrum[maxN + 1] / m_spectrum[maxN];
                freqN += 0.5f * (dR * dR - dL * dL);
            }

            // Convert index to frequency
            m_pitchValue = freqN * (GetDefaultFrequency()) / m_sampleCount;
        }

        /// <summary>
        /// Update the blowing state based on the low pass filter to recognize it from sample data
        /// </summary>
        protected virtual void DeriveBlow()
        {
            UpdateRecords(m_dbValue, m_dbValues);
            UpdateRecords(m_pitchValue, m_pitchValues);

            // Find the average pitch in our records (used to decipher against whistles, clicks, etc).
            float sumPitch = 0;

            foreach (float num in m_pitchValues)
            {
                sumPitch += num;
            }

            sumPitch /= m_pitchValues.Count;

            // Run our low pass filter.
            m_lowPassResults = LowPassFilter(m_dbValue);

            // Decides whether this instance of the result could be a blow or not.
            if (m_lowPassResults > -30 && sumPitch == 0)
            {
                m_blowingTime += 1;
            }
            else
            {
                m_blowingTime = 0;
            }

            // We update the blowing state so we can trigger animations/text or whatever
            var isBlowing = (m_blowingTime > m_blowTimeThreshold);
            if (m_isBlowing != isBlowing)
            {
                m_onBlowingStateChange?.Invoke(isBlowing);
            }
            m_isBlowing = isBlowing;
        }

        /// <summary>
        /// Updates a record, by removing the oldest entry and adding the newest value (val).
        /// </summary>
        /// <param name="val"></param>
        /// <param name="record"></param>

        protected virtual void UpdateRecords(float val, List<float> record)
        {
            if (record.Count > m_recordedFramesLength)
            {
                record.RemoveAt(0);
            }

            record.Add(val);
        }

        /// <summary>
        /// Applies the low pass filter based on the obtained results
        /// </summary>
        /// <param name="peakVolume"></param>
        /// <returns></returns>

        protected virtual float LowPassFilter(float peakVolume)
        {
            return m_lowPassFilterAlpha * peakVolume + (1.0f - m_lowPassFilterAlpha) * m_lowPassResults;
        }

        /// <summary>
        /// Get specific platform frequency to analyze the recorded clip. On iOS is 24000Hz.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetDefaultFrequency()
        {
            return AudioSettings.outputSampleRate;
        }
    }
}