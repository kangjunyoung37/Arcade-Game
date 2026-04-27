using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
      [Header("FOV Settings")]
      public float viewRadius = 10f;
      [Range(0,360)] public float viewAngle = 90f;
      public LayerMask targetMask;
      public LayerMask obstacleMask;
      
      private List<HideableEntity> _previouslyVisibleTargets = new List<HideableEntity>();
      private const int MaxTargets = 50; 
      private Collider[] _targetsInViewRadius = new Collider[MaxTargets];
      void Start()
      {
            StartCoroutine(FindTargetsWithDelay(0.1f));
      }

      private IEnumerator FindTargetsWithDelay(float delay)
      {
            while (true)
            {
                  yield return new WaitForSeconds(delay);
                  FindVisibleTargets();
            }
      }

      private void FindVisibleTargets()
      {
            foreach (var entity in _previouslyVisibleTargets)
            {
                  if(entity) entity.SetVisibility(false);
                  
            }
            _previouslyVisibleTargets.Clear();
            int targetCount = Physics.OverlapSphereNonAlloc(transform.position, viewRadius, _targetsInViewRadius, targetMask);
            for (var i = 0; i < targetCount; ++i)
            {
                  Transform target = _targetsInViewRadius[i].transform;
                  Vector3 dirToTarget = (target.position - transform.position).normalized;

                  if (!(Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2f)) continue;
                  float dstToTarget = Vector3.Distance(transform.position, target.position);
                  if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                  {
                        if (target.TryGetComponent(out HideableEntity h))
                        {
                              h.SetVisibility(true);
                              _previouslyVisibleTargets.Add(h);
                        }
                  }

            }
      }
}
