using LSMStorageEngine.StorageEngine;


    var config = new StorageEngineConfig { MaxMemTableSize = 1000, Path = @"D:\\mine\\data\" };
    var engine = new StorageEngine(config);

    engine.Put("key1", new byte[] { 1, 2, 3 });

engine.ManualFlushToSSTable();
//Console.WriteLine(engine.Get("key1"));
//engine.Delete("key1");
Console.WriteLine(engine.Get("key1"));

Console.WriteLine("Test complete");
