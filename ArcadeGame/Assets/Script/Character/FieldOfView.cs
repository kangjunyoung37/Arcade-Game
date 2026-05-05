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
      
      [Header("Mesh Settings")] 
      public float meshResolution = 1f;
      public MeshFilter viewMeshFilter;
      private Mesh _viewMesh;
      
      private List<HideableEntity> _previouslyVisibleTargets = new List<HideableEntity>();
      private const int MaxTargets = 50; 
      private Collider[] _targetsInViewRadius = new Collider[MaxTargets];
      void Start()
      {
            _viewMesh = new Mesh();
            _viewMesh.name = "ViewMesh";
            viewMeshFilter.mesh = _viewMesh;
            
            StartCoroutine(FindTargetsWithDelay(0.1f));
      }

      void LateUpdate()
      {
            DrawFieldOfView();
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

      private void DrawFieldOfView()
      {
            int stepCount = Mathf.RoundToInt(viewRadius / meshResolution);
            float stepAngleSize = viewAngle / stepCount;
            
            List<Vector3> viewPoints = new List<Vector3>();
            for (int i = 0; i <= stepCount; ++i)
            {
                  float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
                  Vector3 dir = DirFromAngle(angle, true);

                  if (Physics.Raycast(transform.position, dir, out RaycastHit hit, viewRadius, obstacleMask))
                  {
                        viewPoints.Add(hit.point);
                  }
                  else
                  {
                        viewPoints.Add(transform.position + dir * viewRadius);
                  }
            }
            int vertexCount = viewPoints.Count + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[(vertexCount - 2) * 3];
            
            Vector2[] uvs = new Vector2[vertexCount];
            uvs[0] = new Vector2(0.5f, 0.5f);
            vertices[0] = Vector3.zero;
            for (int i = 0; i < vertexCount - 1; ++i)
            {
                  vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
                  uvs[i + 1] = new Vector2(
                        vertices[i + 1].x / (viewRadius * 2f) + 0.5f, 
                        vertices[i + 1].z / (viewRadius * 2f) + 0.5f
                  );
                  
                  if (i < vertexCount - 2)
                  {
                        triangles[i * 3] = 0;
                        triangles[i * 3 + 1] = i + 1;
                        triangles[i * 3 + 2] = i + 2;
                  }
                  
            }

            _viewMesh.Clear();
            _viewMesh.vertices = vertices;
            _viewMesh.triangles = triangles;
            _viewMesh.uv = uvs;
            _viewMesh.RecalculateNormals();
      }
      public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
      {
            if (!angleIsGlobal)
            {
                  angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
      }
      
}
