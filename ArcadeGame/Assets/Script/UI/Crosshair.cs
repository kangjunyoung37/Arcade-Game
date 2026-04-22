using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{

   [Header("Target Reference")] 
   public Character player;
   
   [Header("UI Elements")] 
   public RectTransform crosshairRoot;
   public RectTransform topBar,bottomBar,leftBar,rightBar;

   [Header("Settings")] 
   public float idleSpread = 50f;
   public float aimSpread = 15f;
   public float maxSpread = 150f;
   public float spreadPerShot = 20f;
   public float spreadRecoverySpeed = 5f;

   private float _currentSpread;
   private float _targetBaseSpread;

   [Header("UI Settings")]
   public float spreadMultiplier = 5f;
   private void Start()
   {
      Cursor.visible = false;
      _currentSpread = idleSpread;
      _targetBaseSpread = idleSpread;
   }

   private void Update()
   {
      if (crosshairRoot != null)
         crosshairRoot.position = Input.mousePosition;
      _currentSpread = Mathf.Lerp(_currentSpread, _targetBaseSpread, Time.deltaTime * spreadRecoverySpeed);
      ApplySpread();
   }

   public void SetAiming(bool isAiming)
   {
      _targetBaseSpread = isAiming ? aimSpread : idleSpread; 
   }

   public void AddSpread(float amount)
   {
      _currentSpread += amount;
      _currentSpread = Mathf.Clamp(_currentSpread, 0, maxSpread);
      
   }

   private void ApplySpread()
   {
      if (topBar == null || bottomBar == null || leftBar == null || rightBar == null) return;
      
      topBar.anchoredPosition = new Vector2(0, _currentSpread);
      bottomBar.anchoredPosition = new Vector2(0, -_currentSpread);
      leftBar.anchoredPosition = new Vector2(-_currentSpread, 0);
      rightBar.anchoredPosition = new Vector2(_currentSpread, 0);
   }
}
