using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace FirstPersonMobileTools.DynamicFirstPerson
{
    public class CameraLook : MonoBehaviour
    {
        public enum TouchDetectMode { FirstTouch, LastTouch, All }

        [HideInInspector]
        public float Sensitivity_X { private get { return m_Sensitivity.x; } set { m_Sensitivity.x = value; } }
        [HideInInspector]
        public float Sensitivity_Y { private get { return m_Sensitivity.y; } set { m_Sensitivity.y = value; } }

        [SerializeField] private float m_BottomClamp = 90f;
        [SerializeField] private float m_TopClamp    = 90f;
        [SerializeField] private bool  m_InvertX     = false;
        [SerializeField] private bool  m_InvertY     = false;
        [SerializeField] private int   m_TouchLimit  = 10;
        [SerializeField] private Vector2 m_Sensitivity = Vector2.one;
        [SerializeField] private float recoilRecoverySpeed = 5f;  // velocidad de decay del recoil

        public TouchDetectMode m_TouchDetectMode;

        private Transform m_CameraTransform;
        private EventSystem m_EventStytem;
        private Func<Touch,bool> m_IsTouchAvailable;
        private List<string> m_AvailableTouchesId = new List<string>();

        private float invertX, invertY;
        private float m_HorizontalRot, m_VerticalRot;
        [HideInInspector] public Vector2 delta = Vector2.zero;
        private Vector2 recoilOffset = Vector2.zero;

        private void Start()
        {
            if (Camera.main != null)
                m_CameraTransform = Camera.main.transform;
            else
                Debug.LogError("No hay cámara con tag MainCamera", this);

            if (EventSystem.current != null)
                m_EventStytem = EventSystem.current;
            else
                Debug.LogError("No hay EventSystem en la escena");

            OnChangeSettings();
        }

        private void Update()
        {
            if (Input.touchCount == 0) return;

            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began &&
                    m_EventStytem != null &&
                    !m_EventStytem.IsPointerOverGameObject(touch.fingerId) &&
                    m_AvailableTouchesId.Count < m_TouchLimit)
                {
                    m_AvailableTouchesId.Add(touch.fingerId.ToString());
                }

                if (m_AvailableTouchesId.Count == 0) continue;

                if (m_IsTouchAvailable(touch))
                {
                    delta += touch.deltaPosition;
                    if (touch.phase == TouchPhase.Ended)
                        m_AvailableTouchesId.RemoveAt(0);
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    m_AvailableTouchesId.Remove(touch.fingerId.ToString());
                }
            }
        }

        private void LateUpdate()
        {
            // 1) Cálculo del movimiento táctil
            m_HorizontalRot = delta.x * m_Sensitivity.x * Time.deltaTime * invertX;
            m_VerticalRot   += delta.y * m_Sensitivity.y * Time.deltaTime * invertY;

            // 2) Aplico recoil offset
            m_HorizontalRot += recoilOffset.x;
            m_VerticalRot   += recoilOffset.y;

            // 3) Clamp vertical y aplico rotaciones
            m_VerticalRot = Mathf.Clamp(m_VerticalRot, -m_BottomClamp, m_TopClamp);

            if (m_CameraTransform != null)
                m_CameraTransform.localRotation = Quaternion.Euler(m_VerticalRot, 0f, 0f);

            transform.Rotate(Vector3.up * m_HorizontalRot);

            // 4) Reset touch delta
            delta = Vector2.zero;

            // 5) Decay del recoil hacia Vector2.zero
            recoilOffset = Vector2.MoveTowards(
                recoilOffset,
                Vector2.zero,
                recoilRecoverySpeed * Time.deltaTime
            );
        }

        public void OnChangeSettings()
        {
            invertX = m_InvertX ? -1 : 1;
            invertY = m_InvertY ? -1 : 1;

            switch (m_TouchDetectMode)
            {
                case TouchDetectMode.FirstTouch:
                    m_IsTouchAvailable = (Touch t) => t.fingerId.ToString() == m_AvailableTouchesId[0];
                    break;
                case TouchDetectMode.LastTouch:
                    m_IsTouchAvailable = (Touch t) => t.fingerId.ToString() == m_AvailableTouchesId[^1];
                    break;
                case TouchDetectMode.All:
                    m_IsTouchAvailable = (Touch t) => m_AvailableTouchesId.Contains(t.fingerId.ToString());
                    break;
            }
        }

        /// <summary>
        /// Llámalo desde tu arma: recoil.x = giro horizontal instantáneo; recoil.y = alza de cámara.
        /// </summary>
        public void AddRecoil(Vector2 recoil)
        {
            recoilOffset += recoil;
        }

        public void SetMode(int value)
        {
            m_TouchDetectMode = (TouchDetectMode)(2 - value);
            OnChangeSettings();
        }
    }
}