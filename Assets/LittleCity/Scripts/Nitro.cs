using System;
namespace LittleCity {
 public sealed partial class Simulation {
  public bool NitroInstalled,NitroActive;public float NitroCharge=1;bool nitroRelease;
  public void CancelNitro(){NitroActive=false;nitroRelease=true;}
  void TickNitro(float dt,DriveInput input) {
   if(!input.Nitro)nitroRelease=false;
   NitroActive=NitroInstalled&&Mode==Ride.RaceCar&&input.Nitro&&!nitroRelease&&!input.Brake&&input.Forward>.1f&&!NeedsRepair&&!Indoors;
   if(NitroActive){NitroCharge=Math.Max(0,NitroCharge-dt/3);if(NitroCharge<=.01f){NitroActive=false;nitroRelease=true;}}
   else if(!input.Nitro)NitroCharge=Math.Min(1,NitroCharge+dt/8);
  }
 }
}
