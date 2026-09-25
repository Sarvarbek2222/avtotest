using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Question
{
    public int Id { get; set; }

    public string QuestionUZ { get; set; } = "";  // O'zbek (lotin) — majburiy, standart til
    public string? QuestionRU { get; set; }        // Rus
    public string? QuestionUZK { get; set; }       // O'zbek (kirill)

    // Mavzu (kategoriya) — masalan "Огоҳлантирувчи белгилар"
    [MaxLength(255)]
    public string? Topic { get; set; }
    // Rasm xossasi
    // ----------------------------
    public string? ImageUrl { get; set; }  // nullable, rasm majburiy emas

    [NotMapped]
    public IFormFile? ImageFile { get; set; }  // fayl upload uchun
    public string? ExplanationUZ { get; set; }
    public string? ExplanationUZK { get; set; }
    public string? ExplanationRU { get; set; }

    public List<Option> Options { get; set; } = new List<Option>();

}
