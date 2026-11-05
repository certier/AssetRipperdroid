namespace AssetRipper.SerializationLogic.Tests;

public class SerializablePrimitiveTypeTests
{
    [Test]
    public async Task ConcurrentGetOrCreateCallsReturnTheCachedInstance()
    {
        SerializablePrimitiveType[] results = new SerializablePrimitiveType[256];

        Parallel.For(
            0,
            results.Length,
            index => results[index] = SerializablePrimitiveType.GetOrCreate(PrimitiveType.Int));

        for (int index = 1; index < results.Length; index++)
        {
            await Assert.That(ReferenceEquals(results[0], results[index])).IsTrue();
        }
    }
}
