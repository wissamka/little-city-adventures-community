using UnityEngine;
namespace LittleCity
{
    public sealed class ExplorerFlip
    {
        readonly CityGame game;const float Window=.32f;
        bool held,wasActive;float pending=-1;int world,character;
        public bool Active=>game.State.FlipMotionActive;
        public float Angle=>game.State.FlipProgress*1080;
        public int Completed {get;private set;}
        public ExplorerFlip(CityGame owner){game=owner;}
        bool OnFoot=>game.State.Mode==Ride.Walk && !game.State.Swimming && !game.State.Indoors && game.State.Resting==RestPose.None;
        public void Input(bool pressed,float dt)
        {
            bool edge=pressed&&!held;held=pressed;
            if(game.Hud.MenuOpen || !OnFoot){Cancel();return;}
            if(wasActive)
            {
                if(world!=game.State.WorldIndex || character!=game.State.Character){Cancel();return;}
                if(!Active){if(game.State.FlipProgress>=1)Completed++;wasActive=false;}
                return;
            }
            if(edge)
            {
                if(pending>=0 && pending<=Window)
                {
                    pending=-1;
                    if(game.State.BeginFlipMotion()){wasActive=true;world=game.State.WorldIndex;character=game.State.Character;game.Hud.ClearInput();game.State.Say("Triple backflip!");}
                }
                else pending=game.State.Grounded?0:-1;
            }
            if(pending>=0){pending+=dt;if(pending>Window)pending=-1;}
        }
        public void Cancel(){pending=-1;wasActive=false;game.State.CancelFlipMotion();}
        public void Apply(CharacterModel model)
        {
            if(!Active)return;
            var turn=Quaternion.Euler(-Angle,0,0);Vector3 pivot=Vector3.up*(1.45f*game.State.WalkingScale);
            model.Root.localRotation=turn;model.Root.localPosition=pivot-turn*pivot;
        }
    }
}
