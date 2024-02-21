using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util.Inherited
{
    public class DisposableSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        
        public static T Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = FindObjectOfType<T>(true);
                }

                return instance;
            }

            set=> instance = value;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = GetComponent<T>();
            }
            else if (instance != this)
            {
                Debug.Log("Destroy dispoable singleton [ " + gameObject.name + " ]");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}

