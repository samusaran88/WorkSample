using UnityEngine;
public class PlanarReflectionManager : MonoBehaviour
{
    public Camera m_MainCamera;
    public Camera m_RearViewCamera;
    public Camera m_ReflectionCamera;     
    public GameObject m_ReflectionPlane;
    public RenderTexture m_RenderTarget_RearView;
    public RenderTexture m_RenderTarget_ReflectionPlane;
    public Material m_FloorMaterial;
    public Texture m_FloorTexture;
    public float waterHeight;
    [Range(0.0f, 1.0f)]
    public float m_ReflectionFactor = 0.5f;
    void Start()
    { 
        m_RenderTarget_ReflectionPlane = new RenderTexture(Screen.width, Screen.height, 24);
        //m_RenderTarget.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
    }
    private void OnPreRender()
    {
        RenderRearViewMirror();
        m_ReflectionPlane.transform.position = new Vector3(PlayerManager.I.player.transform.position.x, waterHeight, PlayerManager.I.player.transform.position.z); 
        RenderReflection();
    }
    void RenderRearViewMirror()
    {
        m_FloorMaterial.SetFloat("_ReflectionFactor", 0);
        m_RearViewCamera.Render();
    }
    void RenderReflection()
    {
        m_ReflectionCamera.CopyFrom(m_MainCamera);

        Vector3 cameraDirectionWorldSpace = m_MainCamera.transform.forward;
        Vector3 cameraUpWorldSpace = m_MainCamera.transform.up;
        Vector3 cameraPositionWorldSpace = m_MainCamera.transform.position;

        //Transform the vectors to the floor's space
        Vector3 cameraDirectionPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraDirectionWorldSpace);
        Vector3 cameraUpPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraUpWorldSpace);
        Vector3 cameraPositionPlaneSpace = m_ReflectionPlane.transform.InverseTransformPoint(cameraPositionWorldSpace);

        //Mirror the vectors
        cameraDirectionPlaneSpace.y *= -1.0f;
        cameraUpPlaneSpace.y *= -1.0f;
        cameraPositionPlaneSpace.y *= -1.0f;

        //Transform the vectors back to world space
        cameraDirectionWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraDirectionPlaneSpace);
        cameraUpWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraUpPlaneSpace);
        cameraPositionWorldSpace = m_ReflectionPlane.transform.TransformPoint(cameraPositionPlaneSpace);

        //Set camera position and rotation
        m_ReflectionCamera.transform.position = cameraPositionWorldSpace;
        m_ReflectionCamera.transform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);

        //Set render target for the reflection camera
        m_ReflectionCamera.targetTexture = m_RenderTarget_ReflectionPlane;

        //Render the reflection camera
        m_ReflectionCamera.Render();

        m_FloorMaterial.SetTexture("_ReflectionTex", m_RenderTarget_ReflectionPlane);
        m_FloorMaterial.SetFloat("_ReflectionFactor", m_ReflectionFactor);
    } 
}
