using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public static partial class SkatePark
    {
        public const float MegaX=-133,MegaStart=-127,MegaLip=-102,MegaLanding=-74,MegaHeight=12;
        public static float PipeFloor(float x,float z)
        {
            float wall=Mathf.Clamp01((Mathf.Abs(x+133)-6)/16);
            float end=Mathf.Min(Mathf.SmoothStep(0,1,(z-6)/14),Mathf.SmoothStep(0,1,(76-z)/14));
            return 8*wall*wall*end;
        }
        static bool PlusGround(Point p,out float h)
        {
            h=0;
            if(Mathf.Abs(p.X+133)<=22 && p.Z>=6 && p.Z<=76){h=PipeFloor(p.X,p.Z);return true;}
            if(Mathf.Abs(p.X-MegaX)<=7)
            {
                if(p.Z>=MegaStart && p.Z<=MegaLip){h=MegaHeight*(p.Z-MegaStart)/25;return true;}
                if(p.Z>MegaLip && p.Z<MegaLanding){h=-6;return true;}
                if(p.Z>=MegaLanding && p.Z<=MegaLanding+42){h=6*(1-Mathf.Clamp01((p.Z-MegaLanding-20)/22));return true;}
            }
            // Connected low boxes have matching approach and departure ramps, with open bypass lanes.
            for(int i=0;i<3;i++)
            {
                float x=-130+i*34,z=112+i%2*16;
                if(Mathf.Abs(p.X-x)<4 && p.Z>=z && p.Z<=z+25)
                {h=1.8f*Mathf.Min(Mathf.Clamp01((p.Z-z)/8),Mathf.Clamp01((z+25-p.Z)/8));return true;}
            }
            return false;
        }
        static void BuildPlus(WorldPlace world)
        {
            var r=world.Root;Color blue=Art.Hex("779DAE"),gold=Art.Hex("E7A45B");
            var vertices=new List<Vector3>();var triangles=new List<int>();const int nx=64,nz=52;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {float px=-155+44f*x/nx,pz=6+70f*z/nz;vertices.Add(new Vector3(px,PipeFloor(px,pz)+.02f,pz));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+nx+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            Mesh(r,"Rideable curved half pipe with open roll-in ends",vertices,triangles,blue);
            foreach(int side in new[]{-1,1})Art.Tube("Halfpipe coping",r,new Vector3(-133+side*22,8.1f,20),new Vector3(-133+side*22,8.1f,62),.11f,Art.White);
            Sign(r,"HALF PIPE · ROLL IN",new Vector3(-165,2.3f,1),blue,10);
            Ramp(r,MegaX,MegaStart,MegaLip,14,0,12,gold);Ramp(r,MegaX,MegaLanding,MegaLanding+20,14,6,6,gold);Ramp(r,MegaX,MegaLanding+20,MegaLanding+42,14,6,0,gold);
            Art.Box("Mega jump gap",r,new Vector3(MegaX,.015f,(MegaLip+MegaLanding)/2),new Vector3(14,.025f,28),Art.Ink,0);
            Art.Box("Mega checkpoint",r,new Vector3(MegaX,.025f,MegaStart-16),new Vector3(18,.03f,1),Art.Yellow,0);
            Sign(r,"MEGA · 28 m GAP",new Vector3(-162,3,MegaStart-13),gold,13);
            var ring=Art.Ring("Mega air target",r,3,.10f,.12f,Art.Yellow);ring.transform.localPosition=new Vector3(MegaX,17,(MegaLip+MegaLanding)/2);ring.transform.localRotation=Quaternion.Euler(0,90,0);
            for(int i=0;i<3;i++)
            {
                float x=-130+i*34,z=112+i%2*16;
                Ramp(r,x,z,z+8,8,0,1.8f,blue);Ramp(r,x,z+8,z+17,8,1.8f,1.8f,blue);Ramp(r,x,z+17,z+25,8,1.8f,0,blue);
                foreach(int side in new[]{-1,1})Art.Tube("Low box edge rail",r,new Vector3(x+side*4,1.86f,z+8),new Vector3(x+side*4,1.86f,z+17),.065f,Art.White);
            }
            // Side-to-side gates leave large riding corridors; low hurdles can be jumped or bypassed.
            for(int row=0;row<5;row++)
            {
                float z=-52+row*11,gap=120+(row%2==0?-8:8);
                foreach(int side in new[]{-1,1})
                {
                    float x=gap+side*6;
                    Art.Box("Technical slalom gate",r,new Vector3(x,.65f,z),new Vector3(1.5f,1.3f,1),row%2==0?gold:blue,.04f);
                    AdventureWorlds.Solid(world,new Vector3(x,.65f,z),new Vector3(1.5f,1.3f,1));
                }
            }
            for(int i=0;i<3;i++)
            {
                float x=163,z=-43+i*13;
                Art.Tube("Jump-over hurdle",r,new Vector3(x-4,.8f,z),new Vector3(x+4,.8f,z),.13f,Art.Teal);
                foreach(int side in new[]{-1,1})Art.Tube("Hurdle leg",r,new Vector3(x+side*3,0,z),new Vector3(x+side*3,.8f,z),.1f,Art.Teal);
                AdventureWorlds.Solid(world,new Vector3(x,.45f,z),new Vector3(8,.9f,.35f));
            }
            Sign(r,"GATES / HURDLES",new Vector3(151,2.8f,-65),blue,12);
        }
        public static void PlusCheckpoint(Simulation s)
        {
            var p=s.Position;
            if(Mathf.Abs(p.X-MegaX)<9 && Mathf.Abs(p.Z-(MegaStart-16))<2)s.ParkCheckpoint=new Point(MegaX,0,MegaStart-24);
            if(Mathf.Abs(p.X+133)<8 && p.Z>7 && p.Z<12)s.ParkCheckpoint=new Point(-133,0,10);
        }
    }
}
