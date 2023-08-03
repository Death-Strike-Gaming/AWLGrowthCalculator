namespace AWLGrowthCalculator;

class Program
{
    static void Main()
    {
        var calculator = GrowthCalculatorFactory.Create();
        calculator.Run();
    }
}

// Creates a fully-configured GrowthCalculator instance.
class GrowthCalculatorFactory
{
    public static GrowthCalculator Create()
    {
        IInputParser inputParser = new DateTimeInputParser();
        IConverter<int> seasonConverter = new SeasonConverter();
        IConverter<int> timeConverter = new TimeConverter();
        IConverter<double> durationConverter = new MinutesToHalfDecimalConverter();

        return new GrowthCalculator(inputParser, seasonConverter, timeConverter, durationConverter);
    }
}

// Encapsulates the calculation of a plant's growth time.
class GrowthCalculator
{
    private readonly IInputParser _inputParser;
    private readonly IConverter<int> _seasonConverter;
    private readonly IConverter<int> _timeConverter;
    private readonly IConverter<double> _durationConverter;

    public GrowthCalculator(
        IInputParser inputParser,
        IConverter<int> seasonConverter,
        IConverter<int> timeConverter,
        IConverter<double> durationConverter )
    {
        _inputParser = inputParser;
        _seasonConverter = seasonConverter;
        _timeConverter = timeConverter;
        _durationConverter = durationConverter;
    }

    public void Run()
    {
        Console.WriteLine("Welcome to the SOS:AWL Growth Calculator by Death_Strike_Gaming");

        while ( true )
        {
            bool isValidInputA = false, isValidInputB = false;

            Console.WriteLine();

            InputData inputDataA = new();
            InputData inputDataB = new();

            while ( !isValidInputA )
            {
                Console.Write("Enter the Planted time (e.g., Summer 15 3:30 PM): ");
                string? inputA = Console.ReadLine();
                if ( inputA != null )
                    isValidInputA = _inputParser.TryParseInput(inputA, out inputDataA);
            }

            while ( !isValidInputB )
            {
                Console.Write("Enter the Harvested time (e.g., Autumn 28 8:45 AM): ");
                string? inputB = Console.ReadLine();
                if ( inputB != null )
                    isValidInputB = _inputParser.TryParseInput(inputB, out inputDataB);
            }

            if ( !isValidInputA || !isValidInputB )
            {
                continue;
            }

            int seasonDateA = _seasonConverter.Convert(inputDataA.Season ?? "DefaultSeason");
            int seasonDateB = _seasonConverter.Convert(inputDataB.Season ?? "DefaultSeason");


            if ( seasonDateA == -1 || seasonDateB == -1 )
            {
                continue;
            }

            int minutesA = inputDataA.AmPm != null ? _timeConverter.Convert($"{inputDataA.Hour}:{inputDataA.Minute} {inputDataA.AmPm}") : -1;
            int minutesB = inputDataB.AmPm != null ? _timeConverter.Convert($"{inputDataB.Hour}:{inputDataB.Minute} {inputDataB.AmPm}") : -1;

            int daysBetween = SeasonDateCalculator.CalculateDaysBetween(seasonDateA + inputDataA.Day, seasonDateB + inputDataB.Day);

            double halfDecimal = _durationConverter.Convert($"{minutesA} {minutesB}");

            double result = halfDecimal + daysBetween;

            Console.WriteLine($"Growth time was {result} days.");

            Console.WriteLine("Press Escape key to exit, press C to clear and continue, or any other key to calculate another plant's growth.");
            var keyInfo = Console.ReadKey();
            if ( keyInfo.Key == ConsoleKey.Escape )
            {
                break;
            }
            if ( keyInfo.Key == ConsoleKey.C )
            {
                Console.Clear();
                Console.WriteLine("Welcome to the SOS:AWL Growth Calculator by Death_Strike_Gaming");
            }
        }
    }
}

// Interface for parsing user input into a structured format.
interface IInputParser
{
    bool TryParseInput( string input, out InputData inputData );
}

