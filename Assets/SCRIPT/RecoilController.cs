using UnityEngine;

public class RecoilController : MonoBehaviour
{
    [Header("Recoil Smoothness")]
    public float snappiness = 20f;
    public float returnSpeed = 10f;
    [Range(0f, 1f)] public float cameraKickRatio = 0.25f; // Real CS:GO keeps crosshair low while bullets rise

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void AddRecoil(float verticalKick, float horizontalKick)
    {
        // Only apply a fraction of the kick to the camera itself
        targetRotation += new Vector3(-verticalKick * cameraKickRatio, horizontalKick * cameraKickRatio, 0f);
    }
}