//C# .NET 8 


using System.Diagnostics;
using System.Linq;

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
    public static async Task Main(string[] args)
    {
        //Парсим параметры или используем дефолтное значение
        string inPath = "./songs.csv";

        var csvLoader = new CsvLoader<Song>((arg) => new Song(arg[2], arg[1].Split(" & "), ulong.Parse(arg[0]), DateOnly.Parse(arg[3])), ';', 1); // streams;artist_name;track_name;date

      

        var artists = new Dictionary<string, ulong>();



        await foreach (var s in csvLoader.LoadData(inPath))
        {
            var artist = s.ArtistsName[0];

            if(!artists.ContainsKey(artist))
            {
                artists.Add(artist, 1);
                continue;
            }

            artists[artist] = artists[artist] + 1;

        }

        artists = artists.OrderByDescending(x => x.Value).Take(10).ToDictionary();

        foreach(var a in artists)
        {
            Console.WriteLine($"{a.Key} выпустил {a.Value} песен.");
        } 


    }




    private static string NormilizeArtistName(string[] artists)
    {
        if (artists.Length == 1)
            return artists[0];

        return String.Join(" & ", artists);
    }

    /// <summary>
    /// Расчёт кол-во просмотров песни при ошибке в бд
    /// </summary>
    /// <param name="s">Объект песни</param>
    /// <param name="baseDate">Базова дата (из условия)</param>
    /// <returns></returns>
    private static ulong FallbackViewCount(Song s, DateOnly baseDate)
    {
        return ((ulong)Math.Abs((baseDate.DayNumber - s.ReleaseDate.DayNumber) / (NormilizeArtistName(s.ArtistsName).Length + s.Name.Length))) * 10000;
    }
}