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
    public static async Task Main(string[] args)
    {
        //Парсим параметры или используем дефолтное значение
        string inPath = "./songs.csv";

        var csvLoader = new CsvLoader<Song>((arg) => new Song(arg[2], arg[1].Split(" & "), ulong.Parse(arg[0]), DateOnly.Parse(arg[3])), ';', 1); // streams;artist_name;track_name;date

        List<string> russianArtists = new();
        List<string> foreignArtists = new();


        const string russianAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";


        await foreach (var s in csvLoader.LoadData(inPath))
        {
            foreach (var artist in s.ArtistsName)
            {
                if (foreignArtists.Contains(artist) || russianArtists.Contains(artist))
                    continue;

                if (artist.Any(x => russianAlphabet.contains(x)))
                {
                    russianArtists.Add(artist);
                    continue;
                }

                foreignArtists.Add(artist);

            }

        }

        Console.WriteLine($"Количество российских исполнителей: {russianArtists.Count}");
        Console.WriteLine($"Количество иностранных исполнителей: {foreignArtists.Count}");

        await File.AppendAllLinesAsync("russian_artists.txt", russianArtists);
        await File.AppendAllLinesAsync("foreign_artists.txt", foreignArtists);

    }
}