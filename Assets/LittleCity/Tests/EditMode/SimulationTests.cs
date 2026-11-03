using NUnit.Framework;
using UnityEngine;

namespace LittleCity.Tests
{
    public sealed class SimulationTests
    {
        [Test]
        public void MovementCollisionAndFlightScenarios()
        {
            SimulationScenarios.Run();
            Debug.Log("SIMULATION_CHECKS_PASSED " + SimulationScenarios.Passed.Count);
        }
        [Test]
        public void RequiredCartoonShaderIsIncluded()
        { Assert.That(Shader.Find("LittleCity/Sunshine"), Is.Not.Null); }
    }
}
