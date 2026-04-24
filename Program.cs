using SS15.Code.Controllers;
using System;

class Program
{
    static void Main(string[] args)
    {
        int size = 256; // по умолчанию
        if (args.Length > 0 && int.TryParse(args[0], out int parsedSize) && parsedSize > 0)
        {
            size = parsedSize;
            Console.WriteLine($"Запуск с картой {size}x{size}");
        }
        else
        {
            Console.WriteLine("Запуск с картой по умолчанию 256x256. Можно указать размер: dotnet run -- 10");
        }
        Console.WriteLine("В игре: 1 – 10x10, 2 – 100x100, 3 – 256x256, R – сброс текущей");

        var master = new MasterController(size);
        master.Run();
    }
}