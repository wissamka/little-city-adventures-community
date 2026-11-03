using NUnit.Framework;
namespace LittleCity.Tests
{
    [SetUpFixture]public sealed class PointerTestSetup
    {
        [OneTimeSetUp]public void SyntheticEventsOwnTheirPointers(){PointerPresence.IsHeld=_=>true;}
        [OneTimeTearDown]public void RestorePhysicalSource(){PointerPresence.IsHeld=PointerPresence.ReadPhysical;}
    }
}
