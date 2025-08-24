namespace Ogma.Tests;

[TestClass]
public sealed class BasicTests
{
    [TestMethod]
    public void TestInMemory()
    {
        var store  = new Store<uint, Person>( String.Empty );
        var person = new Person( "John", "Doe", 25 );
        store[1] = person;

        var value = store[1];
        Assert.AreEqual( person, value );
    }

    [TestMethod]
    public async Task TestDiskPersistenceWithoutEncryption()
    {
        const string fileName = "./noencryptiontest.ogma";
        var          store    = new Store<uint, Person>( fileName );
        var          person   = new Person( "John", "Doe", 25 );
        store[1] = person;

        await store.SaveAsync();
        store = await Store<uint, Person>.OpenAsync( fileName );

        var value = store[1];
        Assert.AreEqual( person, value );
    }

    [TestMethod]
    public async Task TestDiskPersistenceWithEncryption()
    {
        const string fileName = "./encryptiontest.ogma";
        const string password = "hunter2";

        var store  = new Store<uint, Person>( fileName, password );
        var person = new Person( "John", "Doe", 25 );
        store[1] = person;

        await store.SaveAsync();
        store = await Store<uint, Person>.OpenAsync( fileName, password );

        var value = store[1];
        Assert.AreEqual( person, value );
    }
}
