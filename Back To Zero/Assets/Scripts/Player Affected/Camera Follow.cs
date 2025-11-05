using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public float FollowSpeed = 1f;
    public float YOffset = 1f;
    public Transform Target;

    void Update()
    {
      Vector3 newPosition = new Vector3(Target.position.x, Target.position.y + YOffset, -10f);
      transform.position = Vector3.Slerp(transform.position, newPosition, FollowSpeed * Time.deltaTime);
    }
}
