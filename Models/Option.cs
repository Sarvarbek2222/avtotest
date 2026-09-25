public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }

    public string OptionUZ { get; set; } = "";   // O'zbek (lotin) — majburiy
    public string? OptionRU { get; set; }         // Rus
    public string? OptionUZK { get; set; }        // O'zbek (kirill)

    public bool IsCorrect { get; set; }

    public Question? Question { get; set; }
}
