using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    // A separate, elastic silhouette uses the existing contact rig. Visual bounce
    // never moves the player collider, seat, pedals, hands, or saved position.
    public sealed class GummyCharacter : MonoBehaviour
    {
        public Transform Visual { get; private set; }
        public Transform ElasticBody { get; private set; }
        public bool Active { get; private set; }
        public bool LivingGlass {get;private set;}
        public Light GlowLight {get;private set;}
        Material bodyGel,limbGel,bodyGlass,limbGlass;
        public float Stretch { get; private set; } = 1;
        public Color EyeColor { get; private set; }
        public Renderer[] Originals { get; private set; }
        public readonly Transform[] Hands = new Transform[2], Feet = new Transform[2];
        CharacterModel model;
        Transform face, crown;
        readonly Transform[] arms = new Transform[2], legs = new Transform[2], eyes = new Transform[2];
        readonly List<Material> materials = new List<Material>();
        readonly List<GameObject> closedEyes = new List<GameObject>();
        Mesh bean, smileMesh;
        float time, speed, compression, springVelocity, previousVertical;
        bool seated, sleeping, swimming, grounded = true, wasGrounded = true;
        float verticalSpeed;
        public static bool Selected => GraphicsQuality.Current != null ? GraphicsQuality.Current.Cozy :
            PlayerPrefs.GetInt(GraphicsQuality.CozyPreference, 0) == 1 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--cozy") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--living-glass")>=0;
        public static bool GlassSelected=>GraphicsQuality.Current!=null?GraphicsQuality.Current.LivingGlass:Selected && (PlayerPrefs.GetInt(GraphicsQuality.GlassPreference,0)==1 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--living-glass")>=0);

        public static void Attach(CharacterModel rig, int index)
        {
            var gummy = rig.Root.gameObject.AddComponent<GummyCharacter>();
            rig.Gummy = gummy; gummy.model = rig;
            gummy.Originals = rig.Root.GetComponentsInChildren<Renderer>(true);
            gummy.Build(index);gummy.SetLivingGlass(GlassSelected); gummy.SetStyle(Selected);
        }
        public static void SetAll(bool active)
        {
            foreach (var gummy in FindObjectsByType<GummyCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {gummy.SetLivingGlass(GlassSelected);gummy.SetStyle(active);}
        }
        public void SetStyle(bool active)
        {
            Active = active; Visual.gameObject.SetActive(active);if(GlowLight!=null)GlowLight.gameObject.SetActive(active && LivingGlass);
            // forceRenderingOff leaves blink/sleep and the skinned rig's own enabled
            // states intact, including eyes that change after the style is selected.
            foreach (var r in Originals) if (r != null) r.forceRenderingOff = active;
            compression = springVelocity = previousVertical = 0; wasGrounded = grounded;
            Draw(0);
        }
        public void SetLivingGlass(bool enabled)
        {
            LivingGlass=enabled;
            foreach(var renderer in Visual.GetComponentsInChildren<Renderer>(true))
            {
                var m=renderer.sharedMaterial;
                if(m==bodyGel || m==bodyGlass)renderer.sharedMaterial=enabled?bodyGlass:bodyGel;
                else if(m==limbGel || m==limbGlass)renderer.sharedMaterial=enabled?limbGlass:limbGel;
            }
            if(GlowLight!=null)GlowLight.gameObject.SetActive(Active && enabled);
        }
        public void EnablePlayerGlow()
        {
            if(GlowLight!=null)return;
            GlowLight=new GameObject("Living glass local glow").AddComponent<Light>();GlowLight.transform.SetParent(Visual,false);GlowLight.transform.localPosition=new Vector3(0,.9f,0);
            GlowLight.type=LightType.Point;GlowLight.shadows=LightShadows.None;GlowLight.renderMode=LightRenderMode.ForcePixel;GlowLight.range=2.5f;GlowLight.cullingMask=~(1<<9);
            GlowLight.color=Color.Lerp(bodyGlass.color,Color.white,.18f);GlowLight.intensity=.45f;GlowLight.gameObject.SetActive(Active && LivingGlass);
        }
        public void Pose(float clock, float movement, bool sitting)
        { time = clock; speed = movement; seated = sitting; }
        public void Motion(bool onGround, float vertical, bool asleep, bool inWater)
        { grounded = onGround; verticalSpeed = vertical; sleeping = asleep; swimming = inWater; }
        public void Sleep(bool asleep) { sleeping = asleep; }
        void LateUpdate() { if (Active) Draw(Mathf.Min(Time.deltaTime, .1f)); }

        public void Draw(float dt)
        {
            if (!Active) return;
            if(GlowLight!=null && LivingGlass)GlowLight.intensity=(seated?.16f:.45f)*(1+.035f*Mathf.Sin(time*2.1f));
            if (dt > 0)
            {
                if (grounded && !wasGrounded && !seated && !swimming)
                    springVelocity -= Mathf.Clamp(-previousVertical * .8f, 1.8f, 6);
                wasGrounded = grounded; previousVertical = verticalSpeed;
                // Substeps keep the damped rebound stable on slower devices.
                int steps = Mathf.Max(1, Mathf.CeilToInt(dt / .008f)); float h = dt / steps;
                for (int i = 0; i < steps; i++)
                { springVelocity += (-115 * compression - 7.5f * springVelocity) * h; compression += springVelocity * h; }
                compression = Mathf.Clamp(compression, -.30f, .20f);
            }
            float moving = seated || sleeping ? 0 : Mathf.Clamp01(Mathf.Abs(speed) / 2.6f);
            float stride = Mathf.Sin(time * 9), doubleStep = Mathf.Cos(time * 18);
            float air = !grounded && !seated && !swimming ? .12f + Mathf.Clamp(verticalSpeed * .012f, -.06f, .07f) : 0;
            Stretch = Mathf.Clamp(1 + compression + air + doubleStep * moving * .085f + Mathf.Sin(time * (sleeping ? 1.8f : 2.6f)) * .018f, .70f, 1.25f);
            float width = 1 / Mathf.Sqrt(Stretch);
            // The wide bean sits lower in a cockpit; contacts remain on the actual controls.
            ElasticBody.localPosition = new Vector3(0, seated ? .66f : .20f + moving * (.065f + doubleStep * .065f), 0);
            ElasticBody.localScale = new Vector3(width * (seated ? .75f : 1), Stretch * (seated ? .73f : 1), width * (seated ? .84f : 1));
            ElasticBody.localRotation = seated ? model.Body.localRotation : Quaternion.Euler(swimming ? -12 : moving * 6, 0, stride * moving * -5 + springVelocity * 1.2f);
            face.localRotation = Quaternion.Euler(0, model.Head.localEulerAngles.y, -stride * moving * 2);
            crown.localRotation = Quaternion.Euler(springVelocity * -3, 0, stride * moving * 5);
            bool blink = sleeping || Mathf.Repeat(time, 4.7f) > 4.53f;
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                eyes[i].localScale = new Vector3(1, blink ? .08f : 1, 1);
                foreach (var line in closedEyes) line.SetActive(sleeping);
                eyes[i].gameObject.SetActive(!sleeping);
                Vector3 hip = Visual.InverseTransformPoint(ElasticBody.TransformPoint(new Vector3(side * .32f, .32f, 0)));
                Vector3 shoulder = Visual.InverseTransformPoint(ElasticBody.TransformPoint(new Vector3(side * .57f, 1.04f, 0)));
                Vector3 foot = seated ? Visual.InverseTransformPoint(model.Ends[i].position) : new Vector3(side * .37f, .13f + Mathf.Max(0, stride * side) * moving * .20f, .09f + stride * side * moving * .24f);
                Vector3 hand = seated || swimming ? Visual.InverseTransformPoint(model.Ends[i + 2].position) : shoulder + new Vector3(side * .08f, -.40f + Mathf.Sin(time * 9 - .5f) * side * moving * .065f, -stride * side * moving * .28f);
                // Hammer and other raised-hand interactions also reach their original target.
                if (!seated && model.Ends[i + 2].localPosition.y > 2.1f) hand = Visual.InverseTransformPoint(model.Ends[i + 2].position);
                Feet[i].localPosition = foot; Feet[i].localScale = seated ? new Vector3(.28f, .22f, .32f) : new Vector3(.47f, .28f, .57f);
                Feet[i].localRotation = Quaternion.Euler(seated ? 0 : stride * side * moving * 18, side * 8, 0);
                Hands[i].localPosition = hand; Hands[i].localScale = Vector3.one * (seated ? .25f : .34f);
                Segment(legs[i], hip, foot, seated ? .24f : .32f);
                Segment(arms[i], shoulder, hand, seated ? .23f : .30f);
            }
        }
        static void Segment(Transform t, Vector3 a, Vector3 b, float width)
        { t.localPosition = (a + b) * .5f; t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); t.localScale = new Vector3(width, Vector3.Distance(a, b) + width * .65f, width); }
        Material Rubber(string name, Color color, float shine = .60f)
        {
            var m = new Material(Resources.Load<Shader>("GummyRubber")) { name = name, color = color, enableInstancing = true };
            m.SetFloat("_Glossiness", shine);m.renderQueue=2040; materials.Add(m); return m;
        }
        Transform Ball(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = position; o.transform.localScale = size;
            var collider = o.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            o.GetComponent<Renderer>().sharedMaterial = material; return o.transform;
        }
        void Build(int index)
        {
            Visual = Art.Group("Bubbly character", transform);
            ElasticBody = Art.Group("Squishy body and face", Visual);
            var pigment = Art.Hex(new[] { "4DABEB", "69BF80", "F7AA48", "EF6965", "E78394", "F7B657" }[index]);
            Color.RGBToHSV(pigment,out float hue,out float saturation,out float value);pigment=Color.HSVToRGB(hue,Mathf.Min(.93f,saturation*1.25f),value);
            var rubber = new Material(Resources.Load<Shader>("GummyGel")){name="Translucent candy body",color=pigment,enableInstancing=true};
            materials.Add(rubber);bodyGel=rubber;
            limbGel=new Material(rubber){name="Thin translucent candy limbs"};limbGel.SetFloat("_ThicknessScale",.48f);materials.Add(limbGel);
            bodyGlass=new Material(Resources.Load<Shader>("GummyLivingGlass")){name="Living jewel glass",color=Art.Hex(new[]{"087CFF","12F75C","FFAC08","FF133C","FF189F","FF8708"}[index]),enableInstancing=true};bodyGlass.SetTexture("_GlassEnvironment",LivingGlassEnvironment.Texture);materials.Add(bodyGlass);
            limbGlass=new Material(bodyGlass){name="Thin living jewel glass"};limbGlass.SetFloat("_ThicknessScale",.48f);materials.Add(limbGlass);
            var dark = Rubber("Soft ink", Art.Hex("242D38"), .3f);
            var white = Rubber("Milky eyes", Art.Hex("FFF9EB"), .62f);
            EyeColor = Art.Hex("69492F");
            var iris = Rubber("Gummy iris", EyeColor, .72f);
            var cheek = Rubber("Warm cheeks", Color.Lerp(pigment, Art.Hex("F27E8C"), .45f), .45f);
            var body = new GameObject("Continuous rubber bean", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(ElasticBody, false); bean = BeanMesh(); body.GetComponent<MeshFilter>().sharedMesh = bean; body.GetComponent<Renderer>().sharedMaterial = rubber;
            face = Art.Group("Expressive face", ElasticBody);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                eyes[i] = Art.Group("Blinking eye", face, new Vector3(side * .205f, 1.46f, .411f));
                Ball("Eye white", eyes[i], Vector3.zero, new Vector3(.29f, .34f, .135f), white);
                Ball("Iris", eyes[i], new Vector3(-side * .015f, -.012f, .071f), new Vector3(.14f, .19f, .07f), iris);
                Ball("Pupil", eyes[i], new Vector3(-side * .015f, -.014f, .107f), new Vector3(.067f, .11f, .026f), dark);
                Ball("Catchlight", eyes[i], new Vector3(-.025f, .042f, .122f), new Vector3(.041f, .048f, .018f), white);
                var lid = Ball("Sleeping smile eye", face, new Vector3(side * .205f, 1.45f, .473f), new Vector3(.21f, .025f, .024f), dark);
                closedEyes.Add(lid.gameObject);
                Ball("Blush", face, new Vector3(side * .38f, 1.22f, .420f), new Vector3(.14f, .068f, .026f), cheek);
                arms[i] = Ball("Rubbery arm", Visual, Vector3.zero, Vector3.one, limbGel);
                legs[i] = Ball("Stubby leg", Visual, Vector3.zero, Vector3.one, limbGel);
                Hands[i] = Ball("Squishy mitten", Visual, Vector3.zero, Vector3.one, limbGel);
                Feet[i] = Ball("Puffy foot", Visual, Vector3.zero, Vector3.one, limbGel);
            }
            var smile = new GameObject("Smooth smile",typeof(MeshFilter),typeof(MeshRenderer));smile.transform.SetParent(face,false);
            smileMesh=SmileMesh();smile.GetComponent<MeshFilter>().sharedMesh=smileMesh;smile.GetComponent<Renderer>().sharedMaterial=dark;
            crown = Art.Group("Bouncy hair or hat", ElasticBody, new Vector3(0, 1.87f, 0));
            var hair = Rubber("Soft hair", Art.Hex("644436"), .44f);
            if (index == 5)
            {
                var cap = Rubber("Trail cap", Art.Hex("569663"), .51f);
                Ball("Rounded cap", crown, new Vector3(0, .018f, -.04f), new Vector3(.86f, .40f, .76f), cap);
                Ball("Soft cap brim", crown, new Vector3(.08f, -.09f, .38f), new Vector3(.70f, .09f, .53f), cap);
                Ball("Cap button", crown, new Vector3(0, .20f, -.04f), Vector3.one * .08f, cap);
            }
            else if (index == 1)
            {
                var hat = Rubber("Maya yellow beanie", Art.Hex("F7CF62"), .43f);
                Ball("Round beanie", crown, Vector3.zero, new Vector3(.82f, .36f, .72f), hat);
                Ball("Bouncy pompom", crown, new Vector3(.12f, .23f, 0), Vector3.one * .23f, hat);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    var tuft = Ball("Soft hair swoop", crown, new Vector3((i - 1) * .21f, .015f + i * .027f, -.02f), new Vector3(.43f, .27f, .58f), hair);
                    tuft.localRotation = Quaternion.Euler(0, -20, -12);
                }
            }
        }
        static Mesh BeanMesh()
        {
            const int rings = 40, sides = 64;
            var v = new Vector3[(rings + 1) * (sides + 1)]; var uv = new Vector2[v.Length]; var triangles = new List<int>();
            for (int y = 0; y <= rings; y++)
            {
                float a = Mathf.PI * y / rings, h = Mathf.Cos(a), radius = Mathf.Sin(a), fullness = 1 - h * .16f;
                for (int x = 0; x <= sides; x++)
                {
                    int n = y * (sides + 1) + x; float p = x * Mathf.PI * 2 / sides;
                    v[n] = new Vector3(Mathf.Cos(p) * radius * .67f * fullness, 1.02f + h * .96f, Mathf.Sin(p) * radius * .53f * fullness);
                    uv[n] = new Vector2(x / (float)sides, y / (float)rings);
                    if (y < rings && x < sides) { int next = n + sides + 1; triangles.Add(n); triangles.Add(n + 1); triangles.Add(next); triangles.Add(n + 1); triangles.Add(next + 1); triangles.Add(next); }
                }
            }
            var mesh = new Mesh { name = "Smooth continuous pear shaped rubber body" }; mesh.vertices = v; mesh.uv = uv; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            // Weld the UV seam's normals; no visible crease in glossy reflections.
            var normals = mesh.normals;
            for (int y = 0; y <= rings; y++) { int a = y * (sides + 1), b = a + sides; var n = (normals[a] + normals[b]).normalized; normals[a] = normals[b] = n; }
            mesh.normals = normals; mesh.RecalculateBounds(); return mesh;
        }
        static Mesh SmileMesh()
        {
            const int steps=24,sides=8;var vertices=new Vector3[(steps+1)*sides];var triangles=new List<int>();
            for(int i=0;i<=steps;i++)
            {
                float x=(i/(float)steps-.5f)*.23f;var center=new Vector3(x,1.14f+x*x*3,.515f);var normal=new Vector3(-6*x,1,0).normalized;
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides;vertices[i*sides+j]=center+(normal*Mathf.Cos(a)+Vector3.forward*Mathf.Sin(a))*.011f;
                    if(i<steps){int n=i*sides+j,k=i*sides+(j+1)%sides;triangles.Add(n);triangles.Add(k);triangles.Add(n+sides);triangles.Add(k);triangles.Add(k+sides);triangles.Add(n+sides);}
                }
            }
            var mesh=new Mesh{name="Continuous curved smile"};mesh.vertices=vertices;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy() { foreach (var m in materials) if (m != null) Destroy(m); if (bean != null) Destroy(bean); if(smileMesh!=null)Destroy(smileMesh); }
    }
}
