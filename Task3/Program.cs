//C# .NET 8 


using System.Diagnostics;

/// <summary>
/// Структура, описывающий песню из таблицы 
/// </summary>
/// <param name="Name">Название песни (track_name в csv)</param>
/// <param name="ArtistName">Название автора (artist_name в csv)</param>
/// <param name="ViewCount">Количество просмотров (streams в csv)</param>
/// <param name="ReleaseDate">Дата релиза (date в csv)</param>
public record struct Song(string Name, string[] ArtistsName, ulong ViewCount, DateOnly ReleaseDate);


/// <summary>
/// Класс, описывающий загрузку из таблицы
/// </summary>
/// <typeparam name="T">Тип элемента таблицы</typeparam>
/// <param name="Parser">Функция, преобразцющая строку таблицы в объект таблицы <see cref="T"/></param>
/// <param name="Devisor">Разделитель csv таблицы</param>
/// <param name="HeaderRowCount">Кол-во строк, котрое нужно пропустить в начале</param>
public class CsvLoader<T>(Func<string[], T> Parser, char Devisor = ';', int HeaderRowCount = 1)
{

    /// <summary>
    /// Возвращает поток с данными из csv таблицы
    /// </summary>
    /// <param name="filePath">Путь до файла</param>
    /// <returns></returns>
    public async IAsyncEnumerable<T> LoadData(string filePath)
    {
        int currentLine = 1;

        var reader = new StreamReader(filePath);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (currentLine <= HeaderRowCount) // Пропускаем первые строки
            {
                currentLine++;
                continue;
            }


            if (line == null) { continue; }

            yield return Parser(line.Split(Devisor));
        }

        yield break;
    }
}


public static class Program
{
    public static void Main(string[] args)
    {
        //Парсим параметры или используем дефолтное значение
        string inPath = "./songs.csv";

        if (args.Length == 1) { inPath = args[0]; }


        Console.WriteLine($"Ожидайте, идёт загрзука данных из {inPath}...");

        var timer = new Stopwatch();
        timer.Start();

        var csvLoader = new CsvLoader<Song>((arg) => new Song(arg[2], arg[1].Split(" & "), ulong.Parse(arg[0]), DateOnly.Parse(arg[3])), ';', 1); // streams;artist_name;track_name;date

        var songs = csvLoader.LoadData(inPath).ToBlockingEnumerable().ToList();


        timer.Stop();
        Console.WriteLine($"Готово! Данные загруженны за {timer.Elapsed.TotalMilliseconds} мс");

        Console.WriteLine($"Для выхода из программы напишите 0");

        Console.Write($"Введите имя артиста: ");
        string? userInput = Console.ReadLine();



        while (userInput != "0")
        {

            if (userInput is null ) 
            {
                Console.Write($"Введите имя артиста: ");
                userInput = Console.ReadLine();
                continue;
            }

            var artistsSong = songs.FirstOrDefault(x => x.ArtistsName.Any(x => x == userInput)); //Находим подходящию песню при помощи метода Linq FirstOrDefault, имеющий сложность O(n)

            if(artistsSong == default)
            {
                Console.WriteLine("К сожалению, ничего не удалось найти");
                Console.Write($"Введите имя артиста: ");
                userInput = Console.ReadLine();
                continue;
            }

            Console.WriteLine($"У {userInput} найдена песня: {artistsSong.Name}");

            Console.Write($"Введите имя артиста: ");
            userInput = Console.ReadLine();
        }
        Console.WriteLine("Досвидание");
    }


}