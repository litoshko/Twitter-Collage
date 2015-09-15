using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace web_twitter_collage.Models
{
    public class LoadUserViewModel
    {
        [DisplayName("User Name:")]
        [Required]
        [DataType(DataType.Text)]
        public string Text { get; set; }
        [DisplayName("Collage image size:")]
        [Required]
        [DataType(DataType.Text)]
        public int size { get; set; }
        [DisplayName("Resize images proportionally to tweets count:")]
        [Required]
        public bool Resize { get; set; }


        public string Response { get; set; }
    }
}
