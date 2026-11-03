using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleCity
{
    public sealed class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        public RectTransform Knob;
        public Vector2 Value { get; private set; }
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue) return;
            pointer = data.pointerId; OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer) return;
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out Vector2 local);
            Value = Vector2.ClampMagnitude((local - rect.rect.center) / (Mathf.Min(rect.rect.width, rect.rect.height) * .36f), 1);
            if (Value.magnitude < .12f) Value = Vector2.zero;
            Knob.anchoredPosition = Value * rect.rect.width * .28f;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) Clear(); }
        public void OnEndDrag(PointerEventData data){if(data.pointerId==pointer)Clear();}
        public void OnCancel(BaseEventData data)=>Clear();
        public void Reconcile(){if(pointer!=int.MinValue && !PointerPresence.IsHeld(pointer))Clear();}
        void LateUpdate()=>Reconcile();
        public void Clear() { pointer = int.MinValue; Value = Vector2.zero; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
        void OnDisable() => Clear();
    }

    public sealed class HoldAction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        public bool Held { get; private set; }
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData data)
        { if (pointer == int.MinValue) { pointer = data.pointerId; Held = true; } }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) Clear(); }
        public void OnPointerExit(PointerEventData data) { if (pointer == data.pointerId) Clear(); }
        public void OnCancel(BaseEventData data) => Clear();
        public void Reconcile(){if(pointer!=int.MinValue && !PointerPresence.IsHeld(pointer))Clear();}
        void LateUpdate()=>Reconcile();
        public void Clear() { pointer = int.MinValue; Held = false; }
        void OnDisable() => Clear();
    }

    public sealed class LookGesture : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        public Vector2 Delta;
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData data) { if (pointer == int.MinValue) pointer = data.pointerId; }
        public void OnDrag(PointerEventData data) { if (pointer == data.pointerId) Delta += data.delta; }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) Clear(); }
        public void OnEndDrag(PointerEventData data){if(data.pointerId==pointer)Clear();}
        public void OnCancel(BaseEventData data)=>Clear();
        public void Reconcile(){if(pointer!=int.MinValue && !PointerPresence.IsHeld(pointer))Clear();}
        void LateUpdate()=>Reconcile();
        public Vector2 Consume() { Vector2 delta = Delta; Delta = Vector2.zero; return delta; }
        public void Clear() { pointer = int.MinValue; Delta = Vector2.zero; }
        void OnDisable() => Clear();
    }

    public sealed class SafeArea : MonoBehaviour
    {
        Rect previous; int width, height;
        void Update()
        {
            Rect area = Screen.safeArea;
            if (area == previous && width == Screen.width && height == Screen.height) return;
            previous = area; width = Screen.width; height = Screen.height;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.x / width, area.y / height);
            rect.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CityMap : MaskableGraphic
    {
        public Simulation Simulation;
        float timer;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;bool sea=Simulation!=null&&Simulation.WorldIndex==1&&!Simulation.Indoors;float mapWidth=sea?1800:Simulation!=null&&Simulation.WorldIndex==0?1840:480,mapHeight=sea?1280:Simulation!=null&&Simulation.WorldIndex==0?1640:420,centerZ=sea?440:0;
            void Box(float x, float z, float w, float h, Color col)
            {
                float sx = r.width / mapWidth, sy = r.height / mapHeight;
                Vector2 c = new Vector2(r.center.x + x * sx, r.center.y + (z-centerZ) * sy);
                int start = vh.currentVertCount;
                vh.AddVert(c + new Vector2(-w * sx / 2, -h * sy / 2), col, Vector2.zero);
                vh.AddVert(c + new Vector2(w * sx / 2, -h * sy / 2), col, Vector2.zero);
                vh.AddVert(c + new Vector2(w * sx / 2, h * sy / 2), col, Vector2.zero);
                vh.AddVert(c + new Vector2(-w * sx / 2, h * sy / 2), col, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
            if(Simulation==null)return;
            if(Simulation.Indoors)
            {
                Box(0,0,480,420,Art.Hex("D6C5A6"));Box(0,0,440,380,Art.White);Box(0,-182,60,15,Art.Teal);
            }
            else if(Simulation.WorldIndex==0)
            {
                Box(0,0,1840,1640,Art.Hex("86CDA4"));
                for(int i=-4;i<4;i++){float avenue=112+i*224;Box(avenue,0,18,1570,Art.White);Box(0,avenue,1760,18,Art.White);}
                foreach(int s in CityWorld.Streets){Box(s,0,13,215,Art.White);Box(0,s,215,13,Art.White);}
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if(x!=0||z!=0)Box(x*64,z*64,37,37,Art.Hex("D5B68F"));
                Box(136,-96,73,12,Art.White);Box(-13,8,15,22,Art.Blue);
            }
            else if(sea)
            {
                Box(0,440,1800,1280,Art.Blue);Box(-25,-75,410,225,Art.Hex("F0D8A0"));Box(172,-3,80,400,Art.Hex("F0D8A0"));
                for(int i=0;i<3;i++){var c=IslandLayout.Centers[i];Box(c.X,c.Z,164,164,i==0?Art.Teal:i==1?Art.Yellow:Art.White);var dock=IslandLayout.Dock(i);Box(dock.X,dock.Z,10,52,Art.White);}
            }
            else if(Simulation.WorldIndex==4)
            {
                Box(0,0,480,420,Art.Hex("A7B4B4"));
                for(int lane=0;lane<3;lane++)for(int stage=0;stage<3;stage++)Box(SkatePark.LaneX(lane),SkatePark.StartZ(stage)+7,SkatePark.Width(lane),14,lane==0?Art.Teal:lane==1?Art.Yellow:Art.Red);
                Box(123,55,68,68,Art.Blue);
            }
            else if(Simulation.WorldIndex==3)
            {
                Box(0,0,480,420,Art.Hex(Simulation.GeneratedTheme==1?"A9C2B6":Simulation.GeneratedTheme==3?"D8D6B7":"77B986"));
                foreach(var obstacle in Simulation.Obstacles)if(obstacle.HalfX>2 && obstacle.X<140)Box(obstacle.X,obstacle.Z,obstacle.HalfX*2,obstacle.HalfZ*2,Art.Hex("73978D"));
                Box(60,-32,210,10,Art.White);
            }
            else
            {
                Box(0,0,480,420,Art.Hex(Simulation.WorldIndex==1?"F0D8A0":"E0EDF3"));
                Box(35,-32,340,10,Art.White);Box(149,-64,10,72,Art.White);
                if(Simulation.WorldIndex==1){Box(-60,122,355,166,Art.Blue);Box(-65,105,22,84,Art.White);Box(-130,106,22,72,Art.White);Box(-5,106,22,72,Art.White);foreach(int x in new[]{22,55,90,104,-170,-185})Box(x+4,46,8,10,Art.Teal);Box(35,110,12,12,Art.Yellow);Box(-65,48,5,28,Art.White);Box(-130,52,5,30,Art.White);}
                else{Box(-30,6,14,11,Art.Red);Box(25,6,14,11,Art.Blue);Box(-105,80,40,40,Art.Teal);}
            }
            if(!Simulation.Indoors){Box(172,-3,22,310,Art.Ink);Box(172,165,38,12,Art.Teal);if(Simulation.WorldIndex==3)Box(172,-170,38,12,Art.Blue);}
            float px=r.center.x+(Simulation.Indoors?(Simulation.Position.X-Simulation.RoomX)/(Simulation.RoomHalfX*2.2f):Simulation.Position.X/mapWidth)*r.width;
            float py=r.center.y+(Simulation.Indoors?(Simulation.Position.Z-Simulation.RoomZ)/(Simulation.RoomHalfZ*2.2f):(Simulation.Position.Z-centerZ)/mapHeight)*r.height;
            Vector2 pos = new Vector2(Mathf.Clamp(px,r.xMin+8,r.xMax-8), Mathf.Clamp(py,r.yMin+8,r.yMax-8)), forward = new Vector2(Mathf.Sin(Simulation.Yaw), Mathf.Cos(Simulation.Yaw));
            Vector2 right = new Vector2(forward.y, -forward.x);
            int index = vh.currentVertCount;
            vh.AddVert(pos + forward * 8, Art.Red, Vector2.zero);
            vh.AddVert(pos - forward * 5 + right * 5, Art.Red, Vector2.zero);
            vh.AddVert(pos - forward * 5 - right * 5, Art.Red, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }
        void Update() { timer += Time.unscaledDeltaTime; if (timer > .1f) { timer = 0; SetVerticesDirty(); } }
    }
}
