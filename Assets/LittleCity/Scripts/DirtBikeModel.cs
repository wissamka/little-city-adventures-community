using UnityEngine;
namespace LittleCity
{
    public static partial class Models
    {
        static void DirtBike(VehicleModel m)
        {
            Bike(m,true);m.Root.localScale=Vector3.one*1.05f;AddSuspension(m);
            foreach(Transform t in m.Root)if(t.name=="Mirror" || t.name=="Mirror stalk")t.gameObject.SetActive(false);
            Art.Box("Raised offroad front fender",m.Root,new Vector3(0,1.14f,1.03f),new Vector3(.43f,.10f,.91f),Art.Hex("F4AD32"),.045f);
            Art.Box("Long rear mudguard",m.Root,new Vector3(0,1.31f,-1.09f),new Vector3(.44f,.11f,.65f),Art.Hex("F4AD32"),.04f);
            Art.Box("White race number plate",m.Root,new Vector3(0,1.57f,.76f),new Vector3(.46f,.45f,.08f),Art.White,.05f);
            Art.Box("Number stripe one",m.Root,new Vector3(-.07f,1.59f,.81f),new Vector3(.045f,.27f,.012f),Art.Ink,0);
            Art.Box("Number stripe two",m.Root,new Vector3(.06f,1.59f,.81f),new Vector3(.045f,.27f,.012f),Art.Ink,0);
            foreach(int side in new[]{-1,1})Art.Box("Offroad hand guard",m.Root,new Vector3(side*.43f,1.58f,.72f),new Vector3(.28f,.12f,.12f),Art.Hex("F4AD32"),.04f);
            foreach(var wheel in m.Wheels)for(int n=0;n<24;n++)for(int side=-1;side<=1;side++)
            {
                float a=n*Mathf.PI*2/24;var tread=Art.Box("Offroad tire tread",wheel,new Vector3(side*.095f,Mathf.Cos(a)*.475f,Mathf.Sin(a)*.475f),new Vector3(.09f,.04f,.085f),Art.Ink,.008f);tread.transform.localRotation=Quaternion.Euler(a*Mathf.Rad2Deg,0,0);
            }
        }
    }
}
