using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VirtualPhenix.MicrophoneBlowDetector
{
    public class MicrophoneBlowDetectorDebugText : MonoBehaviour
    {
        [Header("Detector"),Space]
        /// <summary>
        /// Reference to the detector
        /// </summary>
        [SerializeField] private MicrophoneBlowDetector m_detector;

        [Header("Text References"), Space]
        /// <summary>
        /// Index in Microphone.devices array
        /// </summary>
        [SerializeField] protected TMP_Text m_debugText;

        private void Reset()
        {
            m_detector = FindAnyObjectByType<MicrophoneBlowDetector>();

            if (!m_debugText)
                m_debugText = GetComponent<TMP_Text>();
        }

        // Start is called before the first frame update
        void Start()
        {
            if (!m_detector)
                m_detector = FindAnyObjectByType<MicrophoneBlowDetector>();

            if (!m_debugText)
                m_debugText = GetComponent<TMP_Text>();

            StartListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        /// <summary>
        /// Start listening to detector state change event
        /// </summary>
        private void StartListening()
        {
            m_detector?.OnBlowingStateChange?.AddListener(UpdateDebugText);
        }

        /// <summary>
        /// Stop listening to detector state change event
        /// </summary>
        private void StopListening()
        {
            m_detector?.OnBlowingStateChange?.RemoveListener(UpdateDebugText);
        }

        /// <summary>
        /// Mofify the UI to see the results
        /// </summary>
        protected virtual void UpdateDebugText(bool _isBlowing)
        {
            if (!m_debugText)
                return;

            m_debugText.text = (_isBlowing) ? "Player is blowing!" : "Player is <color=red>NOT</color> blowing!";
        }

    }
}