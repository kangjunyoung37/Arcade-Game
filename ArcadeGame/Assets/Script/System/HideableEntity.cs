using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HideableEntity : MonoBehaviour
{
   private const int InitialRegistryCapacity = 128;
   private static readonly List<HideableEntity> ActiveEntities = new List<HideableEntity>(InitialRegistryCapacity);
   
   [Tooltip("시야 검사 대상 지점. 비워 두면 이 오브젝트 위치를 사용합니다.")]
   [SerializeField] private Transform visibilityPoint;
   private Renderer[] _renderers;
   private Canvas[] _canvases;
   private bool _visibilityApplied;
   public bool IsVisible { get; private set; }
   public Vector3 VisibilityPosition => visibilityPoint != null ? visibilityPoint.position : transform.position;
   public static int ActiveCount => ActiveEntities.Count;
   public static HideableEntity GetActiveEntity(int index)
   {
      return ActiveEntities[index];
   }  

   private void Awake()
   {
       _renderers = GetComponentsInChildren<Renderer>(true);
       _canvases = GetComponentsInChildren<Canvas>(true);
   }

   private void OnEnable()
   {
        ActiveEntities.Add(this);
        _visibilityApplied = false;
        SetVisibility(false);
   }

   private void OnDisable()
   {
        ActiveEntities.Remove(this);
        SetVisibility(false);
   }

   public void SetVisibility(bool isVisible)
   {
      // 결과가 같으면 Renderer와 Canvas를 다시 변경하지 않습니다.
      if (_visibilityApplied && IsVisible == isVisible)
         return;

      IsVisible = isVisible;
      _visibilityApplied = true;

      if (_renderers != null)
      {
         for (int i = 0; i < _renderers.Length; i++)
         {
               Renderer targetRenderer = _renderers[i];
               if (targetRenderer != null)
                  targetRenderer.enabled = isVisible;
         }
      }

      if (_canvases != null)
      {
         for (int i = 0; i < _canvases.Length; i++)
         {
               Canvas targetCanvas = _canvases[i];

               if (targetCanvas != null)
                  targetCanvas.enabled = isVisible;
         }
      }
   }
}
