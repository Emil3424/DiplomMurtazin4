using System.ComponentModel.DataAnnotations;

namespace DiplomMurtazin.Model
{
    [MetadataType(typeof(UserMetadata))]
    public partial class Users
    {
        // Дополнительные свойства, если нужны
    }

    public class UserMetadata
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Пароль должен быть не менее 3 символов")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        public string Role { get; set; }
    }
}