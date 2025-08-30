namespace Ogma.Tests;

[TestClass]
public sealed class BasicTests
{
    [TestMethod]
    public void TestInMemory()
    {
        var store  = new Store<uint, Person>( default );
        var person = new Person( "John", "Doe", 25 );
        store[1] = person;

        var value = store[1];
        Assert.AreEqual( person, value );
    }

    [TestMethod]
    public async Task TestDiskPersistenceWithoutEncryption()
    {
        var options = new StoreOptions { Path = "./noencryptiontest.ogma" };
        var store   = new Store<uint, Person>( options );
        
        var          person   = new Person( "John", "Doe", 25 );
        store[1] = person;

        await store.SaveAsync();
        store = await Store<uint, Person>.OpenAsync( options );

        var value = store[1];
        Assert.AreEqual( person, value );
    }
}
