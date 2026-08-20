using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideableEntity : MonoBehaviour
{
   private Renderer[] _renderers;
   private Canvas _canvas;

   private void Awake()
   {
      _renderers = GetComponentsInChildren<Renderer>();
      _canvas = GetComponent<Canvas>();
   }

   private void Start()
   {
      SetVisibility(false);
   }

   public void SetVisibility(bool isVisible)
   {
      foreach (Renderer r in _renderers)
      {
         r.enabled = isVisible;
      }
      
      if (_canvas != null)
      {
         _canvas.enabled = isVisible;
      }
   }
}