// Implementation of IInputParser that expects a date and time as input.
class DateTimeInputParser : IInputParser
{
    public bool TryParseInput( string input, out InputData inputData )
    {
        string [] parts = input.Split(' ');

        if ( parts.Length != 4 )
        {
            Console.WriteLine("Invalid input format. Please use the format: 'Season Day Hour:Minute AM/PM'");
            inputData = new InputData();
            return false;
        }

        string season = parts [ 0 ];
        if ( !int.TryParse(parts [ 1 ], out int day) ||
             !int.TryParse(parts [ 2 ].Split(':') [ 0 ], out int hour) ||
             !int.TryParse(parts [ 2 ].Split(':') [ 1 ], out int minute) ||
             !IsValidAmPm(parts [ 3 ].Trim()) )
        {
            Console.WriteLine("Invalid input values. Please provide valid input.");
            inputData = new InputData();
            return false;
        }

        inputData = new InputData
        {
            Season = season,
            Day = day,
            Hour = hour,
            Minute = minute,
            AmPm = parts [ 3 ]
        };
        return true;
    }

    private static bool IsValidAmPm( string amPm )
    {
        return string.Equals(amPm, "AM", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(amPm, "PM", StringComparison.OrdinalIgnoreCase);
    }
}

// Holds the structured input data parsed from the user's input.
class InputData
{
    public string? Season { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string? AmPm { get; set; }
}

// Interface for converting one type of data into another.
interface IConverter<T>
{
    T Convert( string input );
}

// Converts season names to an associated integer value.
class SeasonConverter : IConverter<int>
{
    public int Convert( string? season )
    {
        int seasonValue;
        string lowerCaseSeason = season?.ToLower() ?? string.Empty;
        switch ( lowerCaseSeason )
        {
            case "spring":
                seasonValue = 0;
                break;
            case "summer":
                seasonValue = 10;
                break;
            case "autumn":
            case "fall":
                seasonValue = 20;
                break;
            case "winter":
                seasonValue = 30;
                break;
            default:
                Console.WriteLine("Invalid season. Please provide a valid season: Spring, Summer, Autumn, or Winter.");
                return -1;
        }

        return seasonValue;
    }
}

// Converts a time (including AM/PM) into minutes since midnight.
class TimeConverter : IConverter<int>
{
    public int Convert( string time )
    {
        string [] parts = time.Split(' ');
        int hour = int.Parse(parts [ 0 ].Split(':') [ 0 ]);
        int minute = int.Parse(parts [ 0 ].Split(':') [ 1 ]);
        string amPm = parts [ 1 ];

        if ( amPm.ToUpper() == "AM" )
        {
            return hour * 60 + minute;
        }
        else if ( amPm.ToUpper() == "PM" )
        {
            int convertedHour = ( hour % 12 == 0 ) ? 12 : hour % 12 + 12;
            return convertedHour * 60 + minute;
        }
        else
        {
            throw new ArgumentException("Invalid AM/PM value. Please use either 'AM' or 'PM'.");
        }
    }
}

// Converts two numbers of minutes into a fraction of a day, rounded to the nearest half day.
class MinutesToHalfDecimalConverter : IConverter<double>
{
    private readonly int _minutesInDay;

    public MinutesToHalfDecimalConverter( int minutesInDay = 1440 )
    {
        this._minutesInDay = minutesInDay;
    }

    public double Convert( string input )
    {
        string [] parts = input.Split(' ');
        int minutesA = int.Parse(parts [ 0 ]);
        int minutesB = int.Parse(parts [ 1 ]);

        int minutesTotal;
        if ( minutesA > minutesB )
        {
            minutesTotal = minutesA - minutesB;
        }
        else if ( minutesB > minutesA )
        {
            minutesTotal = minutesB - minutesA;
        }
        else
        {
            minutesTotal = 0;
        }

        if ( minutesTotal < 0 || minutesTotal > this._minutesInDay )
        {
            throw new ArgumentException($"Invalid input: minutes should be between 0 and {this._minutesInDay}");
        }

        double percent = ( minutesTotal / (double) this._minutesInDay );

        if ( percent >= 0.75 )
            return 1;
        else if ( percent <= 0.75 && percent >= 0.25 )
            return 0.5;
        else
            return 0;
    }
}

// Calculates the number of days between two dates in a game season.
class SeasonDateCalculator
{
    public static int CalculateDaysBetween( int dateA, int dateB )
    {
        int totalDays = 40;

        if ( dateA <= dateB )
        {
            return dateB - dateA;
        }
        else
        {
            return ( totalDays - dateA ) + dateB;
        }
    }
}