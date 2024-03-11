//C# .NET 8 


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
        string outPath = "./songs_new.csv";
        
        DateOnly filterUntillDate = DateOnly.Parse("01.01.2002");


        if (args.Length == 1) { inPath = args[0]; }
        else if (args.Length == 2) { inPath = args[0]; outPath = args[1]; }

        var csvLoader = new CsvLoader<Song>((arg) => new Song(arg[2], arg[1].Split(" & "), ulong.Parse(arg[0]), DateOnly.Parse(arg[3])), ';', 1); // streams;artist_name;track_name;date


        await using var fileWritter = new StreamWriter(outPath);

        string csvDeviser = " - ";
        await fileWritter.WriteLineAsync("track_name - artist_name - streams");
        

        await foreach(var s in csvLoader.LoadData(inPath)) 
        {
            if (s.ReleaseDate.DayNumber > filterUntillDate.DayNumber)
                continue; //Пропускаем песни, выпущенные позже


            var newSong = s;

            if (s.ViewCount == 0)
            {
                newSong = newSong with { ViewCount = FallbackViewCount(s, filterUntillDate) };
            }

            await fileWritter.WriteLineAsync($"{newSong.Name}{csvDeviser}{newSong.ArtistsName[0]}{csvDeviser}{newSong.ViewCount}");
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