using System.ComponentModel.DataAnnotations;

namespace Rocky_Models
{
    public class ApplicationType
    {
        // столбец идентификации
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
