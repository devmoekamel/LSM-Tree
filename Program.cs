


using LSMStorageEngine.memtable;

var memtable  = new MemoryTable();

memtable.Put("Key1", System.Text.Encoding.UTF8.GetBytes( "Value1"));

Console.WriteLine(memtable.isFull);