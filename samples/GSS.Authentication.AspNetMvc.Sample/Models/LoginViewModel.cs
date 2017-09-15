using System.ComponentModel.DataAnnotations;

namespace GSS.Authentication.AspNetMvc.Sample.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}