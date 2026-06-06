using UnityEngine;

namespace ChainNet.Camera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset = new(0f, 14f, -12f);
        [SerializeField] private float followSpeed = 5f;

        public void SetTarget(Transform target) => followTarget = target;

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            var targetPos = followTarget.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        public void TriggerImpact(float intensity)
        {
            transform.position += Random.insideUnitSphere * (0.05f * intensity);
        }
    }
}
