using UnityEngine;
using System.Collections.Generic;

namespace LittleCity
{
    public sealed class CrashParts : MonoBehaviour
    {
        static readonly Queue<GameObject> recent = new Queue<GameObject>();
        float remaining=7;
        void Update() { remaining-=Time.deltaTime; if(remaining<=0)Destroy(gameObject); }
        public static void Scatter(VehicleModel vehicle,float speed)
        {
            var container = new GameObject("Recoverable cartoon crash parts");container.AddComponent<CrashParts>();
            recent.Enqueue(container);while(recent.Count>3){var old=recent.Dequeue();if(old!=null)Object.Destroy(old);}
            var ground=container.AddComponent<BoxCollider>();container.transform.position=new Vector3(vehicle.Root.position.x,-.20f,vehicle.Root.position.z);ground.size=new Vector3(24,.4f,24);
            int count=0;
            foreach(Transform child in vehicle.Root)
            {
                bool part=child.name.Contains("hood") || child.name.Contains("Nose") || child.name.Contains("spoiler") || child.name.Contains("Spinning wheel") || child.name=="Roof" || child.name.Contains("mirror") || child.name=="Wheel arch";
                if(!part || count>=10)continue;
                var clone=Object.Instantiate(child.gameObject,child.position,child.rotation,container.transform);
                clone.transform.localScale=Vector3.Scale(child.lossyScale,Vector3.one);
                foreach(var owned in clone.GetComponentsInChildren<OwnedMesh>()) owned.Mesh=null;
                foreach(var filter in clone.GetComponentsInChildren<MeshFilter>())
                {
                    filter.sharedMesh=Object.Instantiate(filter.sharedMesh);
                    var owned=filter.GetComponent<OwnedMesh>() ?? filter.gameObject.AddComponent<OwnedMesh>();owned.Mesh=filter.sharedMesh;
                }
                foreach(Collider c in clone.GetComponentsInChildren<Collider>())Object.Destroy(c);
                var renderer=clone.GetComponentInChildren<Renderer>();
                var collider=clone.AddComponent<BoxCollider>();collider.size=renderer==null?Vector3.one*.35f:clone.transform.InverseTransformVector(renderer.bounds.size);
                collider.size=new Vector3(Mathf.Abs(collider.size.x),Mathf.Abs(collider.size.y),Mathf.Abs(collider.size.z));
                collider.center=renderer==null?Vector3.zero:clone.transform.InverseTransformPoint(renderer.bounds.center);
                var rb=clone.AddComponent<Rigidbody>();rb.mass=1;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                rb.linearVelocity=-vehicle.Root.forward*speed*.18f+vehicle.Root.right*(count%2==0?-1:1)*(1+count*.15f)+Vector3.up*(2+count%3);
                rb.angularVelocity=new Vector3(count%3+1,2,count%2==0?2:-2);
                child.gameObject.SetActive(false);count++;
            }
        }
    }
}
