using TMPro;
using UnityEngine;
namespace LittleCity
{
    public sealed class WorldLabel : MonoBehaviour
    {
        public float FarDistance=38;
        TMP_Text label;Color tint;
        void Awake(){label=GetComponent<TMP_Text>();tint=label.color;}
        void LateUpdate(){RefreshVisibility(Camera.main);}
        public void RefreshVisibility(Camera camera)
        {
            if(camera==null)return;
            float distance=Vector3.Distance(transform.position,camera.transform.position);
            float alpha=Mathf.Clamp01((distance-4)/4)*Mathf.Clamp01((FarDistance-distance)/10);
            Vector3 screen=camera.WorldToViewportPoint(transform.position);
            alpha*=screen.z>0?Mathf.Clamp01(Mathf.Min(screen.x,1-screen.x,screen.y,1-screen.y)*15):0;
            Color c=tint;c.a=alpha*.8f;label.color=c;
        }
    }
}
