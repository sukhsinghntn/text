using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDAProcesses.Shared.Models
{
    public class UserModel
    {
        [Key]
        public string UserName { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Department { get; set; }
        public string? Title { get; set; }
        public string? Email { get; set; }
        public string? Manager { get; set; }
        public string? Location { get; set; }
        public string? Password { get; set; }
    }
}
