using UnityEngine;

namespace OneBitLab.Services
{
    public class SingletonBase<T> : MonoBehaviour where T: MonoBehaviour 
    {
        //-------------------------------------------------------------
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if( s_Instance == null )
                {
                    s_Instance = FindObjectOfType<T>();
                }

                return s_Instance;
            }
        }
        //-------------------------------------------------------------
    }
}