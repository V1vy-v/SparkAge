using UnityEngine;

namespace SparkAge.View.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        protected abstract void Init();

        protected virtual void Start()
        {
            Init();
        }
        public virtual void ShowMe()
        {
            gameObject.SetActive(true);
        }
        public virtual void HideMe()
        {
            gameObject.SetActive(false);
        }
    }
}
