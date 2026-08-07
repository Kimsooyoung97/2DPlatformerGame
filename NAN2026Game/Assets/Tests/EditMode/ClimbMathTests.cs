using NUnit.Framework;
using NAN2026.Core;

public class ClimbMathTests
{
    [Test] public void Up_Positive() { Assert.AreEqual(3.5f, ClimbMath.ClimbVelocity(true, false, 3.5f)); }
    [Test] public void Down_Negative() { Assert.AreEqual(-3.5f, ClimbMath.ClimbVelocity(false, true, 3.5f)); }
    [Test] public void Both_Zero() { Assert.AreEqual(0f, ClimbMath.ClimbVelocity(true, true, 3.5f)); }
    [Test] public void None_Zero() { Assert.AreEqual(0f, ClimbMath.ClimbVelocity(false, false, 3.5f)); }
}
