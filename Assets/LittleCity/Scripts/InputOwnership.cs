using System;
using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public static class PointerPresence
    {
        // The event stream can lose its final Up/Cancel when a window or view changes.
        // Confirm ownership against the physical input before applying gameplay input.
        public static Func<int,bool> IsHeld=ReadPhysical;
        public static bool ReadPhysical(int id)
        {
            if(!Application.isFocused)return false;
            if(id<0)return id>=-3 && Input.GetMouseButton(-id-1);
            for(int n=0;n<Input.touchCount;n++){var t=Input.GetTouch(n);if(t.fingerId==id)return t.phase!=TouchPhase.Ended && t.phase!=TouchPhase.Canceled;}
            return false;
        }
    }
    public sealed class KeyReleaseGate
    {
        static readonly KeyCode[] keys={KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.UpArrow,KeyCode.DownArrow,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.J,KeyCode.L,KeyCode.I,KeyCode.K,KeyCode.E,KeyCode.Q,KeyCode.Z,KeyCode.X,KeyCode.V,KeyCode.Space,KeyCode.LeftShift,KeyCode.LeftControl,KeyCode.LeftAlt,KeyCode.RightAlt,KeyCode.M,KeyCode.Equals,KeyCode.Minus,KeyCode.KeypadPlus,KeyCode.KeypadMinus};
        readonly HashSet<KeyCode> blocked=new HashSet<KeyCode>();
        public void Clear(Func<KeyCode,bool> source){foreach(var key in keys)if(source(key))blocked.Add(key);}
        public void Poll(Func<KeyCode,bool> source){foreach(var key in keys)if(!source(key))blocked.Remove(key);}
        public bool Read(KeyCode key,Func<KeyCode,bool> source)=>!blocked.Contains(key)&&source(key);
    }
}
