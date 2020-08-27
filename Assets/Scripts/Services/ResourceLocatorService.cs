using UnityEngine;

namespace OneBitLab.Services
{
    public class ResourceLocatorService : SingletonBase<ResourceLocatorService>
    {
        //-------------------------------------------------------------
        public RenderTexture m_HeightFieldRT;
        public Camera        m_MainCam;

        //-------------------------------------------------------------
    }
}